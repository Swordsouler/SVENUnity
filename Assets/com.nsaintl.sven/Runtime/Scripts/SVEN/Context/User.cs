// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            InitializeAsync();
        }

        /// <summary>
        /// Initializes the user.
        /// </summary>
        private async void InitializeAsync()
        {
            bool isGraphInitialized = false;
            for (int i = 0; i < 5; i++)
            {
                isGraphInitialized = GraphManager.IsGraphInitialized;
                if (isGraphInitialized)
                    break;
                else await Task.Delay(2000);
            }
            if (!isGraphInitialized)
            {
                Debug.LogError("GraphManager is not initialized. Please check your settings.");
                return;
            }
            IUriNode userNode = GraphManager.CreateUriNode(":" + UUID);

            GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("rdf:type"), GraphManager.CreateUriNode("sven:User")));
            if (pointOfView != null) GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("sven:pointOfView"), GraphManager.CreateUriNode(":" + pointOfView.GetComponent<SemantizationCore>().GetUUID())));
            if (graspArea != null) GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("sven:graspArea"), GraphManager.CreateUriNode(":" + graspArea.GetComponent<SemantizationCore>().GetUUID())));
            if (pointers != null)
                foreach (Pointer pointer in pointers)
                    GraphManager.Assert(new Triple(userNode, GraphManager.CreateUriNode("sven:pointer"), GraphManager.CreateUriNode(":" + pointer.GetComponent<SemantizationCore>().GetUUID())));
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
                            if (SvenSettings.Debug) Debug.Log(key + " pressed");
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
                        if (SvenSettings.Debug) Debug.Log(key + " released");
                        inputEvent.End(GraphManager.CurrentInstant);
                        inputEvent.Semanticize();
                        _inputEvents.Remove(key);
                    }
                }
            }
        }
    }
}