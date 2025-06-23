// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using System.Collections.Generic;
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
        /// Start is called before the first frame update.
        /// </summary>
        public void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the user.
        /// </summary>
        private void Initialize()
        {
            IUriNode userNode = GraphManager.CreateUriNode("sven:" + UUID);

            GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("rdf:type"), GraphManager.CreateUriNode("sven:User")));
            if (pointOfView != null) GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("sven:pointOfView"), GraphManager.CreateUriNode("sven:" + pointOfView.GetComponent<SemantizationCore>().GetUUID())));
            if (graspArea != null) GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("sven:graspArea"), GraphManager.CreateUriNode("sven:" + graspArea.GetComponent<SemantizationCore>().GetUUID())));
            if (pointers != null)
                foreach (Pointer pointer in pointers)
                    GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("sven:pointer"), GraphManager.CreateUriNode("sven:" + pointer.GetComponent<SemantizationCore>().GetUUID())));
        }

        public void OnDestroy()
        {
            foreach (KeyValuePair<string, InputEvent> inputEvent in _inputEvents)
            {
                inputEvent.Value.End(GraphManager.CurrentInstant);
                inputEvent.Value.Semanticize();
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
                            if (SvenConfig.Debug) Debug.Log(key + " pressed");
                            InputEvent inputEvent = new(this, key);
                            inputEvent.Start(GraphManager.CurrentInstant);
                            inputEvent.Semanticize();
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
                        if (SvenConfig.Debug) Debug.Log(key + " released");
                        inputEvent.End(GraphManager.CurrentInstant);
                        inputEvent.Semanticize();
                        _inputEvents.Remove(key);
                    }
                }
            }
        }
    }
}