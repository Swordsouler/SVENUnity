using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VDS.RDF;

namespace SVEN.RDF
{
    [DisallowMultipleComponent]
    public class GraphManager : MonoBehaviour
    {
        #region Prefixes

        /// <summary>
        /// Flag to check if the graph is started.
        /// </summary>
        private bool IsStarted => graph != null;

        /// <summary>
        /// Prefix class to store the prefix name and uri.
        /// </summary>
        [Serializable]
        public class Prefix
        {
            [field: SerializeField]
            public string Name { get; set; }
            [field: SerializeField]
            public string Uri { get; set; }
        }

        /// <summary>
        /// Base URI for the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted")]
        private string baseUri = "http://www.sven.fr#";

        /// <summary>
        /// List of prefixes to add to the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted")]
        private List<Prefix> prefixes = new() {
            new Prefix { Name = "rdf", Uri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
            new Prefix { Name = "rdfs", Uri = "http://www.w3.org/2000/01/rdf-schema#" },
            new Prefix { Name = "owl", Uri = "http://www.w3.org/2002/07/owl#" },
            new Prefix { Name = "xsd", Uri = "http://www.w3.org/2001/XMLSchema#" },
            new Prefix { Name = "sven", Uri = "http://www.sven.fr#" }
        };

        #endregion

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static GraphManager Instance;

        /// <summary>
        /// Graph to store the RDF data.
        /// </summary>
        private IGraph graph;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private void Start()
        {
            // Initialize the graph with the base URI and prefixes.
            graph = new Graph() { BaseUri = UriFactory.Create(baseUri) };
            foreach (var prefix in prefixes)
                graph.NamespaceMap.AddNamespace(prefix.Name, UriFactory.Create(prefix.Uri));
        }

        // Start is called before the first frame update
        /*async void Start()
        {
            IGraph g = new Graph();

            IUriNode dotNetRDF = g.CreateUriNode(UriFactory.Create("http://www.dotnetrdf.org"));
            IUriNode says = g.CreateUriNode(UriFactory.Create("http://example.org/says"));
            ILiteralNode helloWorld = g.CreateLiteralNode("Hello World");
            ILiteralNode bonjourMonde = g.CreateLiteralNode("Bonjour tout le Monde", "fr");

            g.Assert(new Triple(dotNetRDF, says, helloWorld));
            g.Assert(new Triple(dotNetRDF, says, bonjourMonde));

            foreach (Triple t in g.Triples)
            {
                Debug.Log(t.ToString());
            }

            await SendRdfToEndpoint(g);
        }

        private async Task SendRdfToEndpoint(IGraph graph)
        {
            var writer = new CompressingTurtleWriter();
            StringBuilder sb = new StringBuilder();
            writer.Save(graph, new System.IO.StringWriter(sb));

            var content = new StringContent(sb.ToString(), Encoding.UTF8, "text/turtle");
            var response = await client.PostAsync("http://localhost:9999/blazegraph/namespace/test/sparql", content);

            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Data successfully sent to the endpoint.");
            }
            else
            {
                Debug.LogError("Failed to send data to the endpoint.");
            }
        }*/
    }
}