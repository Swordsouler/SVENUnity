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
        [SerializeField, DisableIf("IsStarted")]
        private string _endpoint = "http://localhost:7200/repositories/Demo-Scene/rdf-graphs/service";

        /// <summary>
        /// Storage name of the graph.
        /// </summary>
        [DisableIf("IsStarted")]
        public string graphName = "GraphName";

        /// <summary>
        /// Number of instant created per second.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), Range(1, 60)]
        private int _instantPerSecond = 10;

        public int InstantPerSecond => _instantPerSecond;

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
            await Task.Run(() =>
            {
                Debug.Log("Semantizing to the server... " + graph.Triples.Count + " triples in the graph.");
                MimeTypeDefinition writerMimeTypeDefinition = MimeTypesHelper.GetDefinitions("application/x-turtle").First();
                string turtle = DecodeGraph(graph);
                string serviceUri = _endpoint;
                serviceUri = (!(graph.BaseUri != null)) ? (serviceUri + "?default") : (serviceUri + "?graph=" + Uri.EscapeDataString($"{graph.BaseUri.AbsoluteUri}{Uri.EscapeDataString(graphName)}"));
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

            // apply rule ontology to the graph
            ApplyRuleOntology();

            SaveToFile(graph, $"{Application.dataPath}/../SVENs/{graphName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ttl");
            await SaveToEndpoint();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            SaveExperiment();
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
            return new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond / (1000 / _instantPerSecond) * (1000 / _instantPerSecond));
        }

        #endregion
    }
}