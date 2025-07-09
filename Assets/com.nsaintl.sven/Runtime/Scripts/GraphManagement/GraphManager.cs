// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using DG.Tweening;
using Sven.Content;
using Sven.GraphManagement.Description;
using Sven.OwlTime;
using Sven.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Inference;
using VDS.RDF.Writing;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

namespace Sven.GraphManagement
{
    public static class GraphManager
    {
        private static readonly Graph _instance = new();
        public static int Count => _instance.Triples.Count;
        private static readonly Dictionary<string, string> _ontologies = new();
        private static readonly List<Instant> _instants = new();
        private static AuthenticationHeaderValue _authenticationHeaderValue = null;
        public static DateTime StartedAt => _instants.Count > 0 ? _instants[0].inXSDDateTime : DateTime.Now;
        public static DateTime EndedAt => _instants.Count > 0 ? _instants[^1].inXSDDateTime : DateTime.Now;
        public static float Duration => (float)(EndedAt - StartedAt).TotalSeconds;
        public static Instant CurrentInstantLoaded { get; private set; } = null;
        public static string BaseUri => _instance.BaseUri?.AbsoluteUri ?? string.Empty;
        public static string GraphName => BaseUri.Split("/")[^2];
        public static bool IsGraphInitialized => HasNamespace("sven");

        public static void SetAuthenticationHeaderValue(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username) + " is null or empty.");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password) + " is null or empty.");
            _authenticationHeaderValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        }

        public static bool HasNamespace(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) throw new ArgumentNullException(nameof(prefix) + " is null or empty.");
            return _instance.NamespaceMap.HasNamespace(prefix);
        }
        public static void Clear()
        {
            _instance.Clear();
            _ontologies.Clear();
            _instants.Clear();
        }

        /// <summary>
        /// Decode the graph to a turtle string.
        /// </summary>
        /// <param name="graph">The graph to decode.</param>
        /// <returns>Decoded graph in turtle format.</returns>
        public static string DecodeGraph(IGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph) + " is null.");
            StringBuilder sb = new();
            CompressingTurtleWriter writer = new(TurtleSyntax.Rdf11Star);
            writer.Save(graph, new System.IO.StringWriter(sb));
            return sb.ToString();
        }
        public static string DecodeGraph()
        {
            return DecodeGraph(_instance);
        }

        public static void SetBaseUri(string baseUri)
        {
            if (string.IsNullOrEmpty(baseUri)) throw new ArgumentNullException(nameof(baseUri) + " is null or empty.");
            _instance.BaseUri = new Uri(baseUri);
        }

        public static void SetNamespace(string prefix, string uri)
        {
            if (string.IsNullOrEmpty(uri)) throw new ArgumentNullException(nameof(uri) + " is null or empty.");
            _instance.NamespaceMap.AddNamespace(prefix, UriFactory.Create(uri));
        }

        public static async Task LoadOntologyAsync(string ontologyName, string ontologyFileName)
        {
            if (string.IsNullOrEmpty(ontologyName)) throw new ArgumentNullException(nameof(ontologyName) + " is null or empty.");
            if (string.IsNullOrEmpty(ontologyFileName)) throw new ArgumentNullException(nameof(ontologyFileName) + " is null or empty.");
            if (_ontologies.ContainsKey(ontologyName)) throw new ArgumentException($"Ontology '{ontologyName}' already exists.");

#if UNITY_WEBGL && !UNITY_EDITOR
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(ontologyFileName))
            {
                await request.SendWebRequest();
                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    throw new Exception("Failed to load ontology: " + request.error);

                string ttlContent = request.downloadHandler.text;
                TurtleParser turtleParser = new();
                using (var reader = new System.IO.StringReader(ttlContent))
                {
                    turtleParser.Load(_instance, reader);
                }
            }
#else
            await Task.Run(() =>
            {
                TurtleParser turtleParser = new();
                turtleParser.Load(_instance, ontologyFileName);
            });
