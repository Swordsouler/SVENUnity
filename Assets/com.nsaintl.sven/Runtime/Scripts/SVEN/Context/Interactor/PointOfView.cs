using System.Collections;
using System.Collections.Generic;
using Sven.Content;
using Sven.Utils;
using UnityEngine;

namespace Sven.Context
{
    /// <summary>
    /// Represents the point of view in the scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PointOfView : Interactor
    {
        /// <summary>
        /// The camera component attached to this GameObject.
        /// </summary>
        public Camera cameraComponent;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        protected new void Awake()
        {
            base.Awake();
            cameraComponent = GetComponent<Camera>();
            if (cameraComponent == null) Destroy(this);
        }

        /// <summary>
        /// Checks the field of view for SemantizationCore objects and updates the visible objects list.
        /// </summary>
        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                Vector3 cameraPosition = cameraComponent.transform.position;
                float visionDistance = cameraComponent.farClipPlane;

                Collider[] colliders = Physics.OverlapSphere(cameraPosition, visionDistance, cameraComponent.cullingMask);
                HashSet<SemantizationCore> newVisibleObjects = new();

                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cameraComponent);

                for (int j = 0; j < colliders.Length; j++)
                {
                    Collider collider = colliders[j];
                    if (GeometryUtility.TestPlanesAABB(frustumPlanes, collider.bounds))
                    {
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
                                    if (SvenHelper.Debug) Debug.Log("Object " + semantizationCore.name + " enters the field of view.");
                                    CollisionEvent collisionEvent = new(_semantizationCore, semantizationCore);
                                    collisionEvent.Start(_graphBuffer.CurrentInstant);
                                    collisionEvent.Semantize(_graphBuffer.Graph);
                                    _collisionEvents.Add(dictionaryKey, collisionEvent);
                                }
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
                            if (SvenHelper.Debug) Debug.Log("Object " + obj.name + " exits the field of view.");
                            collisionEvent.End(_graphBuffer.CurrentInstant);
                            collisionEvent.Semantize(_graphBuffer.Graph);
                            _collisionEvents.Remove(dictionaryKey);
                        }
                    }
                }

                // Update the list of currently visible objects
                currentInteractedObjects = newVisibleObjects;
                yield return new WaitForSeconds(i);
            }
        }

        protected new void OnDrawGizmos()
        {
            if (!_debug) return;
            Gizmos.color = SvenHelper.PointOfViewDebugColor;
            base.OnDrawGizmos();

            if (cameraComponent == null) return;

            // Get the corners of the frustum
            Vector3[] frustumCorners = new Vector3[4];
            cameraComponent.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cameraComponent.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

            // Convert the corners to world space
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = cameraComponent.transform.TransformPoint(frustumCorners[i]);
            }

            // Draw the frustum
            Gizmos.DrawLine(cameraComponent.transform.position, frustumCorners[0]);
            Gizmos.DrawLine(cameraComponent.transform.position, frustumCorners[1]);
            Gizmos.DrawLine(cameraComponent.transform.position, frustumCorners[2]);
            Gizmos.DrawLine(cameraComponent.transform.position, frustumCorners[3]);

            Gizmos.DrawLine(frustumCorners[0], frustumCorners[1]);
            Gizmos.DrawLine(frustumCorners[1], frustumCorners[2]);
            Gizmos.DrawLine(frustumCorners[2], frustumCorners[3]);
            Gizmos.DrawLine(frustumCorners[3], frustumCorners[0]);
        }
    }
}