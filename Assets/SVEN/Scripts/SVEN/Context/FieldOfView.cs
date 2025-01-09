using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sven.Content
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public class FieldOfView : MonoBehaviour
    {

        /// <summary>
        /// The set of currently visible objects.
        /// /// </summary>
        private HashSet<SemantizationCore> currentVisibleObjects = new();

        /// <summary>
        /// Array to store the results of collisions.
        /// </summary>
        private Collider[] colliders;

        /// <summary>
        /// The camera component attached to this GameObject.
        /// </summary>
        private Camera cameraComponent;

        /// <summary>
        /// The semantization core attached to the camera.
        /// </summary>
        private SemantizationCore cameraSemantizationCore;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraSemantizationCore = GetComponent<SemantizationCore>();
        }

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Start()
        {
            StartCoroutine(CheckVision(1.0f / cameraSemantizationCore.GraphBuffer.InstantPerSecond));
        }

        /// <summary>
        /// Checks the field of view for SemantizationCore objects and updates the visible objects list.
        /// </summary>
        private IEnumerator CheckVision(float i)
        {
            while (true)
            {
                Vector3 cameraPosition = cameraComponent.transform.position;
                float visionDistance = cameraComponent.farClipPlane;

                colliders = Physics.OverlapSphere(cameraPosition, visionDistance, cameraComponent.cullingMask);
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
                            if (!currentVisibleObjects.Contains(semantizationCore))
                            {
                                // Object enters the field of view, create interval for interaction and semantize the action
                                Debug.Log("Object " + semantizationCore.name + " enters the field of view.");
                            }
                        }
                    }
                }

                // Detect objects that are no longer visible
                foreach (SemantizationCore obj in currentVisibleObjects)
                {
                    if (!newVisibleObjects.Contains(obj))
                    {
                        // Object exits the field of view, close interval for interaction and semantize the action
                        Debug.Log("Object " + obj.name + " exits the field of view.");
                    }
                }

                // Update the list of currently visible objects
                currentVisibleObjects = newVisibleObjects;
                yield return new WaitForSeconds(i);
            }
        }
    }
}