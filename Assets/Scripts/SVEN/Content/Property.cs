using System;
using System.Collections.Generic;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace SVEN.Content
{
    /// <summary>
    /// Observes changes in a single property and triggers multiple callbacks when the property changes.
    /// </summary>
    public class Property : Resource
    {
        /// <summary>
        /// The graph to semantize the property.
        /// </summary>
        private IGraph graph;

        /// <summary>
        /// The parent component of the property.
        /// </summary>
        private Component parentComponent;

        /// <summary>
        /// The parent node of the property.
        /// </summary>
        private IUriNode ParentNode => graph.CreateUriNode("sven:" + parentComponent.GetUUID());

        /// <summary>
        /// The name of the property to observe.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Gets the name of the property to observe.
        /// </summary>
        public string Name => name;

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

        private readonly ObservedProperty observedProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyObserver"/> class.
        /// </summary>
        /// <param name="getter">A function that returns the value of the property to observe.</param>
        public Property(string name, Func<object> getter)
        {
            this.name = name;
            observedProperty = new ObservedProperty
            {
                Getter = getter,
                LastValue = null
            };
        }

        /// <summary>
        /// Initializes the semantization of the property.
        /// </summary>
        /// <param name="graph">The graph output to semantize the property.</param>
        /// <param name="parentNode">The parent node of the property.</param>
        public void SemanticObserve(IGraph graph, Component parentComponent)
        {
            if (graph == null || parentComponent == null)
            {
                Debug.LogWarning("You are trying to initialize the semantization of a property without a graph node or parent component.");
                return;
            }
            if (this.graph != null)
            {
                Debug.LogWarning("You are trying to initialize the semantization of a property that has already been initialized.");
                return;
            }
            this.graph = graph;
            this.parentComponent = parentComponent;
        }

        /// <summary>
        /// Checks if the observed property has changed and invokes the callbacks if it has.
        /// </summary>
        public void CheckForChanges()
        {
            object currentValue = observedProperty.Getter();
            if (!Equals(currentValue, observedProperty.LastValue))
            {
                observedProperty.LastValue = currentValue;
                if (graph != null) Semantize();
            }
        }

        /// <summary>
        /// Semantizes the property.
        /// </summary>
        public void Semantize()
        {
            if (Settings.Debug)
                Debug.Log("Semantizing property (" + parentComponent.name + ")." + parentComponent.GetType().Name + "." + Name + " with value " + observedProperty.LastValue);
            DestroyUUID();
            IUriNode propertyNode = graph.CreateUriNode("sven:" + GetUUID());

            graph.Assert(new Triple(ParentNode, graph.CreateUriNode("sven:" + name), propertyNode));
            graph.Assert(new Triple(propertyNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:Property")));
            Dictionary<string, object> values = observedProperty.LastValue.GetSemantizableValues();
            foreach (KeyValuePair<string, object> value in values)
            {
                string stringValue = value.Value.ToString();
                string XmlSchemaDataType = value.Value.GetXmlSchemaTypes();
                if (XmlSchemaDataType == XmlSpecsHelper.XmlSchemaDataTypeBoolean) stringValue = stringValue.ToLower();
                graph.Assert(new Triple(propertyNode, graph.CreateUriNode("sven:" + value.Key), graph.CreateLiteralNode(stringValue, new Uri(XmlSchemaDataType))));
            }
        }
    }
}