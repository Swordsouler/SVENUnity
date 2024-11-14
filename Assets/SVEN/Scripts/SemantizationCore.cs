using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
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
        private static readonly IGraph graph = new Graph();

        /// <summary>
        /// Flag to randomize the resource identifier.
        /// </summary>
        public bool randomizeResourceID = true;

        /// <summary>
        /// Unique identifier for the resource.
        /// </summary>
        [SerializeField, HideIf("randomizeResourceID"), InfoBox("Be sure to set a unique identifier for the resource.", EInfoBoxType.Warning)]
        private string resourceID = "ResourceID";
        public string ResourceID
        {
            get => resourceID;
            set => resourceID = value;
        }

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
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (randomizeResourceID) resourceID = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
            foreach (KeyValuePair<Component, List<Property>> componentProperties in componentsProperties)
                foreach (Property property in componentProperties.Value)
                {
                    property.CheckForChanges();
                }
        }

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private void Start()
        {
            gameObjectProperties.Add(new Property("name", () => gameObject.name));
            gameObjectProperties.Add(new Property("active", () => gameObject.activeSelf));
            gameObjectProperties.Add(new Property("tag", () => gameObject.tag));
            gameObjectProperties.Add(new Property("layer", () => gameObject.layer));

            // foreach component, print all variables
            foreach (Component component in componentsToSemantize)
            {
                List<Property> properties = (List<Property>)typeof(SemantizationExtensions)
                                                    .GetMethod("GetProperties", new[] { component.GetType() })
                                                    .Invoke(null, new object[] { component });
                componentsProperties.Add(component, properties);
                foreach (Property property in properties)
                {
                    //Debug.Log(Environment.Resource() + " " + this.Resource() + " " + component.Resource() + " " + property.Resource() + " " + property.Name + " " + property.LastValue);
                    property.AddCallback(() => Debug.Log(Environment.Resource() + " " + this.Resource() + " " + component.Resource() + " " + property.Resource() + " " + property.Name + " " + property.LastValue));
                }
            }
            SemantizeGameObject();

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

        private void SemantizeGameObject()
        {
            IUriNode gameObjectNode = SemantizationCore.graph.CreateUriNode(UriFactory.Create(ResourceID));

            var triples = new List<string>
            {
                $"{this.Resource()} a sven:GameObject",
                $"{this.Resource()} rdfs:label {gameObject.name.ToXSDData()}"
            };

            triples.AddRange(gameObjectProperties.Select(property => $"{ResourceID} sven:{property.Name} {property.Resource()}"));
            triples.AddRange(componentsProperties.Select(componentProperty => $"{ResourceID} sven:component {componentProperty.Key.Resource()}"));

            string graph = string.Join(" . \n", triples) + " .";
            Debug.Log(graph);
        }

        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {

            foreach (Property property in gameObjectProperties)
                property.RemoveAllCallbacks();

            foreach (KeyValuePair<Component, List<Property>> componentProperties in componentsProperties)
            {
                componentProperties.Key.DestroyResourceID();

                foreach (Property property in componentProperties.Value)
                    property.RemoveAllCallbacks();
            }

            //todo : semantize the end of life of the gameObject
        }
    }
}