using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NaughtyAttributes;
using OWLTime;
using RDF;
using SVEN.Content;
using UnityEditor;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Query.Inference;

namespace SVEN
{
    /// <summary>
    /// Graph reader to load the scene content from the graph.
    /// </summary>
    public class GraphReader : GraphBehaviour
    {
        /// <summary>
        /// Current scene content.
        /// First string is the UUID of the object.
        /// Second string is the UUID of the component.
        /// </summary>
        private Dictionary<string, Tuple<GameObject, Dictionary<string, Component>>> currentSceneContent = new();

        /// <summary>
        /// Graph that contains rules
        /// </summary>
        private Graph schema;

        /// <summary>
        /// Validate properties and fields.
        /// </summary>
        private void OnValidate()
        {
            if (instants != null && instants.Count > 0)
                CurrentInstantIndex = Mathf.Clamp(CurrentInstantIndex, 0, MaxInstantIndex);
            else
                CurrentInstantIndex = 0;
        }


        public UnityEngine.Object tempGraphFile;
        public UnityEngine.Object tempSchemaFile;
        public UnityEngine.Object tempUnitySupportFile;
        private void Start()
        {
            if (tempGraphFile != null)
            {
                graph = new Graph();
                graph.LoadFromFile(AssetDatabase.GetAssetPath(tempGraphFile));
                graph.LoadFromFile(AssetDatabase.GetAssetPath(tempUnitySupportFile));
                LoadInstants();
            }

            if (tempSchemaFile != null)
            {
                schema = new Graph();
                schema.LoadFromFile(AssetDatabase.GetAssetPath(tempSchemaFile));
                StaticRdfsReasoner reasoner = new();
                reasoner.Initialise(schema);
                reasoner.Apply(graph);
            }
            LoadInstants();
            CurrentInstantIndex = 5;
        }

        /// <summary>
        /// Object description.
        /// </summary>
        public class ComponentDescription
        {
            /// <summary>
            /// Component.
            /// </summary>
            public Component Component { get; set; }

            /// <summary>
            /// Active state of the object.
            /// </summary>
            public bool Active { get; set; }
            /// <summary>
            /// Layer of the object.
            /// </summary>
            public int Layer { get; set; }
            /// <summary>
            /// Tag of the object.
            /// </summary> 
            public string Tag { get; set; }
            /// <summary>
            /// Name of the object.
            /// </summary>
            public string Name { get; set; }
        }

        /// <summary>
        /// Object description.
        /// </summary>
        public class GameObjectDescription
        {
            /// <summary>
            /// GameObject.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// UUID of the object.
            /// </summary>
            public string UUID { get; set; }

            /// <summary>
            /// Equals method.
            /// </summary>
            /// <param name="obj">Object to compare.</param>
            /// <returns>True if the objects are equal, false otherwise.</returns>
            public override bool Equals(object obj)
            {
                return obj is GameObjectDescription description &&
                       UUID == description.UUID;
            }

            /// <summary>
            /// Get the hash code.
            /// </summary>
            /// <returns>Hash code.</returns>
            public override int GetHashCode()
            {
                return HashCode.Combine(UUID);
            }
        }