#endif
            _ontologies.Add(ontologyName, ontologyFileName);
        }

        public static async void LoadOntologiesAsync()
        {
            MapppedComponents.LoadAllMappedComponents();
            Dictionary<string, string> ontologies = await SvenSettings.GetOntologiesAsync();
            foreach (KeyValuePair<string, string> ontology in ontologies)
                await LoadOntologyAsync(ontology.Key, ontology.Value);
        }

        public static async Task ApplyRulesAsync()
        {
            if (_instance == null) throw new InvalidOperationException("Graph instance is not initialized.");

            Graph ontologyGraph = new();
            StaticRdfsReasoner reasoner = new();
            foreach (var ontology in _ontologies)
            {
                try
                {
#if UNITY_WEBGL && !UNITY_EDITOR
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(ontology.Value))
            {
                await request.SendWebRequest();
                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    throw new Exception("Failed to load ontology: " + request.error);

                string ttlContent = request.downloadHandler.text;
                TurtleParser turtleParser = new();
                using (var reader = new System.IO.StringReader(ttlContent))
                {
                    turtleParser.Load(ontologyGraph, reader);
                }
            }
#else
                    await Task.Run(() =>
                    {
                        TurtleParser turtleParser = new();
                        turtleParser.Load(ontologyGraph, ontology.Value);
                    });
#endif
                    reasoner.Initialise(ontologyGraph);
                    reasoner.Apply(_instance);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load ontology \"{ontology.Key}\": {ex.Message}", ex);
                }
            }
        }

        public static void SaveToFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) throw new ArgumentNullException(nameof(absolutePath) + " is null or empty.");
            if (!System.IO.Path.IsPathRooted(absolutePath)) throw new ArgumentException("The path must be absolute.", nameof(absolutePath));
            try
            {
                string turtleContent = DecodeGraph();
                File.WriteAllText(absolutePath, turtleContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save graph to file: {ex.Message}", ex);
            }
        }

        public static async Task SaveToEndpoint()
        {
            string endpointUrl = SvenSettings.EndpointUrl;
            Debug.Log($"Saving graph to endpoint: {endpointUrl}");
            if (string.IsNullOrEmpty(endpointUrl)) throw new ArgumentNullException(nameof(endpointUrl) + " is null or empty.");
            if (!Uri.IsWellFormedUriString(endpointUrl, UriKind.Absolute)) throw new ArgumentException("The endpoint URL is not valid.", nameof(endpointUrl));

            MimeTypeDefinition writerMimeTypeDefinition = MimeTypesHelper.GetDefinitions("application/x-turtle").First();
            string turtleContent = DecodeGraph();
            string serviceUrl = $"{endpointUrl}/rdf-graphs/service?graph={Uri.EscapeDataString(_instance.BaseUri.AbsoluteUri)}";
            try
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Authorization = _authenticationHeaderValue;

                HttpRequestMessage request = new(HttpMethod.Put, serviceUrl)
                {
                    Content = new StringContent(turtleContent, Encoding.UTF8, writerMimeTypeDefinition.CanonicalMimeType)
                };

                using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(request);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    Debug.Log("Graph saved to endpoint.");
                    return;
                }

                throw new Exception("Failed to save the graph to the endpoint. " + httpResponseMessage.ReasonPhrase);
#else
                using UnityWebRequest request = new(serviceUrl, UnityWebRequest.kHttpVerbPUT)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(turtleContent))
                    {
                        contentType = writerMimeTypeDefinition.CanonicalMimeType
                    },
                    downloadHandler = new DownloadHandlerBuffer()
                };
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");
                request.SetRequestHeader("Accept", writerMimeTypeDefinition.CanonicalMimeType);
                request.SetRequestHeader("Authorization", _authenticationHeaderValue.ToString());

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Graph saved to endpoint.");
                    return;
                }

                throw new Exception("Failed to save the graph to the endpoint. " + request.error);
