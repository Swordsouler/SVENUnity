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
    public class GraphReader : GraphBuffer
    {
        private Dictionary<string, Tuple<GameObject, Dictionary<string, Component>>> currentSceneContent = new();

        private List<Instant> instants = new();

        private Graph schema;

        private int MaxInstantIndex => instants.Count - 1;


        [SerializeField]
        private int instantIndex;

        public int InstantIndex
        {
            get => instantIndex;
            private set
            {
                instantIndex = value;
                if (instants != null && instants.Count > 0)
                {
                    LoadInstant(instants[instantIndex]);
                }
            }
        }

        private void OnValidate()
        {
            if (instants != null && instants.Count > 0)
            {
                InstantIndex = Mathf.Clamp(InstantIndex, 0, MaxInstantIndex);
            }
            else
            {
                InstantIndex = 0;
            }
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
            InstantIndex = 9;
        }

        private void LoadInstant(Instant instant)
        {
            string query = $@"
                PREFIX time: <http://www.w3.org/2006/time#>
                PREFIX sven: <http://www.sven.fr/ontology#>

                SELECT ?object ?componentType ?component ?propertyName ?propertyType ?propertyIndex ?propertyValue
                WHERE {{
                    ?object a sven:GameObject ;
                            sven:component ?component .
                    ?component a sven:Component ;
                               sven:exactType ?componentExactType ;
                               ?propertyName ?property ;
                               time:hasTemporalExtent ?interval .
                    ?componentExactType sven:unityEngine ?componentType .
                    ?property ?propertyIndex ?propertyValue ;
                              sven:exactType ?propertyExactType .
                    ?propertyExactType sven:unityEngine ?propertyType .
                    ?interval time:inside <{instant.GetUriNode(graph)}> .
                }}";

            SparqlResultSet results = graph.ExecuteQuery(query) as SparqlResultSet;

            // GameObject -> Component -> (ComponentType --- PropertyName -> PropertyIndex -> PropertyValue)
            Dictionary<string, Dictionary<string, Tuple<Type, Dictionary<string, Tuple<Type, Dictionary<string, object>>>>>> sceneContent = new();

            Debug.Log($"Instant: {instant.inXSDDateTime}");
            string[] propertyNameToIgnore = new[] { "hasTemporalExtent", "type", "exactType" };
            string[] propertyIndexToIgnore = new[] { "hasTemporalExtent", "type", "exactType" };
            Assembly unityAssembly = Assembly.Load("UnityEngine");
            foreach (SparqlResult result in results.Cast<SparqlResult>())
            {
                string objectUUID = result["object"].ToString().Split('#')[1];
                string componentUUID = result["component"].ToString().Split('#')[1];
                string componentStringType = result["componentType"].AsValuedNode().AsString();
                string propertyStringType = result["propertyType"].AsValuedNode().AsString();
                string propertyName = result["propertyName"].ToString().Split('#')[1];
                string propertyIndex = result["propertyIndex"].ToString().Split('#')[1];
                string propertyValue = result["propertyValue"].AsValuedNode().AsString();

                if (propertyNameToIgnore.Contains(propertyName)) continue;
                if (propertyIndexToIgnore.Contains(propertyIndex)) continue;

                if (!sceneContent.ContainsKey(objectUUID))
                    sceneContent[objectUUID] = new();

                if (!sceneContent[objectUUID].ContainsKey(componentUUID))
                {
                    Type componentType = componentStringType.Contains("UnityEngine") ?
                        unityAssembly.GetType(componentStringType) :
                        Type.GetType(componentStringType);
                    sceneContent[objectUUID][componentUUID] = new(componentType, new());
                }

                if (!sceneContent[objectUUID][componentUUID].Item2.ContainsKey(propertyName))
                {
                    Type propertyType = propertyStringType.Contains("UnityEngine") ?
                        unityAssembly.GetType(propertyStringType) :
                        Type.GetType(propertyStringType);
                    sceneContent[objectUUID][componentUUID].Item2[propertyName] = new(propertyType, new());
                }

                if (!sceneContent[objectUUID][componentUUID].Item2[propertyName].Item2.ContainsKey(propertyIndex))
                    sceneContent[objectUUID][componentUUID].Item2[propertyName].Item2[propertyIndex] = propertyValue;
            }
            /*
                        foreach (var obj in sceneContent)
                            foreach (var component in obj.Value)
                                foreach (var property in component.Value.Item2)
                                    foreach (var value in property.Value.Item2)
                                        Debug.Log($"{obj.Key} -> {component.Key} -> {component.Value.Item1} -> {property.Key} -> {property.Value.Item1} -> {value.Key} -> {value.Value}");
            */
            // create gameobject for each identified object
            Dictionary<string, Tuple<GameObject, Dictionary<string, Component>>> oldSceneContent = new(currentSceneContent);
            Dictionary<string, Tuple<GameObject, Dictionary<string, Component>>> newSceneContent = new();
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
                            List<string> indexes = SemantizationExtensions.SupportedNestedTypes[propertyType];

                            if (propertyType == typeof(Vector3))
                            {
                                Vector3 newInfo = new(
                                    float.Parse(propertyValues["x"].ToString()),
                                    float.Parse(propertyValues["y"].ToString()),
                                    float.Parse(propertyValues["z"].ToString())
                                );
                                Debug.Log(component.GetType() + " " + propertyName + " " + newInfo);
                                component.GetType().GetProperty(propertyName).SetValue(component, newInfo);
                            }
                            /*Type type = propertyType;
                            object newInfo = Activator.CreateInstance(type);

                            foreach (var prop in type.GetProperties())
                            {
                                if (propertyValues.ContainsKey(prop.Name))
                                {
                                    var value = Convert.ChangeType(propertyValues[prop.Name], prop.PropertyType);
                                    prop.SetValue(newInfo, value);
                                }
                            }

                            Debug.Log(component.GetType() + " " + propertyName + " " + newInfo);
                            component.GetType().GetProperty(propertyName).SetValue(component, newInfo);*/
                            // make an instance of the type and apply the values for each indexes from property1.Value.Item2[index]
                            // for example if we instantiate a UnityEngine.Vector3, we will have to apply the values for x, y and z
                            /*object propertyInstance = Activator.CreateInstance(propertyType);
                            foreach (var index in indexes)
                            {
                                PropertyInfo property = propertyType.GetProperty(index);
                                property?.SetValue(propertyInstance, propertyValues[index]);
                            }
                            Debug.Log($"{obj.Key} -> {comp.Key} -> {component.GetType().Name} -> {propertyName} -> {propertyInstance}");*/
                        }
                        catch (KeyNotFoundException)
                        {
                            // apply the value directly to the property
                            PropertyInfo property = propertyType.GetProperty(propertyName);
                            property?.SetValue(component, property1.Value.Item2["value"]);
                        }
                    }
                }

                // create components for each identified component
                /*foreach (var component in obj.Value)
                {
                    if (components.ContainsKey(component.Key))
                    {
                        // don't delete this component if it exist already
                        componentsToDelete.Remove(component.Key);
                    }
                    else
                    {
                        // instantiate component if it doesn't exist
                        Type componentType = component.Value.Item1.Contains("UnityEngine") ?
                                                    unityAssembly.GetType(component.Value.Item1) :
                                                    Type.GetType(component.Value.Item1);
                        Component componentInstance = gameObject.AddComponent(componentType);
                        components[component.Key] = componentInstance;

                        // make a link between the gameobject and the component
                        gameObjectComponents[gameObjects[obj.Key]].Add(componentInstance);

                        // set properties for each identified property
                        foreach (var property in component.Value.Item2)
                        {
                            // set property value
                            foreach (var value in property.Value)
                            {
                                // set property value
                                //componentInstance.SetPropertyValue(property.Key, value.Key, value.Value);
                            }
                        }
                    }
                }*/
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

        /// <summary>
        /// Load the graph from a file.
        /// </summary>
        [Button("Load Graph")]
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
        [Button("Load Schema")]
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
    }
}