        private void LoadInstant(Instant instant)
        {
            /*
            ?active ?layer ?tag ?name 
            sven:active ?active ;
            sven:layer ?layer ;
            sven:tag ?tag ;
            sven:name ?name ;
            */
            string query = $@"
                PREFIX time: <http://www.w3.org/2006/time#>
                PREFIX sven: <http://www.sven.fr/ontology#>

                SELECT ?object ?component ?componentType ?propertyName ?propertyNestedName ?propertyValue ?propertyType
                WHERE {{
                    ?object a sven:GameObject ;
                            sven:component ?component .
                    ?component sven:exactType ?componentType ;
                               ?propertyName ?property .
                    ?property sven:exactType ?propertyType ;
                              ?propertyNestedName ?propertyValue ;
                              time:hasTemporalExtent ?interval .
                    ?interval time:inside <{instant.GetUriNode(graph)}> .
                }}";

            // Execute the query
            SparqlResultSet results = graph.ExecuteQuery(query) as SparqlResultSet;

            // GameObject -> Component -> (ComponentType --- PropertyName -> PropertyIndex -> PropertyValue)
            Dictionary<string, Dictionary<string, Tuple<Type, Dictionary<string, Tuple<Type, Dictionary<string, object>>>>>> sceneContent = new();

            Debug.Log($"Instant: {instant.inXSDDateTime}");
            foreach (SparqlResult result in results.Cast<SparqlResult>())
            {
                // get uuids
                string objectUUID = result["object"].ToString().Split('#')[1];
                string componentUUID = result["component"].ToString().Split('#')[1];

                // get types
                string componentStringType = result["componentType"].ToString().Split("#")[1];
                string propertyStringType = result["propertyType"].ToString().Split("#")[1];

                string propertyName = result["propertyName"].NodeType switch
                {
                    NodeType.Uri => result["propertyName"].ToString().Split('#')[1],
                    _ => result["propertyName"].AsValuedNode().AsString()
                };
                string propertyNestedName = result["propertyNestedName"].ToString().Split('#')[1];
                string propertyValue = result["propertyValue"].AsValuedNode().AsString();

                Type componentType = MapppedComponents.GetType(componentStringType) ?? Type.GetType(componentStringType);
                if (!MapppedComponents.HasProperty(componentType, propertyName)) continue;

                Type propertyType = MapppedProperties.GetType(propertyStringType) ?? Type.GetType(propertyStringType);
                if (!MapppedProperties.HasNestedProperty(propertyType, propertyNestedName)) continue;

                if (!sceneContent.ContainsKey(objectUUID))
                    sceneContent[objectUUID] = new();

                if (!sceneContent[objectUUID].ContainsKey(componentUUID))
                    sceneContent[objectUUID][componentUUID] = new(componentType, new());

                if (!sceneContent[objectUUID][componentUUID].Item2.ContainsKey(propertyName))
                    sceneContent[objectUUID][componentUUID].Item2[propertyName] = new(propertyType, new());

                if (!sceneContent[objectUUID][componentUUID].Item2[propertyName].Item2.ContainsKey(propertyNestedName))
                    sceneContent[objectUUID][componentUUID].Item2[propertyName].Item2[propertyNestedName] = propertyValue;

            }

            foreach (var obj in sceneContent)
                foreach (var component in obj.Value)
                    foreach (var property in component.Value.Item2)
                        foreach (var value in property.Value.Item2)
                            Debug.Log($"{obj.Key} -> {component.Key} -> {component.Value.Item1} -> {property.Key} -> {property.Value.Item1} -> {value.Key} -> {value.Value}");

            // create gameobject for each identified object
            Dictionary<string, Tuple<GameObject, Dictionary<string, Component>>> oldSceneContent = new(currentSceneContent);
            Dictionary<string, Tuple<GameObject, Dictionary<string, Component>>> newSceneContent = new();

            //todo, manage it with MappedComponents Setter
            foreach (var obj in sceneContent)
            {
                GameObject gameObject;
                if (currentSceneContent.TryGetValue(obj.Key, out var gameObjectComponents))
                {
                    gameObject = gameObjectComponents.Item1;
                }
                else
                {
                    gameObject = new(obj.Key);
                }
                newSceneContent[obj.Key] = new(gameObject, new());

                // create components for each identified component
                foreach (var comp in obj.Value)
                {
                    if (!(currentSceneContent.ContainsKey(obj.Key) && currentSceneContent[obj.Key].Item2.TryGetValue(comp.Key, out Component component)))
                    {
                        if (comp.Value.Item1.Equals(typeof(Transform)))
                        {
                            component = gameObject.transform;
                        }
                        else
                        {
                            component = gameObject.AddComponent(comp.Value.Item1);
                            if (component is MeshRenderer meshRenderer)
                            {
                                meshRenderer.material = new Material(Shader.Find("Standard"));
                            }
                            if (component is MeshFilter meshFilter)
                            {
                                // load from resources
                                meshFilter.mesh = Resources.Load<Mesh>("Models/Pyramid");
                            }
                        }
                    }
                    newSceneContent[obj.Key].Item2[comp.Key] = component;

                    // apply properties for each identified property
                    foreach (var property1 in comp.Value.Item2)
                    {
                        // propertyName = position, rotation, scale, etc.
                        string propertyName = property1.Key;
                        // propertyType = UnityEngine.Vector3, UnityEngine.Quaternion, etc.
                        Type propertyType = property1.Value.Item1;
                        Dictionary<string, object> propertyValues = property1.Value.Item2;
                        try
                        {
                            if (propertyType == typeof(object)) throw new KeyNotFoundException();
                            List<string> indexes = MapppedProperties.GetValue(propertyType).NestedProperties;

                            if (indexes != null && indexes.Count > 0)
                            {
                                var constructorParams = new object[indexes.Count];
                                for (int i = 0; i < indexes.Count; i++)
                                {
                                    Debug.Log(propertyValues[indexes[i]].ToString());
                                    constructorParams[i] = float.Parse(propertyValues[indexes[i]].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                                }

                                var newInfo = Activator.CreateInstance(propertyType, constructorParams);
                                //Debug.Log(component.GetType() + " " + propertyName + " " + newInfo + " " + string.Join(" ", indexes.Select(index => propertyValues[index].ToString())));
                                if (propertyName.Contains("."))
                                {
                                    //nested property example material.color
                                    string[] nestedProperties = propertyName.Split('.');
                                    PropertyInfo property = component.GetType().GetProperty(nestedProperties[0]);
                                    if (property != null)
                                    {
                                        object nestedComponent = property.GetValue(component);
                                        PropertyInfo nestedProperty = nestedComponent.GetType().GetProperty(nestedProperties[1]);
                                        nestedProperty?.SetValue(nestedComponent, newInfo);
                                    }
                                }
                                else
                                {
                                    Debug.Log(component.GetType() + " " + propertyName + " " + newInfo);
                                    component.GetType().GetProperty(propertyName).SetValue(component, newInfo);
                                }
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            if (propertyName == "material.shader")
                            {
                                Material material = (component as MeshRenderer).material;
                                material.shader = Shader.Find(property1.Value.Item2["value"].ToString());
                            }
                            else
                            {
                                // apply the value directly to the property
                                PropertyInfo property = propertyType.GetProperty(propertyName);
                                property?.SetValue(component, property1.Value.Item2["value"]);
                            }

                        }
                    }
                }
            }

            // compare the old scene content with the new one and delete components and objects that are not present anymore
            foreach (var obj in oldSceneContent)
            {
                if (!newSceneContent.ContainsKey(obj.Key))
                {
                    Destroy(obj.Value.Item1);
                }
                else
                {
                    foreach (var comp in obj.Value.Item2)
                    {
                        if (!newSceneContent[obj.Key].Item2.ContainsKey(comp.Key))
                        {
                            Destroy(comp.Value);
                        }
                    }
                }
            }

            currentSceneContent = newSceneContent;
        }

        /// <summary>
        /// Load the graph from a file.
        /// </summary>
        [Button("Load Graph from file")]
        private void LoadGraph()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Load Graph", "Assets/Resources", "ttl");
            if (!string.IsNullOrEmpty(path))
            {
                graph = new Graph();
                graph.LoadFromFile(path);
                LoadInstants();
            }
#else
    graph = new Graph();
    graph.LoadFromFile("Assets/Resources/ontology.ttl");
#endif
        }

        /// <summary>
        /// Load the schema from a file.
        /// </summary>
        [Button("Load Schema from file")]
        private void LoadSchema()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Load Schema", "Assets/Resources", "ttl");
            if (!string.IsNullOrEmpty(path))
            {
                schema ??= new Graph();
                schema.LoadFromFile(path);
                StaticRdfsReasoner reasoner = new();
                reasoner.Initialise(schema);
                reasoner.Apply(graph);
            }
#else
    schema = new Graph();
    schema.LoadFromFile("Assets/Resources/schema.ttl");
#endif
        }