#endif
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save graph to endpoint: {ex}", ex);
            }
        }

        public static async void LoadFromEndpoint(string endpointUrl)
        {
            if (string.IsNullOrEmpty(endpointUrl)) throw new ArgumentNullException(nameof(endpointUrl) + " is null or empty.");
            if (!Uri.IsWellFormedUriString(endpointUrl, UriKind.Absolute)) throw new ArgumentException("The endpoint URL is not valid.", nameof(endpointUrl));

            string query = @"
SELECT ?s ?p ?o
WHERE {
    ?s ?p ?o .
} LIMIT 1000000";

            SparqlResultSet results = await QueryEndpoint(endpointUrl, query);

            if (results == null || results.Count == 0)
            {
                Debug.LogWarning("No results found in the graph at the endpoint.");
                return;
            }
            foreach (var result in results)
            {
                INode subject = result["s"];
                INode predicate = result["p"];
                INode @object = result["o"];
                if (subject != null && predicate != null && @object != null)
                {
                    _instance.Assert(new Triple(subject, predicate, @object));
                }
            }
        }

        public static void LoadFromFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) throw new ArgumentNullException(nameof(absolutePath) + " is null or empty.");
            if (!System.IO.Path.IsPathRooted(absolutePath)) throw new ArgumentException("The path must be absolute.", nameof(absolutePath));
            if (!File.Exists(absolutePath)) throw new FileNotFoundException($"File not found: {absolutePath}");
            _instance.LoadFromFile(absolutePath);
        }

        public static async Task<SparqlResultSet> QueryAsync(string query, bool withReasoning)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query) + " is null or empty.");
            if (withReasoning) await ApplyRulesAsync();
            SparqlQueryParser parser = new();
            SparqlQuery sparqlQuery = parser.ParseFromString(query) ?? throw new InvalidOperationException("Failed to parse SPARQL query.");
            try
            {
                return _instance.ExecuteQuery(sparqlQuery) as SparqlResultSet;
            }
            catch (RdfQueryException ex)
            {
                throw new InvalidOperationException($"SPARQL query execution failed: {ex.Message}", ex);
            }
        }


        #region Time Management

        /// <summary>
        /// Current instant.
        /// </summary>
        private static Instant _currentInstant;
        public static Instant CurrentInstant
        {
            get
            {
                // example : instantPerSecond = 10 -> if now = 2021-10-10T10:10:10.0516051 then dateTime will be 2021-10-10T10:10:10.0000000
                DateTime dateTime = FormatDateTime(DateTime.Now);
                if (_currentInstant == null || _currentInstant.inXSDDateTime != dateTime)
                    CurrentInstant = new Instant(dateTime);
                return _currentInstant;
            }
            private set
            {
                if (_currentInstant == value) return;
                _currentInstant = value;
                _currentInstant.Semanticize();
                _instants.Add(value);
            }
        }

        /// <summary>
        /// Format the DateTime to the instantPerSecond.
        /// </summary>
        /// <param name="dateTime">The DateTime to format.</param>
        /// <returns>DateTime.</returns>
        private static DateTime FormatDateTime(DateTime dateTime)
        {
            return new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond / (1000 / SvenSettings.SemanticizeFrequency) * (1000 / SvenSettings.SemanticizeFrequency));
        }

        #endregion

        public static IUriNode Assert(Triple t)
        {
            IUriNode subject = t.Subject as IUriNode ?? throw new ArgumentException("The subject of the triple must be an IUriNode.", nameof(t));
            _instance.Assert(t);
            return subject;
        }

        public static IUriNode CreateUriNode(string uri)
        {
            if (string.IsNullOrEmpty(uri)) throw new ArgumentNullException(nameof(uri) + " is null or empty.");
            return _instance.CreateUriNode(uri);
        }

        public static ILiteralNode CreateLiteralNode(string name)
        {
            if (string.IsNullOrEmpty(name)) name = string.Empty;
            return _instance.CreateLiteralNode(name);
        }

        public static ILiteralNode CreateLiteralNode(string name, Uri uri)
        {
            if (string.IsNullOrEmpty(name)) name = string.Empty;
            if (uri == null) throw new ArgumentNullException(nameof(uri) + " is null.");
            return _instance.CreateLiteralNode(name, uri);
        }

        public static INode CreateTripleNode(Triple triple)
        {
            return _instance.CreateTripleNode(triple);
        }

        public static async Task<SparqlResultSet> QueryEndpoint(string endpointUrl, string query)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return await Task.Run(async () =>
            {
#endif
                if (string.IsNullOrEmpty(endpointUrl)) throw new ArgumentNullException(nameof(endpointUrl) + " is null or empty.");
                if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query) + " is null or empty.");

                Uri endpointUri = new(endpointUrl);
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Authorization = _authenticationHeaderValue;

                SparqlQueryClient sparqlQueryClient = new(httpClient, endpointUri);
                if (SvenSettings.Debug) Debug.Log($"Graph query: {query}");
#if UNITY_WEBGL
                SparqlResultSet results = await sparqlQueryClient.QueryWebGLWithResultSetAsync(query);
