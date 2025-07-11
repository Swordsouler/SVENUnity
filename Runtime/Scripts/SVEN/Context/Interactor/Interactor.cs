// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Sven.Context
{
    [DisallowMultipleComponent, RequireComponent(typeof(SemantizationCore))]
    public abstract class Interactor : MonoBehaviour
    {
        /// <summary>
        /// The collision events of the interactor.
        /// </summary>
        protected Dictionary<string, CollisionEvent> _collisionEvents = new();

        /// <summary>
        /// The set of currently interacted objects.
        /// </summary>
        [HideInInspector]
        public HashSet<SemantizationCore> currentInteractedObjects = new();

        /// <summary>
        /// The semantization core attached.
        /// </summary>
        protected SemantizationCore _semantizationCore;

        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        //[SerializeField]
        //protected GraphBuffer _graphBuffer;

        /// <summary>
        /// The coroutine to check the interactor.
        /// </summary>
        private Coroutine _checkInteractorCoroutine;

        /// <summary>
        /// Indicates if the interactor is initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// The debug mode.
        /// </summary>
        [SerializeField]
        protected bool _debug = true;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        protected void Awake()
        {
            _semantizationCore = GetComponent<SemantizationCore>();
            if (_semantizationCore == null) Destroy(this);
        }

        /// <summary>
        /// Checks the interaction with the objects.
        /// </summary>
        /// <param name="i">The interval to check the interaction.</param>
        protected abstract IEnumerator CheckInteractor(float i);

        private void Start()
        {
            //if (_graphBuffer == null) return;
            if (_checkInteractorCoroutine != null) StopCoroutine(_checkInteractorCoroutine);
            InitializeAsync();
        }
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
            _checkInteractorCoroutine = StartCoroutine(CheckInteractor(1.0f / SvenSettings.SemanticizeFrequency));
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized) return;
            if (_checkInteractorCoroutine != null) StopCoroutine(_checkInteractorCoroutine);
            _checkInteractorCoroutine = StartCoroutine(CheckInteractor(1.0f / SvenSettings.SemanticizeFrequency));
        }

        private void OnDisable()
        {
            if (_checkInteractorCoroutine != null) StopCoroutine(_checkInteractorCoroutine);
        }

        private void OnDestroy()
        {
            if (_checkInteractorCoroutine != null) StopCoroutine(_checkInteractorCoroutine);
            foreach (CollisionEvent collisionEvent in _collisionEvents.Values)
            {
                collisionEvent.End(GraphManager.CurrentInstant);
                collisionEvent.Semanticize();
            }
        }

        protected void OnDrawGizmos()
        {
            if (!_debug) return;
            // Draw the visible objects
            foreach (var obj in currentInteractedObjects)
            {
                if (obj.TryGetComponent<MeshFilter>(out var meshFilter)) Gizmos.DrawWireMesh(meshFilter.sharedMesh, obj.transform.position, obj.transform.rotation, obj.transform.localScale);
            }
        }
    }
}