using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using Sven.OwlTime;
using Sven.Content;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Inference;
using VDS.RDF.Update;
using Sven.Utils;
using System.IO;

namespace Sven.GraphManagement
{
    /// <summary>
    /// Graph reader to load the scene content from the graph.
    /// </summary>
    [AddComponentMenu("Semantic/Graph Reader")]
    public class GraphReader : GraphBehaviour
    {
        #region Flags

        /// <summary>
        /// Is graph loaded.
        /// </summary>
        public bool IsGraphLoaded => instants.Count > 0;

        /// <summary>
        /// Reading mode of the graph.
        /// </summary>
        [SerializeField, DisableIf("IsGraphLoaded")]
        protected GraphStorageMode _readingMode = GraphStorageMode.Local;

        /// <summary>
        /// Graph that contains the scene content.
        /// </summary>
        protected bool IsLocal => _readingMode == GraphStorageMode.Local;
        /// <summary>
        /// Graph that contains the scene content.
        /// </summary>
        protected bool IsRemote => _readingMode == GraphStorageMode.Remote;

        /// <summary>
        /// Game has started.
        /// </summary>
        private bool GameHasStarted => _gameHasStarted;

        #endregion

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
            /// Active state of the GameObject.
            /// </summary>
            public bool Active;

            /// <summary>
            /// Layer of the GameObject.
            /// </summary>
            public string Layer;

            /// <summary>
            /// Tag of the GameObject.
            /// </summary>
            public string Tag;

            /// <summary>
            /// Name of the GameObject.
            /// </summary>
            public string Name;

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
            /// UUID of the Component.
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
            /// UUID of the Property.
            /// </summary>
            public string UUID { get; set; }

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

            /// <summary>
            /// Generate the value of the property.
            /// </summary>
            /// <returns>Value of the property.</returns>
            public object GenerateValue()
            {
                if (Type == typeof(object))
                {
                    return Values["value"];
                }


                try
                {
                    // try to create an instance of the property directly with the parameters and constructor
                    ConstructorInfo constructor = Type.GetConstructors()
                                          .OrderByDescending(c => c.GetParameters().Length)
                                          .FirstOrDefault();

                    ParameterInfo[] parameterInfos = constructor.GetParameters();
                    object[] orderedParameters = new object[parameterInfos.Length];
                    object[] parameters = Values.Select(x => x.Value).ToArray();

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        var paramInfo = parameterInfos[i];
                        var value = Values.FirstOrDefault(v => v.Key == paramInfo.Name).Value;
                        orderedParameters[i] = value ?? throw new InvalidOperationException($"No value provided for parameter '{paramInfo.Name}'.");
                    }

                    return Activator.CreateInstance(Type, orderedParameters);
                }
                catch (MissingMethodException)
                {
                    // if the constructor is not found, try to create a default instance and set the properties
                    object instance = Activator.CreateInstance(Type);
                    foreach (KeyValuePair<string, object> kvp in Values)
                    {
                        PropertyInfo property = Type.GetProperty(kvp.Key);
                        property?.SetValue(instance, Convert.ChangeType(kvp.Value, property.PropertyType));
                        if (property != null) continue;

                        //try in fields
                        FieldInfo field = Type.GetField(kvp.Key);
                        field?.SetValue(instance, Convert.ChangeType(kvp.Value, field.FieldType));
                    }
                    return instance;
                }
            }

            public PropertyDescription(string uuid, string name, Type type)
            {
                UUID = uuid;
                Name = name;
                Type = type;
                Values = new();
            }

