// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Inference;
using VDS.RDF.Writing;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Threading.Tasks;
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

        public static void SetAuthenticationHeaderValue(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username) + " is null or empty.");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password) + " is null or empty.");
            _authenticationHeaderValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
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

        public static void LoadOntology(string ontologyName, string ontologyFileName)
        {
            if (string.IsNullOrEmpty(ontologyName)) throw new ArgumentNullException(nameof(ontologyName) + " is null or empty.");
            if (string.IsNullOrEmpty(ontologyFileName)) throw new ArgumentNullException(nameof(ontologyFileName) + " is null or empty.");
            if (_ontologies.ContainsKey(ontologyName)) throw new ArgumentException($"Ontology '{ontologyName}' already exists.");
            //Graph ontologyGraph = new();
            TurtleParser turtleParser = new();
            turtleParser.Load(_instance, ontologyFileName);
            //instance.Merge(ontologyGraph);
            _ontologies.Add(ontologyName, ontologyFileName);
        }

        public static void LoadOntologies()
        {
            Dictionary<string, string> ontologies = SvenSettings.Ontologies;
            foreach (KeyValuePair<string, string> ontology in ontologies)
                GraphManager.LoadOntology(ontology.Key, ontology.Value);
        }

        public static void ApplyRules()
        {
            if (_instance == null) throw new InvalidOperationException("Graph instance is not initialized.");

            Graph ontologyGraph = new();
            StaticRdfsReasoner reasoner = new();
            foreach (var ontology in _ontologies)
            {
                try
                {
                    TurtleParser turtleParser = new();
                    turtleParser.Load(ontologyGraph, ontology.Value);
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
            await SaveToEndpoint(SvenSettings.EndpointUrl);
        }

        public static async Task SaveToEndpoint(string endpointUrl)
        {
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
                using UnityWebRequest request = new UnityWebRequest(serviceUrl, UnityWebRequest.kHttpVerbPUT)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(turtleContent))
                    {
                        contentType = writerMimeTypeDefinition.CanonicalMimeType
                    },
                    downloadHandler = new DownloadHandlerBuffer()
                };
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");
                request.SetRequestHeader("Accept", writerMimeTypeDefinition.CanonicalMimeType);

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
                throw new InvalidOperationException($"Failed to save graph to endpoint: {ex.Message}", ex);
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
            string serviceUrl = $"{endpointUrl}?query={Uri.EscapeDataString(query)}&format=text/turtle";
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Authorization = _authenticationHeaderValue;
                SparqlQueryClient sparqlQueryClient = new(httpClient, new Uri(endpointUrl));

#if UNITY_WEBGL
                SparqlResultSet results = await sparqlQueryClient.QueryWebGLWithResultSetAsync(graphQuery);
#else
                SparqlResultSet results = await sparqlQueryClient.QueryWithResultSetAsync(query);
#endif

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
                Debug.Log("Graph loaded from endpoint successfully.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load graph from endpoint: {ex.Message}", ex);
            }
        }

        public static void LoadFromFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) throw new ArgumentNullException(nameof(absolutePath) + " is null or empty.");
            if (!System.IO.Path.IsPathRooted(absolutePath)) throw new ArgumentException("The path must be absolute.", nameof(absolutePath));
            if (!File.Exists(absolutePath)) throw new FileNotFoundException($"File not found: {absolutePath}");
            _instance.LoadFromFile(absolutePath);
        }

        public static SparqlResultSet Query(string query, bool withReasoning)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query) + " is null or empty.");
            if (withReasoning) ApplyRules();
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
    }
}