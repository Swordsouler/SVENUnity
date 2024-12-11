using System;
using UnityEngine;

namespace RDF
{
    /// <summary>
    /// Namespace class to store the prefix name and uri.
    /// </summary>
    [Serializable]
    public class Namespace
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
}