// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using VDS.RDF;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Sven.Content;
using Sven.OwlTime;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Inference;
using UnityEngine.Networking;
using System.Net.Http.Headers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sven.GraphManagement
{
    /// <summary>
    /// GraphBuffer class to store the RDF data and semantize it.
    /// </summary>
    [AddComponentMenu("Semantic/Graph Buffer")]
    public class GraphBuffer : GraphBehaviour
    {
        #region Flags

        /// <summary>
        /// Flag to check if the graph has a configuration.
        /// </summary>
        protected bool HasGraphConfig => ontologyDescription != null;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Name of the graph.
        /// </summary>
        public string GraphName => ontologyDescription != null ? ontologyDescription.Name : "";

        /// <summary>
        /// Endpoint to send the RDF data.
        /// </summary>
        [DisableIf("IsStarted")]
        public string endpoint = "http://localhost:7200/repositories/Demo-Scene/rdf-graphs/service";

        /// <summary>
        /// Storage name of the graph.
        /// </summary>
        [DisableIf("IsStarted")]
        public string graphName = "GraphName";

        /// <summary>
        /// Number of instant created per second.
        /// </summary>
        [DisableIf("IsStarted"), Range(1, 60)]
        public int instantPerSecond = 10;

        #endregion

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public void Awake()
        {
            // initialize the graph
            graph = CreateNewGraph();
        }

        #region Graph Actions

        /// <summary>
        /// Save the turtle of the graph to a file.
        /// </summary>
        [Button("Save to File")]
        private void SaveToFile()
        {
            SaveToFile(Graph);
        }

        /// <summary>
        /// Save the turtle of the graph to a file.
        /// </summary>
        /// <param name="graph">Graph to save.</param>
        /// <param name="path">Path to save the file.</param>
        private void SaveToFile(Graph graph, string path = null)
        {
            string turtle = DecodeGraph(graph);
            string savePath = path;

#if UNITY_EDITOR
            savePath ??= EditorUtility.SaveFilePanel(
                "Save Turtle File",
                "Assets/Resources",
                "graph",
                "ttl");
#endif
            // Save to the specified path
            if (!string.IsNullOrEmpty(savePath))
            {
                // Create the directory if it does not exist
                string directoryPath = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(savePath, turtle);
            }
        }

        /// <summary>
        /// Save the turtle of the graph to an endpoint.
        /// </summary>
        [Button("Save to Endpoint")]
        private async Task SaveToEndpoint()
        {
            await SaveToEndpoint(Graph);
        }

        /// <summary>
        /// Save the turtle of the graph to an endpoint.
        /// </summary>
        private async Task SaveToEndpoint(Graph graph)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            await Task.Run(() =>
            {
                Debug.Log("Semantizing to the server... " + graph.Triples.Count + " triples in the graph.");
                MimeTypeDefinition writerMimeTypeDefinition = MimeTypesHelper.GetDefinitions("application/x-turtle").First();
                string turtle = DecodeGraph(graph);
                string serviceUri = endpoint;
                serviceUri = (!(graph.BaseUri != null)) ? (serviceUri + "?default") : (serviceUri + "?graph=" + Uri.EscapeDataString($"{graph.BaseUri.AbsoluteUri}{Uri.EscapeDataString(graphName)}"));
                // decode
                string decodedServiceUri = Uri.UnescapeDataString(serviceUri);

                Debug.Log("Saving the graph to the endpoint " + serviceUri + " " + decodedServiceUri);
                try
                {
                    using HttpClient httpClient = new();

                    var byteArray = Encoding.ASCII.GetBytes($"admin:sven-iswc");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    HttpRequestMessage request = new(HttpMethod.Put, serviceUri)
                    {
                        Content = new StringContent(turtle, Encoding.UTF8, writerMimeTypeDefinition.CanonicalMimeType)
                    };
                    using HttpResponseMessage httpResponseMessage = httpClient.SendAsync(request).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        Debug.Log("Graph saved to endpoint.");
                        return;
                    }

                    throw new Exception("Failed to save the graph to the endpoint. " + httpResponseMessage.ReasonPhrase);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to save the graph to the endpoint. " + ex.Message);
                }
            });
#else
            Debug.Log("Semantizing to the server... " + graph.Triples.Count + " triples in the graph.");
            MimeTypeDefinition writerMimeTypeDefinition = MimeTypesHelper.GetDefinitions("application/x-turtle").First();
            string turtle = DecodeGraph(graph);
            string serviceUri = endpoint;
            serviceUri = (!(graph.BaseUri != null)) ? (serviceUri + "?default") : (serviceUri + "?graph=" + Uri.EscapeDataString($"{graph.BaseUri.AbsoluteUri}{Uri.EscapeDataString(graphName)}"));
            // decode
            string decodedServiceUri = Uri.UnescapeDataString(serviceUri);

            Debug.Log("Saving the graph to the endpoint " + serviceUri + " " + decodedServiceUri);
            try
            {
                using UnityWebRequest request = new UnityWebRequest(serviceUri, UnityWebRequest.kHttpVerbPUT)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(turtle))
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
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save the graph to the endpoint. " + ex.Message);
            }
