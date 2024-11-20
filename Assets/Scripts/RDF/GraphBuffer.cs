using System;
using System.Collections.Generic;
using System.Text;
using NaughtyAttributes;
using OWLTime;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Writing;
using System.Net.Http;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RDF
{
    [DisallowMultipleComponent]
    public class GraphBuffer : MonoBehaviour
    {
        #region Flags

        /// <summary>
        /// Flag to check if the graph is started.
        /// </summary>
        private bool IsStarted => graph != null;

        /// <summary>
        /// Flag to check if the graph has a configuration.
        /// </summary>
        private bool HasGraphConfig => graphConfig != null;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Graph configuration.
        /// </summary>
        [SerializeField, HideIf("IsStarted")]
        private GraphConfig graphConfig;

        /// <summary>
        /// Name of the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), ShowIf("IsStarted"), HideIf("HasGraphConfig")]
        private string graphName = "default";
        /// <summary>
        /// Name of the graph.
        /// </summary>
        public string GraphName { get => graphName; }

        /// <summary>
        /// Base URI for the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), ShowIf("IsStarted"), HideIf("HasGraphConfig")]
        private string baseUri = "http://www.example.com#";

        /// <summary>
        /// Endpoint to send the RDF data.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), ShowIf("IsStarted"), HideIf("HasGraphConfig")]
        private string endpoint = "http://localhost:9999/blazegraph/sparql";

        /// <summary>
        /// List of prefixes to add to the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), ShowIf("IsStarted"), HideIf("HasGraphConfig")]
        private List<Namespace> namespaces = new() {
            new Namespace { Name = "rdf", Uri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
            new Namespace { Name = "rdfs", Uri = "http://www.w3.org/2000/01/rdf-schema#" },
            new Namespace { Name = "owl", Uri = "http://www.w3.org/2002/07/owl#" },
            new Namespace { Name = "xsd", Uri = "http://www.w3.org/2001/XMLSchema#" },
        };

        /// <summary>
        /// Number of instant created per second.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), ShowIf("IsStarted"), HideIf("HasGraphConfig"), Range(0, 60)]
        private int instantPerSecond = 30;

        /// <summary>
        /// Graph to store the RDF data.
        /// </summary>
        private IGraph graph;
        /// <summary>
        /// Graph to store the RDF data.
        /// </summary>
        public IGraph Graph { get => graph; }

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
                endpoint = graphConfig.Endpoint;
                namespaces = graphConfig.Namespaces;
                instantPerSecond = graphConfig.InstantPerSecond;
            }

            // initialize the graph
            graph = CreateNewGraph();
        }

        #region Graph Actions

        /// <summary>
        /// Print the turtle of the graph in the console.
        /// </summary>
        [Button("Print")]
        private void Print()
        {
            if (!IsStarted)
            {
                Debug.LogError("Graph is not started.");
                return;
            }

            StringBuilder sb = new();
            CompressingTurtleWriter writer = new();
            writer.Save(graph, new System.IO.StringWriter(sb));

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Save the turtle of the graph to a file.
        /// </summary>
        [Button("Save to File")]
        private void SaveToFile()
        {
            if (!IsStarted)
            {
                Debug.LogError("Graph is not started.");
                return;
            }

            StringBuilder sb = new();
            CompressingTurtleWriter writer = new();
            writer.Save(graph, new System.IO.StringWriter(sb));

#if UNITY_EDITOR
            string path = EditorUtility.SaveFilePanel(
                "Save Turtle File",
                "Assets/Resources",
                "graph",
                "ttl");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, sb.ToString());
            }
#else
    // Code pour les builds non-éditeur si nécessaire
    System.IO.File.WriteAllText("Assets/Resources/graph.ttl", sb.ToString());
#endif
        }

        /// <summary>
        /// Save the turtle of the graph to an endpoint.
        /// </summary>
        [Button("Save to Endpoint")]
        private async void SaveToEndpoint()
        {
            if (!IsStarted)
            {
                Debug.LogError("Graph is not started.");
                return;
            }

            var writer = new CompressingTurtleWriter();
            var sb = new StringBuilder();
            writer.Save(graph, new System.IO.StringWriter(sb));

            var content = new StringContent(sb.ToString(), Encoding.UTF8, "text/turtle");
            var client = new HttpClient();

            try
            {
                HttpResponseMessage response = await client.PostAsync(endpoint, content);
                if (response.IsSuccessStatusCode)
                    Debug.Log("Data successfully sent to the endpoint.");
                else
                    Debug.LogError("Failed to send data to the endpoint.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize a new graph with the base URI and prefixes.
        /// </summary>
        /// <returns>Graph.</returns>
        public IGraph CreateNewGraph()
        {
            graph = new Graph() { BaseUri = UriFactory.Create(baseUri) };
            foreach (Namespace ns in namespaces)
                graph.NamespaceMap.AddNamespace(ns.Name, UriFactory.Create(ns.Uri));
            return graph;
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
                DateTime now = DateTime.Now;
                DateTime dateTime = new(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond / (1000 / instantPerSecond) * (1000 / instantPerSecond));
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

        #endregion
    }
}