using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sven.GraphManagement;
using Sven.OwlTime;
using Sven.Utils;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Sven.Content
{
    /// <summary>
    /// Observes changes in a single property and triggers multiple callbacks when the property changes.
    /// </summary>
    public class Property : Resource
    {
        /// <summary>
        /// The graph buffer to semantize the property.
        /// </summary>
        private GraphBuffer graphBuffer;

        /// <summary>
        /// The parent component of the property.
        /// </summary>
        private Component parentComponent;

        /// <summary>
        /// The semantization core to semantize the property.
        /// </summary>
        private SemantizationCore parentObject;

        /// <summary>
        /// The parent object node of the property.
        /// </summary>
        private IUriNode ParentObjectNode => graphBuffer.Graph.CreateUriNode("sven:" + parentObject.GetUUID());

        /// <summary>
        /// The parent component node of the property.
        /// </summary>
        private IUriNode ParentComponentNode => graphBuffer.Graph.CreateUriNode("sven:" + parentComponent.GetUUID());

        /// <summary>
        /// The simplified name of the property to observe.
        /// </summary>
        private readonly string simplifiedName;

        /// <summary>
        /// The name of the property to observe.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Gets the name of the property to observe.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// The last instant the property was semantized.
        /// </summary>
        private Instant lastSemantizedInstant;

        /// <summary>
        /// The interval of the property.
        /// </summary>
        private Interval interval;

        /// <summary>
        /// Represents a property to observe.
        /// </summary>
        private class ObservedProperty
        {
            /// <summary>
            /// A list of functions that return the values of the properties to observe.
            /// </summary>
            public Func<object> Getter;

            /// <summary>
            /// The callback to invoke when any of the observed properties change.
            /// </summary>
            public List<Action> Callbacks = new();

            /// <summary>
            /// The last known values of the observed properties.
            /// </summary>
            public object LastValue;
        }

        /// <summary>
        /// The property to observe.
        /// </summary>
        private readonly ObservedProperty observedProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyObserver"/> class.
        /// </summary>
        /// <param name="getter">A function that returns the value of the property to observe.</param>
        public Property(string name, Func<object> getter, string simplifiedName = "")
        {
            this.name = name;
            this.simplifiedName = simplifiedName;
            observedProperty = new ObservedProperty
            {
                Getter = getter,
                LastValue = null
            };
        }

        /// <summary>
        /// Initializes the semantization of the property.
        /// </summary>
        /// <param name="graphBuffer">The graph buffer to semantize the property.</param>
        /// <param name="parentNode">The parent node of the property.</param>
        public void SemanticObserve(GraphBuffer graphBuffer, Component parentComponent)
        {
            if (graphBuffer == null || parentComponent == null)
            {
                Debug.LogWarning("You are trying to initialize the semantization of a property without a graph buffer or parent component.");
                return;
            }
            if (this.graphBuffer != null)
            {
                Debug.LogWarning("You are trying to initialize the semantization of a property that has already been initialized.");
                return;
            }
            this.graphBuffer = graphBuffer;
            this.parentComponent = parentComponent;
            parentObject = parentComponent.GetComponent<SemantizationCore>();
        }

        private bool _isCheckingForChanges = false;

        /// <summary>
        /// Checks if the observed property has changed and invokes the callbacks if it has.
        /// </summary>
        public async void CheckForChanges()
        {
            if (_isCheckingForChanges) return;
            _isCheckingForChanges = true;
            SynchronizationContext context = SynchronizationContext.Current;
            await Task.Run(() =>
            {
                object currentValue = null;
                context.Send(_ => { if (observedProperty != null && parentObject != null) currentValue = observedProperty.Getter(); }, null);
                if (currentValue == null) return;

                if (!Equals(currentValue, observedProperty.LastValue))
                {
                    // limit the semantization with the graph instantPerSecond
                    Instant currentInstant = null;
                    context.Send(_ => currentInstant = graphBuffer.CurrentInstant, null);
                    if (lastSemantizedInstant != null && currentInstant == lastSemantizedInstant) return;

                    lastSemantizedInstant = currentInstant;
                    observedProperty.LastValue = currentValue;
                    if (graphBuffer != null) context.Send(_ => Semantize(currentInstant), null);
                }
            });
            _isCheckingForChanges = false;
        }

        /// <summary>
        /// Semantizes the property.
        /// </summary>
        public void Semantize(Instant currentInstant)
        {
            if (graphBuffer == null)
            {
                Debug.LogWarning("You are trying to semantize a property without a graph buffer.");
                return;
            }

            if (SvenDebugger.Debug)
                Debug.Log("Semantizing property (" + parentComponent.name + ")." + parentComponent.GetType().Name + "." + Name + " with value " + observedProperty.LastValue);
            DestroyUUID();

            IGraph graph = graphBuffer.Graph;

            IUriNode propertyNode = graph.CreateUriNode("sven:" + GetUUID());
            IUriNode propertyTypeNode = graph.CreateUriNode("sven:" + MapppedProperties.GetValue(observedProperty.LastValue.GetType()).TypeName);

            graph.Assert(new Triple(ParentComponentNode, graph.CreateUriNode("sven:" + name), propertyNode));
            graph.Assert(new Triple(propertyNode, graph.CreateUriNode("rdf:type"), propertyTypeNode));
            graph.Assert(new Triple(propertyNode, graph.CreateUriNode("sven:exactType"), propertyTypeNode));

            Interval oldInterval = interval;
            interval = new Interval();
            oldInterval?.End(currentInstant, interval);
            oldInterval?.Semantize(graph);

            interval.Start(currentInstant, oldInterval);
            IUriNode intervalNode = interval.Semantize(graph);
            graph.Assert(new Triple(propertyNode, graph.CreateUriNode("time:hasTemporalExtent"), intervalNode));

            Dictionary<string, object> values = observedProperty.LastValue.GetSemantizableValues();
            foreach (KeyValuePair<string, object> value in values)
            {
                string stringValue = value.Value.ToRdfString();
                string XmlSchemaDataType = value.Value.GetXmlSchemaTypes();
                if (XmlSchemaDataType == XmlSpecsHelper.XmlSchemaDataTypeBoolean) stringValue = stringValue.ToLower();
                ILiteralNode literalNode = graph.CreateLiteralNode(stringValue, new Uri(XmlSchemaDataType));
                graph.Assert(new Triple(propertyNode, graph.CreateUriNode("sven:" + value.Key), literalNode));

                if (simplifiedName != "")
                {
                    // créez un nouvelle intervalle à chaque fois ?
                    string simplifiedType = value.Key == "value" ? "" : value.Key.ToUpper();
                    Triple triple = new(ParentObjectNode, graph.CreateUriNode("sven:" + simplifiedName + simplifiedType), literalNode);
                    graph.Assert(triple);
                    graph.Assert(new Triple(graph.CreateTripleNode(triple), graph.CreateUriNode("time:hasTemporalExtent"), intervalNode));
                }
            }
        }

        public void Destroy()
        {
            IGraph graph = graphBuffer.Graph;
            Interval oldInterval = interval;
            interval = new Interval("sven:", GetUUID());
            oldInterval?.End(graphBuffer.CurrentInstant, interval);
            oldInterval?.Semantize(graph);
            DestroyUUID();
        }
    }
}