#else
                SparqlResultSet results = await sparqlQueryClient.QueryWithResultSetAsync(query);
#endif

                return results;
#if !UNITY_WEBGL || UNITY_EDITOR
            });
#endif
        }

        public static SparqlResultSet QueryMemory(string query)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query) + " is null or empty.");
            SparqlQueryParser parser = new();
            SparqlQuery sparqlQuery = parser.ParseFromString(query) ?? throw new InvalidOperationException("Failed to parse SPARQL query.");
            try
            {
                return _instance.ExecuteQuery(sparqlQuery) as SparqlResultSet;
            }
            catch (RdfQueryException ex)
            {
                throw new InvalidOperationException($"SPARQL query execution failed: {ex.Message}", ex);
            }
        }

        private static void LoadInstants(SparqlResultSet instantsResultSet)
        {
            _instants.Clear();

            //iterate over the results
            foreach (SparqlResult result in instantsResultSet.Cast<SparqlResult>())
            {
                //get the dateTime
                INode dateTimeNode = result["dateTime"];
                //create a new instant
                DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(dateTimeNode.AsValuedNode().AsString());
                //add the instant to the list
                _instants.Add(new(dateTimeOffset.DateTime));
            }
        }

        private static string LoadInstantsQuery => $@"PREFIX : <{BaseUri}>
PREFIX time: <http://www.w3.org/2006/time#>
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>

