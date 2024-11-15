using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using RDF;
using SVEN.Content;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace SVEN
{
    /// <summary>
    /// Core component to semantize content.
    /// </summary>
    [DisallowMultipleComponent]
    public class SemantizationCore : MonoBehaviour
    {
        /// <summary>
        /// Bevahiours to semantize by using extensions methods.
        /// </summary>
        [HideInInspector]
        public List<Component> componentsToSemantize = new();

        /// <summary>
        /// Properties of the GameObject.
        /// </summary>
        /// <returns></returns>
        private readonly List<Property> gameObjectProperties = new();

        /// <summary>
        /// Properties of the each Component to semantize.
        /// </summary>
        private readonly Dictionary<Component, List<Property>> componentsProperties = new();

        /// <summary>
        /// Dictionary to store the URI nodes depending on the UUID.
        /// </summary>
        //private Dictionary<string, IUriNode> uriNodes = new();

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private void Start()
        {
            gameObjectProperties.Add(new Property("name", () => gameObject.name));
            gameObjectProperties.Add(new Property("active", () => gameObject.activeSelf));
            gameObjectProperties.Add(new Property("tag", () => gameObject.tag));
            gameObjectProperties.Add(new Property("layer", () => LayerMask.LayerToName(gameObject.layer)));

            Initialize();

            // SEMANTIZE GameObject
            // GRAPH sven:$Environment.resource()$ {
            //      resourceID a sven:GameObject .
            //      resourceID rdfs:label gameobject.name .
            //      resourceID sven:$foreach gameObjectObservers.Key$ $foreach gameObjectObservers.Value.ResourceID$ .
            //      resourceID sven:component $foreach components.resource()$
            // }

            // SEMANTIZE Component
            // GRAPH sven:(Environment.resourceID) {
            //      resourceID a sven:GameObject .
            //      resourceID rdfs:label gameobject.name .
            // }
        }

        private void Initialize()
        {
            try
            {
                GraphManager graphManager = GraphManager.Get("sven");
                IGraph graph = graphManager.graph;//NewGraph();

                // Semantize the GameObject attached to his properties and components
                IUriNode gameObjectNode = this.Semantize(graph);

                // foreach component, print all variables
                foreach (Component component in componentsToSemantize)
                {
                    IUriNode componentNode = graph.CreateUriNode("sven:" + component.GetUUID());
                    graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("sven:component"), componentNode));

                    List<Property> properties = (List<Property>)typeof(SemantizationExtensions)
                                                        .GetMethod("GetProperties", new[] { component.GetType() })
                                                        .Invoke(null, new object[] { component });
                    componentsProperties.Add(component, properties);
                    foreach (Property property in properties)
                    {
                        property.InitializeSemantization(graph, componentNode);
                        //Debug.Log(Environment.Resource() + " " + this.Resource() + " " + component.Resource() + " " + property.Resource() + " " + property.Name + " " + property.LastValue);

                        // Debug.Log(Environment.Current.GetUUID() + " " + this.GetUUID() + " " + component.GetUUID() + " " + property.GetUUID() + " " + property.Name + " " + property.LastValue));

                    }
                }

                //graphManager.Merge(graph);
                graphManager.Push();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        [Button("Print Graph")]
        private void PrintGraph()
        {
            GraphManager.Get("sven").Push();
        }

        public IUriNode Semantize(IGraph graph)
        {
            IUriNode gameObjectNode = graph.CreateUriNode("sven:" + this.GetUUID());

            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:GameObject")));
            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(this.name)));
            foreach (Property property in this.gameObjectProperties)
            {
                IUriNode propertyNode = graph.CreateUriNode("sven:" + property.GetUUID());
                graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("sven:" + property.Name), propertyNode));

                property.InitializeSemantization(graph, gameObjectNode);
            }
            return gameObjectNode;
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
            foreach (Property property in gameObjectProperties)
                property.CheckForChanges();
            foreach (KeyValuePair<Component, List<Property>> componentProperties in componentsProperties)
                foreach (Property property in componentProperties.Value)
                    property.CheckForChanges();
        }

        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            this.DestroyUUID();

            foreach (Property property in gameObjectProperties)
                property.DestroyUUID();

            foreach (KeyValuePair<Component, List<Property>> componentProperties in componentsProperties)
            {
                componentProperties.Key.DestroyUUID();

                foreach (Property property in componentProperties.Value)
                    property.DestroyUUID();
            }

            //todo : semantize the end of life of the gameObject
        }
    }
}