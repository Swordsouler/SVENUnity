using System;
using System.Collections.Generic;
using System.Text;
using NaughtyAttributes;
using OWLTime;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Writing;
using System.Net.Http;
using VDS.RDF.Parsing;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RDF
{
    [DisallowMultipleComponent]
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
        [SerializeField, DisableIf("IsStarted"), HideIf("HasGraphConfig")]
        private string baseUri = "http://www.example.com#";

        /// <summary>
        /// Endpoint to send the RDF data.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), HideIf("HasGraphConfig")]
        private string endpoint = "http://localhost:9999/blazegraph/sparql";

        /// <summary>
        /// List of prefixes to add to the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), HideIf("HasGraphConfig")]
        private List<Namespace> namespaces = new() {
            new Namespace { Name = "rdf", Uri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
            new Namespace { Name = "rdfs", Uri = "http://www.w3.org/2000/01/rdf-schema#" },
            new Namespace { Name = "owl", Uri = "http://www.w3.org/2002/07/owl#" },
            new Namespace { Name = "xsd", Uri = "http://www.w3.org/2001/XMLSchema#" },
        };

        /// <summary>
        /// Number of instant created per second.
        /// </summary>
        [SerializeField, DisableIf("IsStarted"), HideIf("HasGraphConfig"), Range(1, 60)]
        private int instantPerSecond = 30;

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
                endpoint = graphConfig.Endpoint;
                namespaces = graphConfig.Namespaces;
                instantPerSecond = graphConfig.InstantPerSecond;
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
            string turtle = DecodeGraph(Graph);

#if UNITY_EDITOR
            string path = EditorUtility.SaveFilePanel(
                "Save Turtle File",
                "Assets/Resources",
                "graph",
                "ttl");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, turtle);
            }
#else
    // Code pour les builds non-éditeur si nécessaire
    System.IO.File.WriteAllText("Assets/Resources/graph.ttl", turtle);
#endif
        }

        /// <summary>
        /// Save the turtle of the graph to an endpoint.
        /// </summary>
        [Button("Save to Endpoint")]
        private async void SaveToEndpoint()
        {
            string turtle = DecodeGraph(Graph);

            StringContent content = new(turtle, Encoding.UTF8, "text/turtle");
            HttpClient client = new();

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
        public Graph CreateNewGraph()
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