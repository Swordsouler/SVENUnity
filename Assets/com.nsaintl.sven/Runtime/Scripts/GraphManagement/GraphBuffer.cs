using System;
using System.Collections.Generic;
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


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sven.GraphManagement
{
    /// <summary>
    /// GraphBuffer class to store the RDF data and semantize it.
    /// </summary>
    public class GraphBuffer : GraphBehaviour
    {
        #region Flags

        /// <summary>
        /// Flag to check if the graph has a configuration.
        /// </summary>
        protected bool HasGraphConfig => graphConfig != null;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Name of the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), ShowIf("IsStarted"), HideIf("HasGraphConfig")]
        private string graphName = "default";

        /// <summary>
        /// Name of the graph.
        /// </summary>
        public string GraphName => graphName;

        /// <summary>
        /// Base URI for the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), HideIf("HasGraphConfig")]
        private string baseUri = "http://www.example.com#";

        /// <summary>
        /// Endpoint to send the RDF data.
        /// </summary>
        [SerializeField, DisableIf("IsStarted")]
        private string endpoint = "http://localhost:7200/repositories/Demo-Scene/rdf-graphs/service";

        /// <summary>
        /// List of prefixes to add to the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), HideIf("HasGraphConfig")]
        private List<GraphNamespace> namespaces = new() {
            new GraphNamespace { Name = "rdf", Uri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
            new GraphNamespace { Name = "rdfs", Uri = "http://www.w3.org/2000/01/rdf-schema#" },
            new GraphNamespace { Name = "owl", Uri = "http://www.w3.org/2002/07/owl#" },
            new GraphNamespace { Name = "xsd", Uri = "http://www.w3.org/2001/XMLSchema#" },
        };

        /// <summary>
        /// Storage name of the graph.
        /// </summary>
        public string storageName = "Scene 1";

        /// <summary>
        /// Number of instant created per second.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), Range(1, 60)]
        private int instantPerSecond = 10;

        public int InstantPerSecond => instantPerSecond;

        #endregion

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            // if using a graph configuration, forward the values to the graph properties
            if (HasGraphConfig)
            {
                graphName = graphConfig.Name;
                baseUri = graphConfig.BaseUri;
                namespaces = graphConfig.Namespaces;
            }

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
            await Task.Run(() =>
            {
                Debug.Log("Semantizing to the server... " + graph.Triples.Count + " triples in the graph.");
                MimeTypeDefinition writerMimeTypeDefinition = MimeTypesHelper.GetDefinitions("application/x-turtle").First();
                string turtle = DecodeGraph(graph);
                string serviceUri = endpoint;
                serviceUri = (!(graph.BaseUri != null)) ? (serviceUri + "?default") : (serviceUri + "?graph=" + Uri.EscapeDataString($"{graph.BaseUri.AbsoluteUri}{Uri.EscapeDataString(storageName)}"));
                // decode
                string decodedServiceUri = Uri.UnescapeDataString(serviceUri);

                Debug.Log("Saving the graph to the endpoint " + serviceUri + " " + decodedServiceUri);
                try
                {
                    using HttpClient httpClient = new();
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
        }

        /// <summary>
        /// Save the experiment.
        /// </summary>
        public async void SaveExperiment()
        {
            Debug.Log("Destroying the semantization cores...");
            SemantizationCore[] semantizationCores = FindObjectsByType<SemantizationCore>(FindObjectsSortMode.None);
            SynchronizationContext context = SynchronizationContext.Current;
            await Task.Run(() =>
            {
                foreach (SemantizationCore semantizationCore in semantizationCores)
                    context.Send(_ => semantizationCore.OnDestroy(), null);
            });
            await SaveToEndpoint();
            SaveToFile(graph, $"{Application.dataPath}/../SVENs/{storageName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ttl");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationQuit()
        {
            SaveExperiment();
        }

        /// <summary>
        /// Initialize a new graph with the base URI and prefixes.
        /// </summary>
        /// <returns>Graph.</returns>
        public Graph CreateNewGraph()
        {
            Graph schema = new();
            string content = graphConfig.OntologyContent;
            if (!string.IsNullOrEmpty(content))
            {
                TurtleParser turtleParser = new();
                turtleParser.Load(schema, new StringReader(content));
            }
            return CreateNewGraph(baseUri, namespaces, schema);
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