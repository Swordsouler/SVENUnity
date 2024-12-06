using System.Collections.Generic;
using OWLTime;
using RDF;
using SVEN.Content;
using UnityEngine;
using VDS.RDF;

namespace SVEN
{
    /// <summary>
    /// Core component to semantize content.
    /// </summary>
    [DisallowMultipleComponent]
    public class SemantizationCore : MonoBehaviour
    {
        /// <summary>
        /// Components to semantize at initialization. WARNING: Do not use this list for algorithmic purposes (it's just for the Unity Editor).
        /// </summary>
        [HideInInspector]
        public List<Component> componentsToSemantize = new();

        /// <summary>
        /// Properties of the each Component to semantize.
        /// </summary>
        private readonly Dictionary<Component, List<Property>> componentsProperties = new();

        //[SerializeField]
        //private GraphBuffer graphBuffer;

        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        [SerializeField]
        private GraphBuffer graphBuffer;

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private void Start()
        {
            if (graphBuffer == null) graphBuffer = GraphManager.Get("sven");
            componentsToSemantize.RemoveAll(component => component == null);
            Initialize();
        }

        /// <summary>
        /// Initializes the semantization of the GameObject, his components and setup the properties observers.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Semantize the GameObject attached to his properties and components
                componentsProperties.Add(this, SemanticObserve(graphBuffer));

                // foreach component in the GameObject, semantize the component and his properties
                foreach (Component component in componentsToSemantize)
                    componentsProperties.Add(component, component.SemanticObserve(graphBuffer));

            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Adds a component to the semantization process on the fly. WARNING: When adding components, it will be semantized until his end of life.
        /// </summary>
        /// <param name="component">The component to add to the semantization process.</param>
        public void AddSemanticComponent(Component component)
        {
            if (componentsToSemantize.Contains(component))
            {
                Debug.LogWarning("Component " + component.GetType().Name + " is already being semantized.");
                return;
            }

            componentsToSemantize.Add(component);
            List<Property> properties = component.SemanticObserve(graphBuffer);
            componentsProperties.Add(component, properties);
        }

        /// <summary>
        /// Overrides the default SemanticObserve method to focus on the GameObject semantization.
        /// </summary>
        /// <param name="graphBuffer">The graph buffer to semantize the GameObject.</param>
        public List<Property> SemanticObserve(GraphBuffer graphBuffer)
        {
            List<Property> properties = new()
            {
                new Property("name", () => gameObject.name),
                new Property("active", () => gameObject.activeSelf),
                new Property("tag", () => gameObject.tag),
                new Property("layer", () => LayerMask.LayerToName(gameObject.layer))
            };

            IGraph graph = graphBuffer.Graph;

            IUriNode gameObjectNode = graph.CreateUriNode("sven:" + this.GetUUID());

            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:VirtualObject")));
            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(name)));
            foreach (Property property in properties)
            {
                property.SemanticObserve(graphBuffer, this);
                if (Settings.Debug)
                    Debug.Log("Observing property (" + name + ")." + GetType().Name + "." + property.Name);
            }

            Interval interval = this.GetInterval();
            interval.Start(graphBuffer.CurrentInstant);
            IUriNode intervalNode = interval.Semantize(graph);
            graph.Assert(new Triple(gameObjectNode, graph.CreateUriNode("time:hasTemporalExtent"), intervalNode));

            return properties;
        }

        #region Properties Observers

        /// <summary>
        /// Checks if the observed properties have changed and invokes the callbacks if they have.
        /// </summary>
        private void CheckForChanges()
        {
            List<Component> toRemove = new();
            foreach (KeyValuePair<Component, List<Property>> componentProperties in componentsProperties)
            {
                try
                {
                    foreach (Property property in componentProperties.Value)
                        property.CheckForChanges();
                }
                catch
                {
                    if (Settings.Debug) Debug.LogWarning("Component " + componentProperties.Key.GetType().Name + " has been destroyed. Removing from semantization.");
                    toRemove.Add(componentProperties.Key);
                }
            }

            foreach (Component component in toRemove)
            {
                foreach (Property property in componentsProperties[component])
                    property.Destroy();

                Interval interval = component.GetInterval();
                interval.End(graphBuffer.CurrentInstant);
                interval.Semantize(graphBuffer.Graph);
                component.DestroyUUID();

                componentsProperties.Remove(component);
            }
        }

        /// <summary>
        /// OnEnable is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            CheckForChanges();
        }

        /// <summary>
        /// OnDisable is called when the behaviour becomes disabled or inactive.
        /// </summary>
        private void OnDisable()
        {
            CheckForChanges();
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
            CheckForChanges();
        }

        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            foreach (KeyValuePair<Component, List<Property>> componentProperties in componentsProperties)
            {
                foreach (Property property in componentProperties.Value)
                    property.Destroy();

                Interval interval = componentProperties.Key.GetInterval();
                interval.End(graphBuffer.CurrentInstant);
                interval.Semantize(graphBuffer.Graph);
                componentProperties.Key.DestroyUUID();
            }
        }

        #endregion
    }
}