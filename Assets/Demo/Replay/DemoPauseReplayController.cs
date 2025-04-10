using NaughtyAttributes;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using VDS.RDF;
using System.Text;
using VDS.RDF.Writing;
using System;
using VDS.RDF.Parsing;

namespace Sven.Demo
{
    public class DemoPauseReplayController : DemoPauseController
    {
        [BoxGroup("View")] public Button downloadButton;
        [BoxGroup("View")] public Button quitButton;

        [BoxGroup("Controller")] public DemoGraphReader graphReader;

        public new void Awake()
        {
            base.Awake();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            if (downloadButton != null) downloadButton.onClick.AddListener(OnDownloadButtonClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private async void OnDownloadButtonClicked()
        {
            IGraph graph = await graphReader.GetGraph();
            // convert to turtle content
            string turtleContent = DecodeGraph(graph);

            // in webgl build, download the turtleContent ass txt file
#if UNITY_WEBGL && !UNITY_EDITOR
            string fileName = $"sven-{DemoManager.graphName}.ttl";
            string jsCode = $@"
                var blob = new Blob([`{turtleContent}`], {{ type: 'text/plain' }});
                var link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                link.download = '{fileName}';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            ";
            Application.ExternalEval(jsCode);
#else
            Debug.Log("Graph content:\n" + turtleContent);
#endif
        }

        /// <summary>
        /// Decode the graph to a turtle string.
        /// </summary>
        /// <param name="graph">The graph to decode.</param>
        /// <returns>Decoded graph in turtle format.</returns>
        protected string DecodeGraph(IGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph) + " is null.");
            StringBuilder sb = new();
            CompressingTurtleWriter writer = new(TurtleSyntax.Rdf11Star);
            writer.Save(graph, new System.IO.StringWriter(sb));
            return sb.ToString();
        }

        private void OnQuitButtonClicked()
        {
            SceneManager.LoadScene("Demo Menu", LoadSceneMode.Single);
        }
    }
}