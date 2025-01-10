using Sven.GraphManagement;
using TMPro;
using UnityEngine;

namespace Sven.Utils
{
    /// <summary>
    /// TextGraphSize class to show the graph size.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextGraphSize : MonoBehaviour
    {
        /// <summary>
        /// GraphBuffer to get the graph.
        /// </summary>
        public GraphBuffer graphBuffer;

        /// <summary>
        /// Text to show the graph size.
        /// </summary>
        private TextMeshProUGUI text;

        void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            GetComponent<TextMeshProUGUI>().text = graphBuffer.Graph.Triples.Count.ToString();
        }
    }
}