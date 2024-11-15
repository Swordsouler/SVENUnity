using System;
using System.Collections.Generic;
using System.Reflection;
using RDF;
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
        /// Unique identifier for the resource.
        /// </summary>
        //private readonly string resourceID = Guid.NewGuid().ToString();
        /// <summary>
        /// Gets the resource identifier for the property.
        /// </summary>
        /// <returns>Resource identifier.</returns>
        /*public string ResourceID()
        {
            return resourceID;
        }*/

        /// <summary>
        /// Gets the resource for the property.
        /// </summary>
        /// <returns>Resource.</returns>
        /*public string Resource()
        {
            return ResourceID();
        }*/

        /// <summary>
        /// The graph to semantize the property.
        /// </summary>
        private IGraph graph;

        /// <summary>
        /// The parent node of the property.
        /// </summary>
        private IUriNode parentNode;

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
                LastValue = getter()
            };
        }

        public void InitializeSemantization(IGraph graph, IUriNode parentNode)
        {
            if (graph == null || parentNode == null)
            {
                Debug.LogWarning("You are trying to initialize the semantization of a property without a graph or parent node.");
                return;
            }
            if (this.graph != null)
            {
                Debug.LogWarning("You are trying to initialize the semantization of a property that has already been initialized.");
                return;
            }
            Debug.Log("Initializing semantization of property " + name);
            this.graph = graph;
            this.parentNode = parentNode;
        }

        /// <summary>
        /// Checks if the observed property has changed and invokes the callbacks if it has.
        /// </summary>
        public void CheckForChanges()
        {
            object currentValue = observedProperty.Getter();
            //UnityEngine.Debug.Log("Current value: " + currentValue + " Last value: " + observedProperty.LastValue);
            if (!Equals(currentValue, LastValue))
            {
                observedProperty.LastValue = currentValue;
                if (graph != null) Semantize();
            }
        }

        public void Semantize()
        {
            Debug.Log("Semantizing property " + name);
            DestroyUUID();
            IUriNode propertyNode = graph.CreateUriNode("sven:" + GetUUID());

            graph.Assert(new Triple(parentNode, graph.CreateUriNode("sven:" + name), propertyNode));
            graph.Assert(new Triple(propertyNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:Property")));
            Dictionary<string, object> values = LastValue.GetSemantizableValues();
            foreach (KeyValuePair<string, object> value in values)
            {
                string stringValue = value.Value.ToString();
                string XmlSchemaDataType = value.Value.GetXmlSchemaTypes();
                if (XmlSchemaDataType == XmlSpecsHelper.XmlSchemaDataTypeBoolean) stringValue = stringValue.ToLower();
                graph.Assert(new Triple(propertyNode, graph.CreateUriNode("sven:" + value.Key), graph.CreateLiteralNode(stringValue, new Uri(XmlSchemaDataType))));
            }
        }

        public void Test(/*IGraph graph*/)
        {
            Dictionary<string, object> values = LastValue.GetSemantizableValues();

            foreach (KeyValuePair<string, object> value in values)
            {
                Debug.Log(value.Key + " : " + value.Value);
            }

            //graph.CreateUriNode("sven:" + this.GetUUID());
            /*PropertyInfo field = this.GetType().GetProperty("LastValue");
            object value = field.GetValue(this);
            Type valueType = value.GetType();
            Debug.Log(field.Name + " : " + value + " | " + field.PropertyType + " | " + valueType);*/
            //graph.Assert(new Triple(newPropertyNode, graph.CreateUriNode("sven:value"), graph.CreateLiteralNode(property.LastValue.ToString())));
        }

        /// <summary>
        /// Gets the last known value of the observed property.
        /// </summary>
        public object LastValue => observedProperty.LastValue;
    }
}