            public PropertyDescription(string uuid, string name, Type type, Dictionary<string, object> values)
            {
                UUID = uuid;
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

        /// <summary>
        /// Graph that contains the scene content.
        /// </summary>
        private bool _gameHasStarted = false;

        private void Awake()
        {
            _gameHasStarted = true;
            if (ontologyDescription != null)
            {
                schema = new Graph();
                string content = ontologyDescription.OntologyContent;
                if (!string.IsNullOrEmpty(content))
                {
                    TurtleParser turtleParser = new();
                    turtleParser.Load(schema, new StringReader(content));
                }
                CreateNewGraph(ontologyDescription.BaseUri, ontologyDescription.Namespaces, schema);
            }
            if (IsRemote) LoadFromEndpoint();
        }

        /// <summary>
        /// Delete all the shapes properties in the graph.
        /// </summary>
        private void DeleteShape()
        {
            //amount of triples
            Debug.Log(graph.Triples.Count);

            InMemoryDataset dataset = new(graph);
            ISparqlUpdateProcessor updateProcessor = new LeviathanUpdateProcessor(dataset);
            SparqlUpdateCommandSet updateCommandSet = new SparqlUpdateParser().ParseFromString($@"
                PREFIX time: <http://www.w3.org/2006/time#>
                PREFIX sven: <http://www.sven.fr/>

                DELETE {{
                    ?object sven:component ?shape .
                    ?shape ?propertyName ?property .
                    ?property ?propertyNestedName ?propertyValue .
                }} WHERE {{
                    ?object a sven:Object ;
                            sven:component ?shape .
                    ?shape sven:exactType sven:Shape .
                    ?shape ?propertyName ?property .
                    OPTIONAL {{
                        ?property ?propertyNestedName ?propertyValue .
                    }}
                }}");

            updateProcessor.ProcessCommandSet(updateCommandSet);

            Debug.Log(graph.Triples.Count);
        }

        /// <summary>
        /// Load the instants from the graph.
        /// </summary>
        private bool _isReadingInstant = false;

        /// <summary>
        /// Load the instants from the graph.
        /// </summary>
        /// <param name="instant">Instant to load.</param>
        private async void LoadInstant(Instant instant)
        {
            if (_isReadingInstant) return;
            _isReadingInstant = true;
            try
            {
                DateTime startProcessing = DateTime.Now;

                // with dateTime
                string intervalProcessing = $@"{{
                    SELECT DISTINCT ?interval
                    WHERE {{
                        VALUES ?instantTime {{ {$"\"{instant.inXSDDateTime:yyyy-MM-ddTHH:mm:ss.fffzzz}\""}^^xsd:dateTime }}
                        ?interval a time:Interval ;
                                time:hasBeginning ?start .
                        ?start time:inXSDDateTime ?startTime .
                        OPTIONAL {{
                            ?interval time:hasEnd ?end .
                            ?end time:inXSDDateTime ?_endTime .
                        }}
                        BIND(IF(BOUND(?_endTime), ?_endTime, NOW()) AS ?endTime)
                        FILTER(?startTime <= ?instantTime && ?instantTime < ?endTime)
                    }} ORDER BY ?startTime ?endTime limit 10000
                }}";

                // with inside
                //string intervalProcessing = $@"?interval time:inside <{instant.GetUriNode(graph)}> .";

                string query = $@"
                    PREFIX time: <http://www.w3.org/2006/time#>
                    PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                    PREFIX sven: <http://www.sven.fr/>
                    PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

                    SELECT DISTINCT ?object ?component ?componentType ?property ?propertyName ?propertyNestedName ?propertyValue ?propertyType
                    WHERE {{
                        {{
                            SELECT *
                            WHERE {{
                                VALUES ?propertyName {{
                                    sven:active
                                    sven:layer
                                    sven:tag
                                    sven:name
                                }}
                                ?object a sven:VirtualObject ;
                                        ?propertyName ?property .
                                ?property sven:value ?propertyValue ;
                                            time:hasTemporalExtent ?interval .
                            }}
                        }}
                        UNION
                        {{
                            SELECT *
                            WHERE {{
                                ?object a sven:VirtualObject ;
                                        sven:component ?component .
                                ?component sven:exactType ?componentType ;
                                        ?propertyName ?property .
                                ?propertyName rdfs:subPropertyOf* sven:componentProperty ;
                                            rdfs:range ?propertyRange .
                                ?property sven:exactType ?propertyType ;
                                        ?propertyNestedName ?propertyValue ;
                                        time:hasTemporalExtent ?interval .
                                ?propertyNestedName rdfs:subPropertyOf sven:propertyData .
                                FILTER(?propertyNestedName != sven:propertyData)
                            }}
                        }}
                        {intervalProcessing}
                    }}";

                // Execute the query
                SparqlResultSet results = await Request(query);
                double queryTime = (DateTime.Now - startProcessing).TotalMilliseconds;

                // GameObject -> Component -> (ComponentType --- PropertyName -> PropertyIndex -> PropertyValue)
                SceneContent targetSceneContent = await Task.Run(() =>
                {
                    SceneContent targetSceneContent = new(instant);

                    foreach (SparqlResult result in results.Cast<SparqlResult>())
                    {
                        // get uuids
                        string objectUUID = result["object"].ToString()[(result["object"].ToString().LastIndexOf("/") + 1)..];

                        string propertyName = result["propertyName"].NodeType switch
                        {
                            NodeType.Uri => result["propertyName"].ToString()[(result["propertyName"].ToString().LastIndexOf("/") + 1)..],
                            _ => result["propertyName"].AsValuedNode().AsString()
                        };
                        string componentUUID, componentStringType, propertyStringType, propertyNestedName;
                        try
                        {
                            componentUUID = result["component"].ToString()[(result["component"].ToString().LastIndexOf("/") + 1)..];

                            // get types
                            componentStringType = result["componentType"]?.ToString()[(result["componentType"].ToString().LastIndexOf("/") + 1)..];
                            propertyStringType = result["propertyType"].ToString()[(result["propertyType"].ToString().LastIndexOf("/") + 1)..];
                            propertyNestedName = result["propertyNestedName"].ToString()[(result["propertyNestedName"].ToString().LastIndexOf("/") + 1)..];
                        }
                        catch
                        {
                            if (!targetSceneContent.GameObjects.ContainsKey(objectUUID))
                                targetSceneContent.GameObjects[objectUUID] = new(objectUUID);

                            switch (propertyName)
                            {
                                case "active":
                                    targetSceneContent.GameObjects[objectUUID].Active = result["propertyValue"].AsValuedNode().AsString() == "true";
                                    continue;
                                case "layer":
                                    targetSceneContent.GameObjects[objectUUID].Layer = result["propertyValue"].AsValuedNode().AsString();
                                    continue;
                                case "tag":
                                    targetSceneContent.GameObjects[objectUUID].Tag = result["propertyValue"].AsValuedNode().AsString();
                                    continue;
                                case "name":
                                    targetSceneContent.GameObjects[objectUUID].Name = result["propertyValue"].AsValuedNode().AsString();
                                    continue;
                            }
                            continue;
                        }


                        Type componentType = MapppedComponents.GetType(componentStringType);// ?? Type.GetType(componentStringType);
                        if (componentType == null || !MapppedComponents.HasProperty(componentType, propertyName)) continue;
                        //Debug.Log($"Component: {componentType} {propertyName}");

                        Type propertyType = MapppedProperties.GetType(propertyStringType) ?? Type.GetType(propertyStringType);
                        if (!MapppedProperties.HasNestedProperty(propertyType, propertyNestedName)) continue;

                        string propertyUUID = result["property"].ToString()[(result["property"].ToString().LastIndexOf("/") + 1)..];
                        object propertyValue = result["propertyValue"].AsValuedNode().ToValue();
                        //if (propertyName == "position")
                        //Debug.Log(propertyName + " " + propertyNestedName + " " + propertyValue + " " + result["propertyValue"].AsValuedNode());

                        if (!targetSceneContent.GameObjects.ContainsKey(objectUUID))
                            targetSceneContent.GameObjects[objectUUID] = new(objectUUID);

                        if (!targetSceneContent.GameObjects[objectUUID].Components.ContainsKey(componentUUID))
                            targetSceneContent.GameObjects[objectUUID].Components[componentUUID] = new(componentUUID, componentType);

                        if (!targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties.ContainsKey(propertyName))
                            targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName] = new(propertyUUID, propertyName, propertyType);

                        if (!targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values.ContainsKey(propertyNestedName))
                            targetSceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values[propertyNestedName] = propertyValue;
                        else Debug.LogWarning($"Property {propertyNestedName} already exists in {propertyName} of {componentType} in {objectUUID} at {instant.inXSDDateTime}");
                    }
                    return targetSceneContent;
                });
                if (SvenHelper.Debug) Debug.Log(targetSceneContent);
                UpdateContent(targetSceneContent);

                DateTime endProcessing = DateTime.Now;
                double sceneUpdateTime = (endProcessing - startProcessing).TotalMilliseconds - queryTime;
                if (SvenHelper.Debug)
                {
                    Debug.Log($"Query Time: {queryTime} ms");
                    Debug.Log($"Scene Update Time: {sceneUpdateTime} ms");
                    Debug.Log($"Processing Time: {(endProcessing - startProcessing).TotalMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred: {ex.Message}");
            }
            _isReadingInstant = false;
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
                {
                    gameObjectDescription.GameObject = new GameObject(gameObjectDescription.UUID);
                    gameObjectDescription.GameObject.transform.SetParent(transform);
                }
                gameObjectDescription.GameObject.SetActive(gameObjectDescription.Active);
                gameObjectDescription.GameObject.layer = LayerMask.NameToLayer(gameObjectDescription.Layer);
                try
                {
                    bool isTagExist = !string.IsNullOrEmpty(gameObjectDescription.Tag);
                    gameObjectDescription.GameObject.tag = isTagExist ? gameObjectDescription.Tag ?? "Untagged" : "Untagged";
                }
                catch (Exception)
                {
                    gameObjectDescription.GameObject.tag = "Untagged";
                }
                gameObjectDescription.GameObject.name = gameObjectDescription.Name;

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
                            try
                            {
                                //Debug.Log(componentDescription.Type);
                                componentDescription.Component = gameObjectDescription.GameObject.AddComponent(componentDescription.Type);

                                // Default initialization
                                if (componentDescription.Component is MeshRenderer meshRenderer)
                                    meshRenderer.material = new Material(Shader.Find("Standard"));
                                if (componentDescription.Component is MeshFilter meshFilter)
                                    meshFilter.mesh = new Mesh();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Error while adding component {componentDescription.Type} to {gameObjectDescription.UUID}: {ex.Message}");
                            }
                        }
                    }

                    // get the setters of the component item1 = priority, item2 = setter
                    Dictionary<string, Tuple<int, Action<object>>> setters = MapppedComponents.GetSetters(componentDescription.Component);
                    //reorder the properties by priority
                    componentDescription.Properties = componentDescription.Properties.OrderBy(x => setters[x.Key].Item1).ToDictionary(x => x.Key, x => x.Value);
                    //if (componentDescription.Component.GetType() == typeof(MeshFilter)) continue;

                    foreach (PropertyDescription propertyDescription in componentDescription.Properties.Values)
                    {
                        // check if the property state exists in the current scene content (by checking the uuid)
                        bool propertyExist = componentExist && currentSceneContent.GameObjects[gameObjectDescription.UUID].Components[componentDescription.UUID].Properties.TryGetValue(propertyDescription.Name, out var currentPropertyDescription) && currentPropertyDescription.UUID == propertyDescription.UUID;
                        if (propertyExist) continue;

                        object propertyValue = propertyDescription.Value;
                        if (propertyValue == null)
                        {
                            Debug.LogWarning($"Property {propertyDescription} is null in {componentDescription.Type} of {gameObjectDescription.UUID}");
                            continue;
                        }
                        //if (componentDescription.Component.GetType() == typeof(MeshRenderer))
                        //    Debug.Log($"Property: {propertyDescription.Name} {propertyDescription.Type} {propertyValue.GetType()}  {propertyValue}");

                        if (setters.TryGetValue(propertyDescription.Name, out var setter) && setter.Item2 != null)
                        {
                            setter.Item2(propertyValue);
                        }
                        //else Debug.LogWarning($"Setter not found for {propertyDescription.Type} in {componentDescription.Type} of {gameObjectDescription.UUID}");
                    }
                }
            }

            // remove the gameobjects and components that are not in the target scene content
            foreach (GameObjectDescription gameObjectDescription in currentSceneContent.GameObjects.Values)
            {
                if (!targetSceneContent.GameObjects.ContainsKey(gameObjectDescription.UUID))
                {
                    foreach (ComponentDescription componentDescription in gameObjectDescription.Components.Values)
                    {
                        DOTween.Kill(componentDescription.Component);
                        if (componentDescription.Type != typeof(Transform))
                            Destroy(componentDescription.Component);
                    }
                    Destroy(gameObjectDescription.GameObject);
                }
                else
                {
                    foreach (ComponentDescription componentDescription in gameObjectDescription.Components.Values)
                        if (!targetSceneContent.GameObjects[gameObjectDescription.UUID].Components.ContainsKey(componentDescription.UUID))
                        {
                            DOTween.Kill(componentDescription.Component);
                            if (componentDescription.Type != typeof(Transform))
                                Destroy(componentDescription.Component);
                        }
                }
            }

            currentSceneContent = targetSceneContent;
        }

        /// <summary>
        /// Load the graph from a file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <exception cref="ArgumentNullException">If the path is null.</exception>
        private void LoadGraph(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            graph = new Graph();
            graph.LoadFromFile(path);
            ApplyCurrentSchema();
            LoadInstants();
        }

        private void ApplyCurrentSchema()
        {
            StaticRdfsReasoner reasoner = new();
            reasoner.Initialise(schema);
            graph ??= new();
            reasoner.Apply(graph);
            graph.Merge(schema);
        }

        /// <summary>
        /// Load the graph from a file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <exception cref="ArgumentNullException">If the path is null.</exception>
        private void LoadSchema(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            schema = new Graph();
            schema.LoadFromFile(path);
            ApplyCurrentSchema();
        }

        /// <summary>
        /// Load the graph from a file.
        /// </summary>
        [Button("Load Graph from file"), ShowIf("IsLocal")]
        private void LoadGraphFromFile()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Load Graph", "Assets/Resources", "ttl");
            if (!string.IsNullOrEmpty(path))
            {
                LoadGraph(path);
            }
