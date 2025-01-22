#if UNITY_EDITOR
using Sven.Content;
using Sven.GraphManagement;
using UnityEditor;
using UnityEngine;

namespace Sven.Editor
{
    /// <summary>
    /// Menu to instantiate SVEN components.
    /// </summary>
    public class SvenMenu
    {
        /// <summary>
        /// Instantiate the graph reader.
        /// </summary>
        [MenuItem("GameObject/Semantic/Graph Reader", false, 10)]
        private static void InstantiateGraphReader()
        {
            GameObject go = new("Graph Reader");
            go.AddComponent<GraphReader>();
        }

        /// <summary>
        /// Instantiate the graph buffer.
        /// </summary>
        [MenuItem("GameObject/Semantic/Graph Buffer", false, 10)]
        private static void InstantiateGraphBuffer()
        {
            GameObject go = new("Graph Buffer");
            go.AddComponent<GraphBuffer>();
        }

        /// <summary>
        /// Instantiate the semantization core.
        /// </summary>
        [MenuItem("GameObject/Semantic/Semantization Core", false, 10)]
        private static void InstantiateSemantizationCore()
        {
            GameObject go = new("Semantization Core");
            go.AddComponent<SemantizationCore>();
        }
    }
}
#endif