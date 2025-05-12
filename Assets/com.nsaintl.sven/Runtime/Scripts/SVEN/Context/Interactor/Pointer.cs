// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Sven.Content;
using Sven.Utils;
using UnityEngine;

namespace Sven.Context
{
    /// <summary>
    /// Represents the pointer in the scene.
    /// </summary>
    public class Pointer : Interactor
    {
        /// <summary>
        /// The maximum distance for the pointer.
        /// </summary>
        [field: SerializeField]
        public float PointerDistance { get; set; } = 20f;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                Vector3 pointerPosition = transform.position;
                Vector3 pointerDirection = transform.forward;
                float visionDistance = PointerDistance;

                Ray ray = new(pointerPosition, pointerDirection);
                RaycastHit[] hits = Physics.RaycastAll(ray, visionDistance);
                HashSet<SemantizationCore> newVisibleObjects = new();

                for (int j = 0; j < hits.Length; j++)
                {
                    RaycastHit hit = hits[j];
                    Collider collider = hit.collider;
                    if (collider.TryGetComponent(out SemantizationCore semantizationCore))
                    {
                        newVisibleObjects.Add(semantizationCore);
                        if (!currentInteractedObjects.Contains(semantizationCore))
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
                foreach (SemantizationCore obj in currentInteractedObjects)
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
                // sort the hashset by distance to the pointer
                List<SemantizationCore> sortedVisibleObjects = new(newVisibleObjects);
                sortedVisibleObjects.Sort((a, b) => Vector3.Distance(a.transform.position, pointerPosition).CompareTo(Vector3.Distance(b.transform.position, pointerPosition)));
                // Update the list of currently interacted objects
                currentInteractedObjects.Clear();
                currentInteractedObjects.UnionWith(sortedVisibleObjects);

                yield return new WaitForSeconds(i);
            }
        }

        protected new void OnDrawGizmos()
        {
            if (!_debug) return;
            Gizmos.color = SvenHelper.PointerDebugColor;
            base.OnDrawGizmos();

            Vector3 direction = transform.forward;
            Vector3 origin = transform.position;
            Vector3 destination = origin + direction * PointerDistance;

            Gizmos.DrawLine(origin, destination);
        }
    }
}