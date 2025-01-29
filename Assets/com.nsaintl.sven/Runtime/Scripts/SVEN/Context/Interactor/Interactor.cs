using System.Collections;
using System.Collections.Generic;
using Sven.Content;
using Sven.GraphManagement;
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
        protected HashSet<SemantizationCore> _currentInteractedObjects = new();

        /// <summary>
        /// The semantization core attached.
        /// </summary>
        protected SemantizationCore _semantizationCore;

        /// <summary>
        /// The graph buffer to semantize the GameObject.
        /// </summary>
        [SerializeField]
        protected GraphBuffer _graphBuffer;

        /// <summary>
        /// The coroutine to check the interactor.
        /// </summary>
        private Coroutine _checkInteractorCoroutine;

        /// <summary>
        /// Indicates if the interactor is initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        protected void Awake()
        {
            if (_graphBuffer == null) _graphBuffer = GraphManager.Get("sven");
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
            if (_checkInteractorCoroutine != null) StopCoroutine(_checkInteractorCoroutine);
            _checkInteractorCoroutine = StartCoroutine(CheckInteractor(1.0f / _graphBuffer.InstantPerSecond));
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized) return;
            if (_checkInteractorCoroutine != null) StopCoroutine(_checkInteractorCoroutine);
            Debug.LogWarning("Interactor OnEnable " + _graphBuffer.name + " " + (1.0f / _graphBuffer.InstantPerSecond));
            _checkInteractorCoroutine = StartCoroutine(CheckInteractor(1.0f / _graphBuffer.InstantPerSecond));
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
                collisionEvent.End(_graphBuffer.CurrentInstant);
                collisionEvent.Semantize(_graphBuffer.Graph);
            }
        }

        protected void OnDrawGizmos()
        {
            // Draw the visible objects
            foreach (var obj in _currentInteractedObjects)
            {
                if (obj.TryGetComponent<MeshFilter>(out var meshFilter)) Gizmos.DrawWireMesh(meshFilter.sharedMesh, obj.transform.position, obj.transform.rotation, obj.transform.localScale);
            }
        }
    }
}