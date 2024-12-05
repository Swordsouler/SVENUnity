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
        #region Scene Content Structure

        /// <summary>
        /// Graph that contains the scene content.
        /// </summary>        
        public class SceneContent
        {
            /// <summary>
            /// Instant of the scene content.
            /// </summary> 
            public Instant Instant { get; set; }
            /// <summary>
            /// Scene content dictionary.
            /// </summary>
            public Dictionary<string, GameObjectDescription> GameObjects { get; set; }

            public SceneContent()
            {
                GameObjects = new();
            }

            public SceneContent(Instant instant)
            {
                Instant = instant;
                GameObjects = new();
            }

            public SceneContent(Dictionary<string, GameObjectDescription> gameObjects)
            {
                GameObjects = gameObjects;
            }

            /// <summary>
            /// ToString method.
            /// </summary>
            /// <returns>String representation of the property description.</returns>
            public override string ToString()
            {
                return $"{Instant.inXSDDateTime}\n{string.Join($"\n", GameObjects.Select(x => $"---------- {x.Key} ----------\n{x.Value}"))}";
            }
        }

        /// <summary>
        /// GameObject description.
        /// </summary>
        public class GameObjectDescription
        {
            /// <summary>
            /// UUID of the GameObject.
            /// </summary>
            public string UUID { get; set; }

            /// <summary>
            /// GameObject.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// Components of the GameObject.
            /// </summary>
            public Dictionary<string, ComponentDescription> Components { get; set; }

            public GameObjectDescription(string uuid)
            {
                UUID = uuid;
                Components = new();
            }

            public GameObjectDescription(string uuid, Dictionary<string, ComponentDescription> components)
            {
                UUID = uuid;
                Components = components;
            }

            public GameObjectDescription(string uuid, GameObject gameObject, Dictionary<string, ComponentDescription> components)
            {
                UUID = uuid;
                GameObject = gameObject;
                Components = components;
            }

            /// <summary>
            /// ToString method.
            /// </summary>
            /// <returns>String representation of the property description.</returns>
            public override string ToString()
            {
                return string.Join("\n", Components.Select(x => $"{x.Key} ({x.Value.Type})\n{x.Value}"));
            }
        }

        /// <summary>
        /// Component description.
        /// </summary>
        public class ComponentDescription
        {
            /// <summary>
            /// UUID of the GameObject.
            /// </summary>
            public string UUID { get; set; }

            /// <summary>
            /// Component.
            /// </summary>
            public Component Component { get; set; }

            /// <summary>
            /// Type of the component.
            /// </summary>
            public Type Type { get; set; }

            /// <summary>
            /// Properties of the component.
            /// </summary>
            public Dictionary<string, PropertyDescription> Properties { get; set; }

            public ComponentDescription(string uuid, Type type)
            {
                UUID = uuid;
                Type = type;
                Properties = new();
            }

            public ComponentDescription(string uuid, Type type, Dictionary<string, PropertyDescription> properties)
            {
                UUID = uuid;
                Type = type;
                Properties = properties;
            }

            public ComponentDescription(string uuid, Component component, Dictionary<string, PropertyDescription> properties)
            {
                UUID = uuid;
                Component = component;
                Properties = properties;
            }

            /// <summary>
            /// ToString method.
            /// </summary>
            /// <returns>String representation of the property description.</returns>
            public override string ToString()
            {
                return string.Join("\n", Properties.Select(x => $"\t{x.Key} ({x.Value.Type}): ({x.Value})"));
            }
        }

        /// <summary>
        /// Property description.
        /// </summary>
        public class PropertyDescription
        {
            /// <summary>
            /// Name of the property.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Type of the property.
            /// </summary>
            public Type Type { get; set; }

            /// <summary>
            /// Values of the property.
            /// </summary>
            public Dictionary<string, object> Values { get; set; }

            /// <summary>
            /// Value of the property.
            /// </summary>
            private object _value = null;
            /// <summary>
            /// Value of the property.
            /// </summary>
            public object Value
            {
                get
                {
                    _value ??= GenerateValue();
                    return _value;
                }
                private set => _value = value;
            }

            public object GenerateValue()
            {
                if (Type == typeof(object))
                {
                    return Values["value"];
                }

                object[] parameters = Values.Select(x => x.Value).ToArray();
                try
                {
                    // try to create an instance of the property directly with the parameters
                    return Activator.CreateInstance(Type, parameters);
                }
                catch (MissingMethodException)
                {
                    // if the constructor is not found, try to create a default instance and set the properties
                    object instance = Activator.CreateInstance(Type);
                    foreach (KeyValuePair<string, object> kvp in Values)
                    {
                        PropertyInfo property = Type.GetProperty(kvp.Key);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(instance, Convert.ChangeType(kvp.Value, property.PropertyType));
                        }
                    }
                    return instance;
                }
            }

            public PropertyDescription(string name, Type type)
            {
                Name = name;
                Type = type;
                Values = new();
            }

            public PropertyDescription(string name, Type type, Dictionary<string, object> values)
            {
                Name = name;
                Type = type;
                Values = values;
            }

            /// <summary>
            /// ToString method.
            /// </summary>
            /// <returns>String representation of the property description.</returns>
            public override string ToString()
            {
                int maxValueSize = 50;
                // x.Value has a limited size of 50 characters
                return string.Join(", ", Values.Select(x => $"{x.Key}: \"{(x.Value.ToString().Length > maxValueSize ? x.Value.ToString()[..maxValueSize] + "..." : x.Value.ToString())}\""));
            }
        }

        #endregion

        /// <summary>
        /// Current scene content.
        /// First string is the UUID of the object.
        /// Second string is the UUID of the component.
        /// </summary>
        private SceneContent currentSceneContent = new();

        /// <summary>
        /// Graph that contains rules
        /// </summary>
        private Graph schema;


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
            SceneContent targetSceneContent = new(instant);

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

                if (!targetSceneContent.GameObjects.ContainsKey(objectUUID))
                    targetSceneContent.GameObjects[objectUUID] = new(objectUUID);

                if (!targetSceneContent.GameObjects[objectUUID].Components.ContainsKey(componentUUID))
                    targetSceneContent.GameObjects[objectUUID].Components[componentUUID] = new(componentUUID, componentType);

                if (!targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties.ContainsKey(propertyName))
                    targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName] = new(propertyName, propertyType);

                if (!targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values.ContainsKey(propertyNestedName))
                    targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values[propertyNestedName] = propertyValue;
                else Debug.LogWarning($"Property {propertyNestedName} already exists in {propertyName} of {componentType} in {objectUUID} at {instant.inXSDDateTime}");
            }

            Debug.Log(targetSceneContent);
            UpdateContent(targetSceneContent);

            // create gameobject for each identified object
            /*Dictionary<string, GameObjectDescription> oldSceneContent = new(currentSceneContent);
            Dictionary<string, GameObjectDescription> newSceneContent = new();

            //todo, manage it with MappedComponents Setter
            foreach (var obj in sceneContent)
            {
                GameObject gameObject;
                if (currentSceneContent.TryGetValue(obj.Key, out var gameObjectComponents))
                {
                    gameObject = gameObjectComponents.GameObject;
                }
                else
                {
                    gameObject = new(obj.Key);
                }
                newSceneContent[obj.Key] = new(gameObject, new());

                // create components for each identified component
                foreach (var comp in obj.Value)
                {
                    if (!(currentSceneContent.ContainsKey(obj.Key) && currentSceneContent[obj.Key].Components.TryGetValue(comp.Key, out Component component)))
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
                    newSceneContent[obj.Key].Components[comp.Key] = component;

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
                    Destroy(obj.Value.GameObject);
                }
                else
                {
                    foreach (var comp in obj.Value.Components)
                    {
                        if (!newSceneContent[obj.Key].Components.ContainsKey(comp.Key))
                        {
                            Destroy(comp.Value);
                        }
                    }
                }
            }

            currentSceneContent = newSceneContent;*/
        }

        /// <summary>
        /// Compare the target scene content with the current scene content and update it accordingly to modifications
        /// </summary>
        /// <param name="targetSceneContent">The target scene content to compare with the current content</param>
        private void UpdateContent(SceneContent targetSceneContent)
        {
            foreach (GameObjectDescription gameObjectDescription in targetSceneContent.GameObjects.Values)
            {
                // create gamobject if it doesn't exist, otherwise get it from the current scene content
                bool gameObjectExist = currentSceneContent.GameObjects.ContainsKey(gameObjectDescription.UUID);
                if (gameObjectExist)
                    gameObjectDescription.GameObject = currentSceneContent.GameObjects[gameObjectDescription.UUID].GameObject;
                else
                    gameObjectDescription.GameObject = new GameObject(gameObjectDescription.UUID);

                foreach (ComponentDescription componentDescription in gameObjectDescription.Components.Values)
                {
                    // create component if it doesn't exist, otherwise get it from the current scene content
                    bool componentExist = gameObjectExist && currentSceneContent.GameObjects[gameObjectDescription.UUID].Components.ContainsKey(componentDescription.UUID);
                    if (componentExist)
                        componentDescription.Component = currentSceneContent.GameObjects[gameObjectDescription.UUID].Components[componentDescription.UUID].Component;
                    else
                    {
                        // we check transform because it is a special case, it is already attached to the gameObject at instantiation and is unique
                        if (componentDescription.Type == typeof(Transform))
                            componentDescription.Component = gameObjectDescription.GameObject.transform;
                        else
                        {
                            componentDescription.Component = gameObjectDescription.GameObject.AddComponent(componentDescription.Type);
                            //temporary to see object
                            if (componentDescription.Component is MeshRenderer meshRenderer)
                                meshRenderer.material = new Material(Shader.Find("Standard"));
                            if (componentDescription.Component is MeshFilter meshFilter)
                                meshFilter.mesh = Resources.Load<Mesh>("Models/Pyramid");
                        }
                    }

                    // get the setters of the component
                    Dictionary<string, Action<object>> setters = MapppedComponents.GetSetters(componentDescription.Component);
                    // print every action :
                    foreach (var setter in setters)
                    {
                        Debug.Log($"Setter: {setter.Key} {setter.Value}");
                    }
                    foreach (PropertyDescription propertyDescription in componentDescription.Properties.Values)
                    {
                        object propertyValue = propertyDescription.Value;
                        if (propertyValue == null)
                        {
                            Debug.LogWarning($"Property {propertyDescription} is null in {componentDescription.Type} of {gameObjectDescription.UUID}");
                            continue;
                        }

                        Debug.Log($"Property: {propertyDescription.Name} {propertyDescription.Type} {propertyValue}");
                        if (setters.TryGetValue(propertyDescription.Name, out var setter) && setter != null) setter(propertyValue);
                        else Debug.LogWarning($"Setter not found for {propertyDescription.Type} in {componentDescription.Type} of {gameObjectDescription.UUID}");
                    }
                }
            }

            foreach (GameObjectDescription gameObjectDescription in currentSceneContent.GameObjects.Values)
            {
                if (!targetSceneContent.GameObjects.ContainsKey(gameObjectDescription.UUID))
                    Destroy(gameObjectDescription.GameObject);

                foreach (ComponentDescription componentDescription in gameObjectDescription.Components.Values)
                {
                    if (!targetSceneContent.GameObjects[gameObjectDescription.UUID].Components.ContainsKey(componentDescription.UUID))
                        Destroy(componentDescription.Component);
                }
            }

            currentSceneContent = targetSceneContent;
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
        [SerializeField, MinValue(0), OnValueChanged("LoadCurrentInstant")]
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
                LoadCurrentInstant();
            }
        }

        /// <summary>
        /// Load the current instant. (Work as a validation)
        /// </summary>
        private void LoadCurrentInstant()
        {
            _currentInstantIndex = Mathf.Clamp(_currentInstantIndex, 0, MaxInstantIndex);
            if (instants != null && instants.Count > 0)
            {
                LoadInstant(instants[CurrentInstantIndex]);
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
