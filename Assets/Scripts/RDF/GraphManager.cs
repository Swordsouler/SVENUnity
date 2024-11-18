using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Writing;

namespace RDF
{
    [DisallowMultipleComponent]
    public class GraphManager : MonoBehaviour
    {
        /// <summary>
        /// Name of the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted")]
        private string graphName = "default";

        /// <summary>
        /// Base URI for the graph.
        /// </summary>
        [SerializeField, DisableIf("IsStarted")]
        private string baseUri = "http://www.sven.fr#";

        /// <summary>
        /// Base URI for the graph.
        /// </summary>
        public string BaseUri
        {
            get => baseUri;
            set => baseUri = value;
        }

        /// <summary>
        /// Endpoint to send the RDF data.
        /// </summary>
        public string dataEndpoint = "http://localhost:9999/blazegraph/namespace/test/sparql";

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
            /// <summary>
            /// Name of the prefix.
            /// </summary>
            [field: SerializeField]
            public string Name { get; set; }

            /// <summary>
            /// URI of the prefix.
            /// </summary>
            [field: SerializeField]
            public string Uri { get; set; }
        }

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

        private Prefix GetPrefix(string name)
        {
            return prefixes.Find(p => p.Name == name);
        }

        #endregion

        #region Instances Management

        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static Dictionary<string, GraphManager> Instances { get; } = new();

        /// <summary>
        /// Get the graph from graph name.
        /// </summary>
        /// <param name="graphName">Name of the graph.</param>
        /// <returns>Graph.</returns>
        public static GraphManager Get(string graphName)
        {
            try
            {
                return Instances[graphName.ToLower()];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (Instances.ContainsKey(graphName.ToLower()))
            {
                Debug.LogWarning($"GraphManager instance already exists for the GameObject {gameObject.name}.");
                Destroy(gameObject);
                return;
            }
            else Instances.Add(graphName.ToLower(), this);

            graph = NewGraph();
        }

        #endregion

        [Button("Print Graph")]
        private void PrintGraph()
        {
            Push();
        }

        /// <summary>
        /// Graph to store the RDF data.
        /// </summary>
        public IGraph graph;

        /// <summary>
        /// Initialize a new graph with the base URI and prefixes.
        /// </summary>
        /// <returns>Graph.</returns>
        public IGraph NewGraph()
        {
            graph = new Graph() { BaseUri = UriFactory.Create(BaseUri) };
            foreach (var prefix in prefixes)
                graph.NamespaceMap.AddNamespace(prefix.Name, UriFactory.Create(prefix.Uri));
            return graph;
        }

        public void Merge(IGraph graph)
        {
            foreach (Triple t in graph.Triples)
            {
                this.graph.Assert(t);
            }
        }

        /// <summary>
        /// Push the graph to the endpoint.
        /// </summary>
        public void Push()
        {
            var writer = new CompressingTurtleWriter();
            var sb = new System.Text.StringBuilder();
            writer.Save(graph, new System.IO.StringWriter(sb));

            var content = new System.Net.Http.StringContent(sb.ToString(), System.Text.Encoding.UTF8, "text/turtle");
            Debug.Log(sb.ToString());
            /*var client = new System.Net.Http.HttpClient();

            client.PostAsync(dataEndpoint, content).ContinueWith(response =>
            {
                if (response.Result.IsSuccessStatusCode)
                    Debug.Log("Data successfully sent to the endpoint.");
                else Debug.LogError("Failed to send data to the endpoint.");
            });*/
        }

        /// <summary>
        /// Clear the graph.
        /// </summary>
        public void Clear()
        {
            graph.Clear();
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