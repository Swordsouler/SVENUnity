// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using System.Collections;
using System.Collections.Generic;
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
        [field: SerializeField]
        public Vector3 PointerPosition { get; set; } = Vector3.zero;
        [field: SerializeField]
        public Vector3 PointerDirection { get; set; } = Vector3.forward;
        [field: SerializeField]
        public Vector3 PointerHitPosition { get; set; } = Vector3.zero;
        [field: SerializeField]
        public float PointerHitDistance { get; set; } = 0f;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                PointerPosition = transform.position;
                PointerDirection = transform.forward;
                float visionDistance = PointerDistance;

                Ray ray = new(PointerPosition, PointerDirection);
                RaycastHit[] hits = Physics.RaycastAll(ray, visionDistance);

                // Détermine le point le plus proche touché par le rayon ; si aucun hit, prend le point à la distance maximale du pointer
                if (hits != null && hits.Length > 0)
                {
                    float minDist = float.MaxValue;
                    Vector3 closestPoint = PointerPosition + PointerDirection.normalized * visionDistance;
                    for (int k = 0; k < hits.Length; k++)
                    {
                        RaycastHit h = hits[k];
                        if (h.distance < minDist)
                        {
                            minDist = h.distance;
                            closestPoint = h.point;
                        }
                    }
                    PointerHitPosition = closestPoint;
                    PointerHitDistance = minDist;
                }
                else
                {
                    PointerHitPosition = PointerPosition + PointerDirection.normalized * visionDistance;
                    PointerHitDistance = visionDistance;
                }

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
                                if (SvenSettings.Debug) Debug.Log("Object " + semantizationCore.name + " enters the pointer range.");
                                CollisionEvent collisionEvent = new(_semantizationCore, semantizationCore);
                                collisionEvent.Start(GraphManager.CurrentInstant);
                                collisionEvent.Semanticize();
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
                            if (SvenSettings.Debug) Debug.Log("Object " + obj.name + " exits the pointer range.");
                            collisionEvent.End(GraphManager.CurrentInstant);
                            collisionEvent.Semanticize();
                            _collisionEvents.Remove(dictionaryKey);
                        }
                    }
                }
                // sort the hashset by distance to the pointer
                List<SemantizationCore> sortedVisibleObjects = new(newVisibleObjects);
                sortedVisibleObjects.Sort((a, b) => Vector3.Distance(a.transform.position, PointerPosition).CompareTo(Vector3.Distance(b.transform.position, PointerPosition)));
                // Update the list of currently interacted objects
                currentInteractedObjects.Clear();
                currentInteractedObjects.UnionWith(sortedVisibleObjects);

                yield return new WaitForSeconds(i);
            }
        }

        protected new void OnDrawGizmos()
        {
            if (!_debug) return;
            Gizmos.color = SvenSettings.PointerDebugColor;
            base.OnDrawGizmos();

            Vector3 direction = transform.forward;
            Vector3 origin = transform.position;
            Vector3 destination = origin + direction * PointerDistance;

            Gizmos.DrawLine(origin, destination);
        }
    }
}