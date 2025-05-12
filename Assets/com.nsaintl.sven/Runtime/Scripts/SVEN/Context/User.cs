// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        public PointOfView pointOfView;

        /// <summary>
        /// The set of currently visible objects.
        /// </summary>
        public List<Pointer> pointers;

        /// <summary>
        /// The grasp area of the user.
        /// </summary>
        [SerializeField]
        public GraspArea graspArea;

        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        public GraphBuffer graphBuffer;

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        public void Start()
        {
            if (graphBuffer == null) graphBuffer = GraphManager.Get("sven");
            Initialize();
        }

        /// <summary>
        /// Initializes the user.
        /// </summary>
        private void Initialize()
        {
            // semantize user -> to each component
            IGraph graph = graphBuffer.Graph;

            IUriNode userNode = graph.CreateUriNode("sven:" + UUID);

            graph.Assert(new Triple(userNode, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("sven:User")));
            if (pointOfView != null) graph.Assert(new Triple(userNode, graph.CreateUriNode("sven:pointOfView"), graph.CreateUriNode("sven:" + pointOfView.GetComponent<SemantizationCore>().GetUUID())));
            if (graspArea != null) graph.Assert(new Triple(userNode, graph.CreateUriNode("sven:graspArea"), graph.CreateUriNode("sven:" + graspArea.GetComponent<SemantizationCore>().GetUUID())));
            if (pointers != null)
                foreach (Pointer pointer in pointers)
                    graph.Assert(new Triple(userNode, graph.CreateUriNode("sven:pointer"), graph.CreateUriNode("sven:" + pointer.GetComponent<SemantizationCore>().GetUUID())));
        }

        public void OnDestroy()
        {
            foreach (KeyValuePair<string, InputEvent> inputEvent in _inputEvents)
            {
                inputEvent.Value.End(graphBuffer.CurrentInstant);
                inputEvent.Value.Semantize(graphBuffer.Graph);
            }
            this.DestroyUUID();
        }

        public void Update()
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
                            inputEvent.Start(graphBuffer.CurrentInstant);
                            inputEvent.Semantize(graphBuffer.Graph);
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
                        inputEvent.End(graphBuffer.CurrentInstant);
                        inputEvent.Semantize(graphBuffer.Graph);
                        _inputEvents.Remove(key);
                    }
                }
            }
        }
    }
}