SELECT ?instant ?dateTime (COUNT(?contentModification) as ?contentModifier)
FROM :
WHERE {{
    ?instant a time:Instant ;
            time:inXSDDateTime ?dateTime .
    ?contentModification sven:hasTemporalExtent ?interval .
    ?interval time:hasBeginning ?instant .
}} GROUP BY ?instant ?dateTime ORDER BY ?dateTime";

        public static async Task LoadInstantsFromEndpoint()
        {
            string endpointUrl = SvenSettings.EndpointUrl;

            SparqlResultSet results = await QueryEndpoint(endpointUrl, LoadInstantsQuery);
            LoadInstants(results);
        }

        public static void LoadInstantsFromMemory()
        {
            SparqlResultSet results = QueryMemory(LoadInstantsQuery);
            LoadInstants(results);
        }

        private static string RetrieveInstantQueryTime(Instant instant)
        {
            return SvenSettings.UseInside ?
                $"?interval time:inside <{instant.UriNode}> ." :
                $@"
    {{
        SELECT DISTINCT ?interval
        WHERE {{
            VALUES ?instantTime {{ {$"\"{instant.inXSDDateTime:yyyy-MM-ddTHH:mm:ss.fffzzz}\""}^^xsd:dateTime }}
            ?interval a time:Interval ;
                    time:hasBeginning/time:inXSDDateTime ?startTime .
            OPTIONAL {{
                ?interval time:hasEnd/time:inXSDDateTime ?_endTime .
            }}
            BIND(IF(BOUND(?_endTime), ?_endTime, NOW()) AS ?endTime)
            FILTER(?startTime <= ?instantTime && ?instantTime < ?endTime)
        }} ORDER BY ?startTime ?endTime limit 10000
    }}";
        }

        private static string RetrieveSceneQuery(Instant instant)
        {
            return $@"PREFIX : <{BaseUri}>
PREFIX time: <http://www.w3.org/2006/time#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

SELECT DISTINCT ?object ?component ?componentType ?property ?propertyName ?propertyNestedName ?propertyValue ?propertyType
FROM :
WHERE {{
    {{
        VALUES ?propertyName {{
            sven:active
            sven:layer
            sven:tag
            sven:name
        }}
        ?object a sven:VirtualObject ;
                ?propertyName ?property .
        ?property sven:value ?propertyValue ;
                    sven:hasTemporalExtent ?interval .
    }}
    UNION
    {{
        ?object a sven:VirtualObject ;
                sven:component ?component .
        ?component sven:exactType ?componentType ;
                ?propertyName ?property .
        ?propertyName rdfs:subPropertyOf* sven:componentProperty ;
                    rdfs:range ?propertyRange .
        ?property sven:exactType ?propertyType ;
                ?propertyNestedName ?propertyValue ;
                sven:hasTemporalExtent ?interval .
        ?propertyNestedName rdfs:subPropertyOf sven:propertyData .
        FILTER(?propertyNestedName != sven:propertyData)
    }}
    {RetrieveInstantQueryTime(instant)}
}}";
        }

        private static async Task<SceneContent> GetSceneContent(SparqlResultSet resultSet)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            SceneContent sceneContent = await Task.Run(() =>
            {
#endif
                SceneContent sceneContent = new();

                foreach (SparqlResult result in resultSet.Cast<SparqlResult>())
                {
                    // get uuids
                    string objectUUID = result["object"].ToString()[(result["object"].ToString().LastIndexOf("/") + 1)..];

                    string propertyName = result["propertyName"].NodeType switch
                    {
                        NodeType.Uri => result["propertyName"].ToString()[(result["propertyName"].ToString().LastIndexOf("#") + 1)..],
                        _ => result["propertyName"].AsValuedNode().AsString()
                    };
                    string componentUUID, componentStringType, propertyStringType, propertyNestedName;
                    try
                    {
                        componentUUID = result["component"].ToString()[(result["component"].ToString().LastIndexOf("/") + 1)..];

                        // get types
                        componentStringType = result["componentType"]?.ToString()[(result["componentType"].ToString().LastIndexOf("#") + 1)..];
                        propertyStringType = result["propertyType"].ToString()[(result["propertyType"].ToString().LastIndexOf("#") + 1)..];
                        propertyNestedName = result["propertyNestedName"].ToString()[(result["propertyNestedName"].ToString().LastIndexOf("#") + 1)..];
                    }
                    catch
                    {
                        if (!sceneContent.GameObjects.ContainsKey(objectUUID))
                            sceneContent.GameObjects[objectUUID] = new(objectUUID);

                        switch (propertyName)
                        {
                            case "active":
                                sceneContent.GameObjects[objectUUID].Active = result["propertyValue"].AsValuedNode().AsString() == "true";
                                continue;
                            case "layer":
                                sceneContent.GameObjects[objectUUID].Layer = result["propertyValue"].AsValuedNode().AsString();
                                continue;
                            case "tag":
                                sceneContent.GameObjects[objectUUID].Tag = result["propertyValue"].AsValuedNode().AsString();
                                continue;
                            case "name":
                                sceneContent.GameObjects[objectUUID].Name = result["propertyValue"].AsValuedNode().AsString();
                                continue;
                        }
                        continue;
                    }

                    // call in main thread
                    Tuple<Type, int> componentData = MapppedComponents.GetData(componentStringType);
                    Type componentType = componentData.Item1;
                    int componentSortOrder = componentData.Item2;
                    if (componentType == null || !MapppedComponents.HasProperty(componentType, propertyName)) continue;
                    //Debug.Log($"Component: {componentType} {propertyName}");

                    Type propertyType = MapppedProperties.GetType(propertyStringType) ?? Type.GetType(propertyStringType);
                    if (!MapppedProperties.HasNestedProperty(propertyType, propertyNestedName)) continue;

                    string propertyUUID = result["property"].ToString()[(result["property"].ToString().LastIndexOf("/") + 1)..];
                    object propertyValue = result["propertyValue"].AsValuedNode().ToValue();
                    //if (propertyName == "position")
                    //Debug.Log(propertyName + " " + propertyNestedName + " " + propertyValue + " " + result["propertyValue"].AsValuedNode());

                    if (!sceneContent.GameObjects.ContainsKey(objectUUID))
                        sceneContent.GameObjects[objectUUID] = new(objectUUID);

                    if (!sceneContent.GameObjects[objectUUID].Components.ContainsKey(componentUUID))
                        sceneContent.GameObjects[objectUUID].Components[componentUUID] = new(componentUUID, componentType, componentSortOrder);

                    if (!sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties.ContainsKey(propertyName))
                        sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName] = new(propertyUUID, propertyName, propertyType);

                    if (!sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values.ContainsKey(propertyNestedName))
                        sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values[propertyNestedName] = propertyValue;
                    else Debug.LogWarning($"Property {propertyNestedName} already exists in {propertyName} of {componentType} in {objectUUID}");
                }
#if !UNITY_WEBGL || UNITY_EDITOR
                return sceneContent;
            });