        #region Time Management
        /// <summary>
        /// List of instants that can be loaded.
        /// </summary>
        private List<Instant> instants = new();

        /// <summary>
        /// Maximum index of the instants list.
        /// </summary>
        private int MaxInstantIndex => instants.Count - 1;

        /// <summary>
        /// Current index of the instants list.
        /// </summary>
        [SerializeField]
        private int _currentInstantIndex;
        /// <summary>
        /// Current index of the instants list.
        /// </summary>
        public int CurrentInstantIndex
        {
            get => _currentInstantIndex;
            private set
            {
                _currentInstantIndex = value;
                if (instants != null && instants.Count > 0)
                {
                    LoadInstant(instants[_currentInstantIndex]);
                }
            }
        }

        /// <summary>
        /// Load all time:Instant instances from the graph.
        /// </summary>
        private void LoadInstants()
        {
            //sparql query to get all time:Instant instances
            string query = @"
                PREFIX time: <http://www.w3.org/2006/time#>

                SELECT ?instant ?dateTime
                WHERE {
                    ?instant a time:Instant ;
                            time:inXSDDateTime ?dateTime .
                } ORDER BY ?dateTime";

            //execute the query
            SparqlResultSet results = graph.ExecuteQuery(query) as SparqlResultSet;
            instants.Clear();

            //iterate over the results
            foreach (SparqlResult result in results.Cast<SparqlResult>())
            {
                //get the dateTime
                INode dateTimeNode = result["dateTime"];
                //create a new instant
                Instant instant = new(dateTimeNode.AsValuedNode().AsDateTime());
                //add the instant to the list
                instants.Add(instant);
            }
        }
        #endregion
    }
}
