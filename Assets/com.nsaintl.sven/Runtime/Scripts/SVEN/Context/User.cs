using System.Collections.Generic;
using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using UnityEngine;
using VDS.RDF;

namespace Sven.Context
{
    /// <summary>
    /// Represents the user in the scene.
    /// </summary>
    public class User : MonoBehaviour
    {
        /// <summary>
        /// The UUID of the user.
        /// </summary>
        public string UUID => this.GetUUID();

        /// <summary>
        /// The input events of the user.
        /// </summary>
        private Dictionary<string, InputEvent> _inputEvents = new();

        /// <summary>
        /// The point of view of the user.
        /// </summary>
        [SerializeField]
        private PointOfView _pointOfView;

        /// <summary>
        /// The set of currently visible objects.
        /// </summary>
        [SerializeField]
        private List<Pointer> _pointers;

        /// <summary>
        /// The grasp area of the user.
        /// </summary>
        [SerializeField]
        private GraspArea _graspArea;

        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        [SerializeField]
        private GraphBuffer _graphBuffer;

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private void Start()
        {
            if (_graphBuffer == null) _graphBuffer = GraphManager.Get("sven");
            Initialize();
        }

        /// <summary>
        /// Initializes the user.
        /// </summary>
        private void Initialize()
        {
            // semantize user -> to each component
            IGraph graph = _graphBuffer.Graph;

            IUriNode userNode = graph.CreateUriNode("sven:" + UUID);

            graph.Assert(new Triple(userNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:User")));
            if (_pointOfView != null) graph.Assert(new Triple(userNode, graph.CreateUriNode("sven:pointOfView"), graph.CreateUriNode("sven:" + _pointOfView.GetComponent<SemantizationCore>().GetUUID())));
            if (_graspArea != null) graph.Assert(new Triple(userNode, graph.CreateUriNode("sven:graspArea"), graph.CreateUriNode("sven:" + _graspArea.GetComponent<SemantizationCore>().GetUUID())));
            if (_pointers != null)
                foreach (Pointer pointer in _pointers)
                    graph.Assert(new Triple(userNode, graph.CreateUriNode("sven:pointer"), graph.CreateUriNode("sven:" + pointer.GetComponent<SemantizationCore>().GetUUID())));
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, InputEvent> inputEvent in _inputEvents)
            {
                inputEvent.Value.End(_graphBuffer.CurrentInstant);
                inputEvent.Value.Semantize(_graphBuffer.Graph);
            }
            this.DestroyUUID();
        }

        private void Update()
        {
            // check for input events (press)
            if (Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        string key = keyCode.ToString();
                        if (!_inputEvents.ContainsKey(key))
                        {
                            if (SvenHelper.Debug) Debug.Log(key + " pressed");
                            InputEvent inputEvent = new(this, key);
                            inputEvent.Start(_graphBuffer.CurrentInstant);
                            inputEvent.Semantize(_graphBuffer.Graph);
                            _inputEvents.Add(key, inputEvent);
                        }
                    }
                }
            }

            // check for input events (release)
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyUp(keyCode))
                {
                    string key = keyCode.ToString();
                    if (_inputEvents.TryGetValue(key, out InputEvent inputEvent))
                    {
                        if (SvenHelper.Debug) Debug.Log(key + " released");
                        inputEvent.End(_graphBuffer.CurrentInstant);
                        inputEvent.Semantize(_graphBuffer.Graph);
                        _inputEvents.Remove(key);
                    }
                }
            }
        }
    }
}