#endif
            await Task.Yield();

            return sceneContent;
        }

        /// <summary>
        /// Search the instant that is closer previous the duration sent.
        /// </summary>
        /// <param name="duration">Duration to search.</param>
        public static Instant SearchInstant(float duration)
        {
            Instant searchedInstant = _instants.LastOrDefault(x => x.inXSDDateTime <= StartedAt.AddSeconds(duration));
            return searchedInstant;
        }


        public static Instant SearchInstant(DateTime date)
        {
            Instant searchedInstant = _instants.LastOrDefault(x => x.inXSDDateTime <= date);
            if (searchedInstant == null)
            {
                Debug.LogWarning($"No instant found for date {date}. Returning the first instant.");
                searchedInstant = _instants.FirstOrDefault();
            }
            return searchedInstant;
        }

        public static async Task RetrieveSceneFromEndpoint(Instant instant)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            CurrentInstantLoaded = instant;
            if (instant == null) return;
            string endpointUrl = SvenSettings.EndpointUrl;

            stopwatch.Restart();
            SparqlResultSet results = await QueryEndpoint(endpointUrl, RetrieveSceneQuery(instant));
            stopwatch.Stop();
            double queryEndpointElapsed = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            SceneContent targetSceneContent = await GetSceneContent(results);
            ReconstructScene(targetSceneContent);
            stopwatch.Stop();
            double reconstructSceneElapsed = stopwatch.ElapsedMilliseconds;
            double totalElapsed = queryEndpointElapsed + reconstructSceneElapsed;

            ProcessingData processedData = new()
            {
                queryTime = queryEndpointElapsed,
                sceneUpdateTime = reconstructSceneElapsed,
                totalProcessingTime = totalElapsed
            };
            processedDatas.Add(processedData);
        }

        public static async Task RetrieveSceneFromMemory(Instant instant)
        {
            CurrentInstantLoaded = instant;
            if (instant == null) return;

            SparqlResultSet results = QueryMemory(RetrieveSceneQuery(instant));
            SceneContent targetSceneContent = await GetSceneContent(results);
            ReconstructScene(targetSceneContent);
        }

        private static SceneContent GetSceneContent()
        {
            try
            {
                SceneContent sceneContent = new(CurrentInstant);
                // get all semantizationCore objects in the scene
                SemantizationCore[] semantizationCores = UnityEngine.Object.FindObjectsByType<SemantizationCore>(FindObjectsSortMode.None);
                // iterate over the semantizationCores and get their content
                // do the things to fill SceneContent
                foreach (SemantizationCore semantizationCore in semantizationCores)
                {
                    string objectUUID = semantizationCore.GetUUID();
                    if (!sceneContent.GameObjects.ContainsKey(objectUUID))
                    {
                        GameObjectDescription gameObjectDescription = new(objectUUID)
                        {
                            Active = semantizationCore.gameObject.activeSelf,
                            Layer = LayerMask.LayerToName(semantizationCore.gameObject.layer),
                            Tag = semantizationCore.gameObject.tag,
                            Name = semantizationCore.gameObject.name,
                            GameObject = semantizationCore.gameObject
                        };
                        sceneContent.GameObjects[objectUUID] = gameObjectDescription;
                    }

                    List<Component> components = semantizationCore.GetComponents<Component>().ToList();
                    foreach (Component component in components)
                    {
                        string componentUUID = component.GetUUID();
                        if (!sceneContent.GameObjects[objectUUID].Components.ContainsKey(componentUUID))
                        {
                            Tuple<Type, int> componentData = MapppedComponents.GetData(component.GetRdfType());
                            if (componentData == null) continue;
                            Type componentType = componentData.Item1;
                            int componentSortOrder = componentData.Item2;

                            if (!sceneContent.GameObjects[objectUUID].Components.ContainsKey(componentUUID))
                                sceneContent.GameObjects[objectUUID].Components[componentUUID] = new(componentUUID, componentType, componentSortOrder)
                                {
                                    Component = component
                                };

                            Dictionary<string, Tuple<int, Func<object>>> getters = MapppedComponents.GetGetters(component);
                            foreach (KeyValuePair<string, Tuple<int, Func<object>>> getter in getters)
                            {
                                string propertyName = getter.Key;
                                Func<object> getterFunc = getter.Value.Item2;
                                object propertyValue = getterFunc();
                                if (propertyValue == null) continue;
                                Type propertyType = propertyValue.GetType();
                                if (!sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties.ContainsKey(propertyName))
                                    sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName] = new(propertyName, propertyName, propertyType);

                                List<string> nestedProperties = MapppedProperties.GetNestedProperties(propertyValue.GetType());
                                foreach (string nestedProperty in nestedProperties)
                                {
                                    object nestedValue;
                                    if (nestedProperty == "value")
                                    {
                                        nestedValue = propertyValue;
                                    }
                                    else
                                    {
                                        nestedValue = propertyType.GetField(nestedProperty)?.GetValue(propertyValue) ??
                                                      propertyType.GetProperty(nestedProperty)?.GetValue(propertyValue);
                                    }
                                    if (!sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values.ContainsKey(nestedProperty))
                                        sceneContent.GameObjects[objectUUID].Components[componentUUID].Properties[propertyName].Values[nestedProperty] = nestedValue;
                                }
                            }
                        }
                    }
                }
                return sceneContent;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return null;
            }
        }

        private static void ReconstructScene(SceneContent sceneContent)
        {
            SceneContent currentSceneContent = GetSceneContent();
            try
            {
                if (SvenSettings.Debug) Debug.Log(currentSceneContent);
                if (SvenSettings.Debug) Debug.Log(sceneContent);

                foreach (GameObjectDescription gameObjectDescription in sceneContent.GameObjects.Values)
                {
#if UNITY_EDITOR
                    if (!EditorApplication.isPlaying) return;
#endif
                    // create gamobject if it doesn't exist, otherwise get it from the current scene content
                    bool gameObjectExist = currentSceneContent.GameObjects.ContainsKey(gameObjectDescription.UUID);
                    if (gameObjectExist)
                        gameObjectDescription.GameObject = currentSceneContent.GameObjects[gameObjectDescription.UUID].GameObject;
                    else
                    {
                        gameObjectDescription.GameObject = new GameObject(gameObjectDescription.UUID);
                        gameObjectDescription.GameObject.AddComponent<SemantizationCore>().AddUUID(gameObjectDescription.UUID);
                        //gameObjectDescription.GameObject.transform.SetParent(transform);
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

                    List<ComponentDescription> componentDescriptions = gameObjectDescription.Components.Values.ToList();
                    // sort the components by sort order
                    componentDescriptions = componentDescriptions.OrderBy(x => x.SortOrder).ToList();

                    foreach (ComponentDescription componentDescription in componentDescriptions)
                    {
                        // create component if it doesn't exist, otherwise get it from the current scene content
                        bool componentExist = gameObjectExist && currentSceneContent.GameObjects[gameObjectDescription.UUID].Components.ContainsKey(componentDescription.UUID);
                        if (componentExist)
                            componentDescription.Component = currentSceneContent.GameObjects[gameObjectDescription.UUID].Components[componentDescription.UUID].Component;
                        else
                        {
                            // we check transform because it is a special case, it is already attached to the gameObject at instantiation and is unique
                            if (componentDescription.Type == typeof(Transform))
                            {
                                componentDescription.Component = gameObjectDescription.GameObject.transform;
                            }
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
                            componentDescription.Component.AddUUID(componentDescription.UUID);
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
                    if (!sceneContent.GameObjects.ContainsKey(gameObjectDescription.UUID))
                    {
                        foreach (ComponentDescription componentDescription in gameObjectDescription.Components.Values)
                        {
                            DOTween.Kill(componentDescription.Component);
                            if (componentDescription.Type != typeof(Transform))
                                GameObject.Destroy(componentDescription.Component);
                        }
                        GameObject.Destroy(gameObjectDescription.GameObject);
                    }
                    else
                    {
                        foreach (ComponentDescription componentDescription in gameObjectDescription.Components.Values)
                            if (!sceneContent.GameObjects[gameObjectDescription.UUID].Components.ContainsKey(componentDescription.UUID))
                            {
                                DOTween.Kill(componentDescription.Component);
                                if (componentDescription.Type != typeof(Transform))
                                    GameObject.Destroy(componentDescription.Component);
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public static Instant NextInstant()
        {
            if (CurrentInstantLoaded == null) return _instants.Count > 0 ? _instants.First() : CurrentInstantLoaded;

            // next of current instant
            Instant nextInstant = _instants.FirstOrDefault(x => x.inXSDDateTime > CurrentInstantLoaded.inXSDDateTime);
            if (nextInstant != null) return nextInstant;
            // if no next instant, return the current instant
            Debug.LogWarning("No next instant found, returning current instant instead.");
            return CurrentInstantLoaded;
        }

        public static Instant PreviousInstant()
        {
            if (CurrentInstantLoaded == null) return _instants.Count > 0 ? _instants.First() : CurrentInstantLoaded;

            // previous of current instant
            Instant previousInstant = _instants.LastOrDefault(x => x.inXSDDateTime < CurrentInstantLoaded.inXSDDateTime);
            if (previousInstant != null) return previousInstant;
            // if no previous instant, return the current instant
            Debug.LogWarning("No previous instant found, returning current instant instead.");
            return CurrentInstantLoaded;
        }

        public static async Task<string> DownloadTTLFromEndpoint(string enpointUrl)
        {

            string query = $@"PREFIX : <{BaseUri}>
PREFIX time: <http://www.w3.org/2006/time#>
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX geo: <http://www.opengis.net/ont/geosparql#>

CONSTRUCT {{
    ?s ?p ?o .
}}
FROM :
WHERE {{
    ?s ?p ?o .
}}";

            Uri endpointUri = new(enpointUrl);
            HttpClient httpClient = new();

            httpClient.DefaultRequestHeaders.Authorization = _authenticationHeaderValue;

            SparqlQueryClient sparqlQueryClient = new(httpClient, endpointUri);
#if UNITY_WEBGL && !UNITY_EDITOR
            string ttlContent = await sparqlQueryClient.QueryWebGLWithResultTTLAsync(query);
#else
            IGraph resultGraph = await sparqlQueryClient.QueryWithResultGraphAsync(query);
            foreach (string prefix in _instance.NamespaceMap.Prefixes)
            {
                Uri uri = _instance.NamespaceMap.GetNamespaceUri(prefix);
                resultGraph.NamespaceMap.AddNamespace(prefix, uri);
            }

            string ttlContent = DecodeGraph(resultGraph);
#endif
            return ttlContent;
        }


        private static readonly List<ProcessingData> processedDatas = new();
        private class ProcessingData
        {
            public double queryTime;
            public double sceneUpdateTime;
            public double totalProcessingTime;
            public override string ToString()
            {
                return $"Query Time: {queryTime} ms\nScene Update Time: {sceneUpdateTime} ms\nTotal Processing Time: {totalProcessingTime} ms";
            }
        }

        public static void PrintExperimentResults()
        {
            string results = "";

            results += "SPARQL-Sampled: " + processedDatas.Count + "\n";

            results += "SPARQL-Median: " + processedDatas.OrderBy(x => x.queryTime).ElementAt(processedDatas.Count / 2).queryTime + " ms\n";
            results += "SPARQL-Mean: " + processedDatas.Average(x => x.queryTime) + " ms\n";
            results += "SPARQL-Min: " + processedDatas.Min(x => x.queryTime) + " ms\n";
            results += "SPARQL-Max: " + processedDatas.Max(x => x.queryTime) + " ms\n";

            results += "SceneUpdate-Median: " + processedDatas.OrderBy(x => x.sceneUpdateTime).ElementAt(processedDatas.Count / 2).sceneUpdateTime + " ms\n";
            results += "SceneUpdate-Mean: " + processedDatas.Average(x => x.sceneUpdateTime) + " ms\n";
            results += "SceneUpdate-Min: " + processedDatas.Min(x => x.sceneUpdateTime) + " ms\n";
            results += "SceneUpdate-Max: " + processedDatas.Max(x => x.sceneUpdateTime) + " ms\n";

            results += "TotalProcessing-Median: " + processedDatas.OrderBy(x => x.totalProcessingTime).ElementAt(processedDatas.Count / 2).totalProcessingTime + " ms\n";
            results += "TotalProcessing-Mean: " + processedDatas.Average(x => x.totalProcessingTime) + " ms\n";
            results += "TotalProcessing-Min: " + processedDatas.Min(x => x.totalProcessingTime) + " ms\n";
            results += "TotalProcessing-Max: " + processedDatas.Max(x => x.totalProcessingTime) + " ms\n";

            Debug.Log("Experiment results:\n" + results);
        }
    }
}