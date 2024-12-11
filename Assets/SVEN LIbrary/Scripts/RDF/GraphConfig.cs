using System.Collections.Generic;
using UnityEngine;

namespace RDF
{
    [CreateAssetMenu(fileName = "GraphConfig", menuName = "RDF/GraphConfig")]
    public class GraphConfig : ScriptableObject
    {
        /// <summary>
        /// Name of the graph.
        /// </summary>
        [field: SerializeField, Tooltip("Name of the graph.")]
        public string Name { get; private set; } = "default";

        /// <summary>
        /// Base URI for the graph.
        /// </summary>
        [field: SerializeField, Tooltip("Base URI for the graph.")]
        public string BaseUri { get; private set; } = "http://www.example.com#";

        /// <summary>
        /// Endpoint to send the RDF data.
        /// </summary>
        [field: SerializeField, Tooltip("Endpoint to send the RDF data.")]
        public string Endpoint { get; private set; } = "http://localhost:9999/blazegraph/sparql";

        /// <summary>
        /// List of namespaces to add to the graph.
        /// </summary>
        [field: SerializeField, Tooltip("List of namespaces to add to the graph.")]
        public List<Namespace> Namespaces { get; private set; } = new() {
            new Namespace { Name = "rdf", Uri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
            new Namespace { Name = "rdfs", Uri = "http://www.w3.org/2000/01/rdf-schema#" },
            new Namespace { Name = "owl", Uri = "http://www.w3.org/2002/07/owl#" },
            new Namespace { Name = "xsd", Uri = "http://www.w3.org/2001/XMLSchema#" },
        };

        /// <summary>
        /// Number of instant created per second.
        /// </summary>
        [field: SerializeField, Range(1, 60), Tooltip("Number of instant created per second.")]
        public int InstantPerSecond { get; private set; } = 30;
    }
}