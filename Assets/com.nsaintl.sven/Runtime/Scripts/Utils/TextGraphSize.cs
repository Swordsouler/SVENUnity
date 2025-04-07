using Sven.GraphManagement;
using TMPro;
using UnityEngine;

namespace Sven.Utils
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextGraphSize : MonoBehaviour
    {
        public GraphBuffer graphBuffer;
        private TextMeshProUGUI tmp;

        void Awake()
        {
            tmp = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            tmp.text = graphBuffer.Graph.Triples.Count.ToString();
        }
    }
}