using System.Collections;
using System.Collections.Generic;
using Sven.Content;
using Sven.Utils;
using UnityEngine;

namespace Sven.Context
{
    /// <summary>
    /// Represents the grasp area in the scene.
    /// </summary>
    public class GraspArea : Interactor
    {
        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        [field: SerializeField]
        public float GraspDistance { get; set; } = 10f;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                Vector3 sphereCenter = transform.position;
                float radius = GraspDistance;

                Collider[] colliders = Physics.OverlapSphere(sphereCenter, radius);
                HashSet<SemantizationCore> newVisibleObjects = new();

                for (int j = 0; j < colliders.Length; j++)
                {
                    Collider collider = colliders[j];
                    if (collider.TryGetComponent(out SemantizationCore semantizationCore))
                    {
                        newVisibleObjects.Add(semantizationCore);
                        if (!_currentInteractedObjects.Contains(semantizationCore))
                        {
                            // Object enters the sphere area, create interval for interaction and semantize the action
                            string dictionaryKey = $"{_semantizationCore.GetUUID()}-{semantizationCore.GetUUID()}";
                            // call start interval semantization of collisionevent
                            if (!_collisionEvents.ContainsKey(dictionaryKey))
                            {
                                if (SvenHelper.Debug) Debug.Log("Object " + semantizationCore.name + " enters the grasp area.");
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
                        // Object exits the sphere area, close interval for interaction and semantize the action
                        // call end interval semantization of collisionevent
                        string dictionaryKey = $"{_semantizationCore.GetUUID()}-{obj.GetUUID()}";
                        if (_collisionEvents.TryGetValue(dictionaryKey, out CollisionEvent collisionEvent))
                        {
                            if (SvenHelper.Debug) Debug.Log("Object " + obj.name + " exits the grasp area.");
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
            if (!_debug) return;
            Gizmos.color = SvenHelper.GraspAreaDebugColor;
            base.OnDrawGizmos();

            Vector3 center = transform.position;
            Gizmos.DrawWireSphere(center, GraspDistance);
        }
    }
}