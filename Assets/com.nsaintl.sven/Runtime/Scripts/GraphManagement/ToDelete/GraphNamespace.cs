// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using UnityEngine;

namespace Sven.GraphManagement
{
    /// <summary>
    /// GraphNamespace class to store the prefix name and uri.
    /// </summary>
    [Serializable]
    public class GraphNamespace
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