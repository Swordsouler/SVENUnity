using System.Collections;
using System.Collections.Generic;
using Sven.Content;
using Sven.Utils;
using UnityEngine;

namespace Sven.Context
{
    [DisallowMultipleComponent, RequireComponent(typeof(SemantizationCore))]
    public class Pointer : Interactor
    {
        /// <summary>
        /// The maximum distance for the pointer.
        /// </summary>
        [SerializeField]
        private float _pointerRange = 20f;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                Vector3 pointerPosition = transform.position;
                Vector3 pointerDirection = transform.forward;
                float visionDistance = _pointerRange;

                Ray ray = new Ray(pointerPosition, pointerDirection);
                RaycastHit[] hits = Physics.RaycastAll(ray, visionDistance);
                HashSet<SemantizationCore> newVisibleObjects = new();

                for (int j = 0; j < hits.Length; j++)
                {
                    RaycastHit hit = hits[j];
                    Collider collider = hit.collider;
                    if (collider.TryGetComponent(out SemantizationCore semantizationCore))
                    {
                        newVisibleObjects.Add(semantizationCore);
                        if (!_currentInteractedObjects.Contains(semantizationCore))
                        {
                            // Object enters the field of view, create interval for interaction and semantize the action
                            string dictionaryKey = $"{_semantizationCore.GetUUID()}-{semantizationCore.GetUUID()}";
                            // call start interval semantization of collisionevent
                            if (!_collisionEvents.ContainsKey(dictionaryKey))
                            {
                                if (SvenHelper.Debug) Debug.Log("Object " + semantizationCore.name + " enters the pointer range.");
                                CollisionEvent collisionEvent = new(_semantizationCore, semantizationCore);
                                collisionEvent.Start(_graphBuffer.CurrentInstant);
                                collisionEvent.Semantize(_graphBuffer.Graph);
                                _collisionEvents.Add(dictionaryKey, collisionEvent);
                            }
                        }
                    }
                }

                // Detect objects that are no longer visible
                foreach (SemantizationCore obj in _currentInteractedObjects)
                {
                    if (!newVisibleObjects.Contains(obj))
                    {
                        // Object exits the field of view, close interval for interaction and semantize the action
                        // call end interval semantization of collisionevent
                        string dictionaryKey = $"{_semantizationCore.GetUUID()}-{obj.GetUUID()}";
                        if (_collisionEvents.TryGetValue(dictionaryKey, out CollisionEvent collisionEvent))
                        {
                            if (SvenHelper.Debug) Debug.Log("Object " + obj.name + " exits the pointer range.");
                            collisionEvent.End(_graphBuffer.CurrentInstant);
                            collisionEvent.Semantize(_graphBuffer.Graph);
                            _collisionEvents.Remove(dictionaryKey);
                        }
                    }
                }

                // Update the list of currently visible objects
                _currentInteractedObjects = newVisibleObjects;
                yield return new WaitForSeconds(i);
            }
        }

        protected new void OnDrawGizmos()
        {
            Gizmos.color = SvenHelper.PointerDebugColor;
            base.OnDrawGizmos();

            Vector3 direction = transform.forward;
            Vector3 origin = transform.position;
            Vector3 destination = origin + direction * _pointerRange;

            Gizmos.DrawLine(origin, destination);
        }
    }
}