using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sven.GraphManagement
{
    /// <summary>
    /// GraphConfig class to store the graph configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "Ontology", menuName = "Semantic/Ontology")]
    public class OntologyDescription : ScriptableObject
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
            new GraphNamespace { Name = "sven", Uri = "http://www.sven.fr/ontology#" },
            new GraphNamespace { Name = "time", Uri = "http://www.w3.org/2006/time#" }
        };

        /// <summary>
        /// Ontology file to add to the graph.
        /// </summary>
        [SerializeField, Tooltip("Ontology file to add to the graph."), OnValueChanged("RefreshOntologyContent")]
        public Object _ontologyFile;
        public Object OntologyFile
        {
            get => _ontologyFile;
            private set
            {
                _ontologyFile = value;
                RefreshOntologyContent();
            }
        }

        /// <summary>
        /// Refresh the ontology content.
        /// </summary>
        [Button("Refresh Ontology Content")]
        public void RefreshOntologyContent()
        {
#if UNITY_EDITOR
            OntologyContent = File.ReadAllText(AssetDatabase.GetAssetPath(_ontologyFile));
#endif
        }

        /// <summary>
        /// Ontology content to add to the graph.
        /// </summary>
        [field: SerializeField, TextArea, ReadOnly, Tooltip("Ontology content to add to the graph.")]
        public string OntologyContent { get; private set; }
    }
}