#else
    graph = new Graph();
    graph.LoadFromFile("Assets/Resources/ontology.ttl");
#endif
        }

        /// <summary>
        /// Load the schema from a file.
        /// </summary>
        //[Button("Load Schema from file"), ShowIf("IsLocal")]
        private void LoadSchemaFromFile()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Load Schema", "Assets/Resources", "ttl");
            if (!string.IsNullOrEmpty(path))
            {
                LoadSchema(path);
            }
#else
    schema = new Graph();
    schema.LoadFromFile("Assets/Resources/schema.ttl");
#endif
        }

        /// <summary>
        /// Endpoint of the graph.
        /// </summary>
        [SerializeField, ShowIf("IsRemote")]
        private string _endpoint;

        /// <summary>
        /// Storage name of the graph.
        /// </summary>
        [SerializeField, ShowIf("IsRemote")]
        public string _storageName = "Scene 1";

        /// <summary>
        /// Loaded endpoint.
        /// </summary>
        private string _loadedEndpoint;

        [SerializeField, ShowIf("IsRemote")]
        private void LoadFromEndpoint(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            _loadedEndpoint = _endpoint = url;
            LoadInstants();
        }

        /// <summary>
        /// Load the graph from an endpoint.
        /// </summary>
        [Button("Load from Endpoint"), ShowIf("IsRemote")]
        private void LoadFromEndpoint()
        {
            LoadFromEndpoint(_endpoint);
        }

        /// <summary>
        /// Action to be executed when the graph is loaded.
        /// </summary>
        public Action OnGraphLoaded;


        #region Time Management

        /// <summary>
        /// Instant class, but with amount of content modifier.
        /// </summary>
        public class InstantDescription : Instant
        {
            /// <summary>
            /// Amount of properties/components/objects that has started at this instant.
            /// </summary>
            public int ContentModifier { get; set; }

            public InstantDescription(DateTime dateTime, int contentModifier) : base(dateTime)
            {
                ContentModifier = contentModifier;
            }
        }


        /// <summary>
        /// List of instants that can be loaded.
        /// </summary>
        private readonly List<InstantDescription> instants = new();
        public List<InstantDescription> Instants => instants;

        /// <summary>
        /// Maximum index of the instants list.
        /// </summary>
        public int MaxInstantIndex => instants.Count - 1;

        public DateTime StartedAt => instants[0].inXSDDateTime;
        public DateTime EndedAt => instants[^1].inXSDDateTime;
        public float Duration => (float)(EndedAt - StartedAt).TotalSeconds;
        public int MeanContentModifier => instants.Sum(x => x.ContentModifier) / instants.Count;

        /// <summary>
        /// Current index of the instants list.
        /// </summary>
        [SerializeField, MinValue(0), OnValueChanged("LoadCurrentInstant"), ShowIf("IsGraphLoaded")]
        private int _currentInstantIndex;
        /// <summary>
        /// Current index of the instants list.
        /// </summary>
        public int CurrentInstantIndex
        {
            get => _currentInstantIndex;
            private set
            {
                if (_currentInstantIndex == value) return;
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

        private async Task<SparqlResultSet> Request(string query)
        {
            return await Task.Run(async () =>
            {
                if (IsLocal) return LocalRequest(query);
                else if (IsRemote) return await RemoteRequest(query);
                return null;
            });
        }

        private SparqlResultSet LocalRequest(string query)
        {
            SparqlQuery sparqlQuery = new SparqlQueryParser().ParseFromString(query);
            return graph.ExecuteQuery(sparqlQuery) as SparqlResultSet;
        }

        private async Task<SparqlResultSet> RemoteRequest(string query)
        {
            // URL de votre endpoint GraphDB
            Uri endpointUri = new(_loadedEndpoint);

            // Créez une instance de HttpClient
            HttpClient httpClient = new();

            // Créez une instance de SparqlQueryClient avec HttpClient et l'URI de l'endpoint
            SparqlQueryClient sparqlQueryClient = new(httpClient, endpointUri);

            string graphUri = $"FROM <{graph.BaseUri.AbsoluteUri}{Uri.EscapeDataString(_storageName)}>";
            int selectIndex = query.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (selectIndex == -1) throw new Exception("Query must contain a SELECT statement.");
            int insertIndex = query.IndexOf('\n', selectIndex) + 1;
            string graphQuery = query.Insert(insertIndex, $"{graphUri}\n");

            // Exécutez la requête SPARQL
            SparqlResultSet results = await sparqlQueryClient.QueryWithResultSetAsync(graphQuery).ConfigureAwait(false);

            return results;
        }

        /// <summary>
        /// Load all time:Instant instances from the graph.
        /// </summary>
        private async void LoadInstants()
        {
            string query = @"
                PREFIX time: <http://www.w3.org/2006/time#>

                SELECT ?instant ?dateTime (COUNT(?contentModification) as ?contentModifier)
                WHERE {
                    ?instant a time:Instant ;
                            time:inXSDDateTime ?dateTime .
                    ?contentModification time:hasTemporalExtent ?interval .
                    ?interval time:hasBeginning ?instant .
                } GROUP BY ?instant ?dateTime ORDER BY ?dateTime";

            SparqlResultSet results = await Request(query);
            instants.Clear();

            //iterate over the results
            foreach (SparqlResult result in results.Cast<SparqlResult>())
            {
                //get the dateTime
                INode dateTimeNode = result["dateTime"];
                int contentModifier = (int)result["contentModifier"].AsValuedNode().AsInteger();
                //create a new instant
                DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(dateTimeNode.AsValuedNode().AsString());
                InstantDescription instant = new(dateTimeOffset.DateTime, contentModifier);
                //add the instant to the list
                instants.Add(instant);
            }
            CurrentInstantIndex = 0;
            OnGraphLoaded?.Invoke();
        }

        /// <summary>
        /// Search the instant that is closer previous the duration sent.
        /// </summary>
        /// <param name="duration">Duration to search.</param>
        public void SearchAt(float duration)
        {
            for (int i = 0; i < instants.Count; i++)
            {
                if (instants[i].inXSDDateTime > StartedAt.AddSeconds(duration))
                {
                    CurrentInstantIndex = i - 1;
                    return;
                }
            }

            // If no element satisfies the condition, set the last element as the CurrentInstantIndex
            CurrentInstantIndex = instants.Count - 1;
        }

        /// <summary>
        /// Get the next instant.
        /// </summary>
        /// <returns>The next instant.</returns>
        public Instant NextInstant()
        {
            if (CurrentInstantIndex < MaxInstantIndex)
                return instants[++CurrentInstantIndex];
            return null;
        }

        /// <summary>
        /// Get the previous instant.
        /// </summary>
        /// <returns>The previous instant.</returns>
        public Instant PreviousInstant()
        {
            if (CurrentInstantIndex > 0)
                return instants[--CurrentInstantIndex];
            return null;
        }
        #endregion
    }
}