#endif
        }

        /// <summary>
        /// Save the experiment.
        /// </summary>
        public async Task SaveExperiment()
        {
            Debug.Log("Destroying the semantization cores...");
            SemantizationCore[] semantizationCores = FindObjectsByType<SemantizationCore>(FindObjectsSortMode.None);
            SynchronizationContext context = SynchronizationContext.Current;
#if !UNITY_WEBGL || UNITY_EDITOR
            await Task.Run(() =>
            {
#endif
                foreach (SemantizationCore semantizationCore in semantizationCores)
                    context.Send(_ => semantizationCore.OnDestroy(), null);
#if !UNITY_WEBGL || UNITY_EDITOR
            });
#endif

            // apply rule ontology to the graph
            ApplyRuleOntology();

            SaveToFile(graph, $"{Application.dataPath}/../SVENs/{graphName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ttl");
            await SaveToEndpoint();
            /*
            #if UNITY_EDITOR
                        EditorApplication.isPlaying = false;
            #else
                        Application.Quit();
            #endif*/
        }

        private void ApplyRuleOntology()
        {
            StaticRdfsReasoner reasoner = new();
            Graph schema = new();
            string content = ontologyDescription.OntologyContent;
            if (!string.IsNullOrEmpty(content))
            {
                TurtleParser turtleParser = new();
                turtleParser.Load(schema, new StringReader(content));
            }
            graph.Merge(schema);
            reasoner.Initialise(schema);
            reasoner.Apply(graph);
        }

        private void OnApplicationQuit()
        {
            _ = SaveExperiment();
        }

        /// <summary>
        /// Initialize a new graph with the base URI and prefixes.
        /// </summary>
        /// <returns>Graph.</returns>
        public Graph CreateNewGraph()
        {
            Graph schema = new();
            string content = ontologyDescription.OntologyContent;
            if (!string.IsNullOrEmpty(content))
            {
                TurtleParser turtleParser = new();
                turtleParser.Load(schema, new StringReader(content));
            }
            return CreateNewGraph(ontologyDescription.BaseUri, ontologyDescription.Namespaces, schema);
        }

        #endregion

        #region Time Management

        /// <summary>
        /// Current instant.
        /// </summary>
        private Instant currentInstant;
        public Instant CurrentInstant
        {
            get
            {
                // example : instantPerSecond = 10 -> if now = 2021-10-10T10:10:10.0516051 then dateTime will be 2021-10-10T10:10:10.0000000
                DateTime dateTime = FormatDateTime(DateTime.Now);
                if (currentInstant == null || currentInstant.inXSDDateTime != dateTime)
                    CurrentInstant = new Instant(dateTime);
                return currentInstant;
            }
            private set
            {
                currentInstant = value;
                currentInstant.Semantize(graph);
            }
        }

        /// <summary>
        /// Format the DateTime to the instantPerSecond.
        /// </summary>
        /// <param name="dateTime">The DateTime to format.</param>
        /// <returns>DateTime.</returns>
        public DateTime FormatDateTime(DateTime dateTime)
        {
            return new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond / (1000 / instantPerSecond) * (1000 / instantPerSecond));
        }

        #endregion
    }
}