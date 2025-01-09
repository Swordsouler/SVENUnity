using System.Collections.Generic;
using UnityEngine;

namespace Sven.GraphManagement
{
    /// <summary>
    /// GraphConfig class to store the graph configuration.
    /// </summary>
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
        /// List of namespaces to add to the graph.
        /// </summary>
        [field: SerializeField, Tooltip("List of namespaces to add to the graph.")]
        public List<GraphNamespace> Namespaces { get; private set; } = new() {
            new GraphNamespace { Name = "rdf", Uri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
            new GraphNamespace { Name = "rdfs", Uri = "http://www.w3.org/2000/01/rdf-schema#" },
            new GraphNamespace { Name = "owl", Uri = "http://www.w3.org/2002/07/owl#" },
            new GraphNamespace { Name = "xsd", Uri = "http://www.w3.org/2001/XMLSchema#" },
        };

        /// <summary>
        /// Ontology file to add to the graph.
        /// </summary>
        [field: SerializeField, Tooltip("Ontology file to add to the graph.")]
        public Object OntologyFile { get; private set; }
    }
}