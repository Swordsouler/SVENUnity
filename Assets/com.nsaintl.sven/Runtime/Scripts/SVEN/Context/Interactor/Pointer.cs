// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using Sven.GraphManagement;
using Sven.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

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
        public int PointerIndex { get; set; } = 0;
        [field: SerializeField]
        public float PointerDistance { get; set; } = 20f;
        [field: SerializeField]
        [field: Range(0f, 90f)]
        public float PointerConeAngle { get; set; } = 0f;
        [field: SerializeField]
        public Vector3 PointerPosition { get; set; } = Vector3.zero;
        [field: SerializeField]
        public Vector3 PointerDirection { get; set; } = Vector3.forward;
        [field: SerializeField]
        public Vector3 PointerHitPosition { get; set; } = Vector3.zero;
        [field: SerializeField]
        public float PointerHitDistance { get; set; } = 0f;

        // Constante pour harmoniser la géométrie du cône
        private const int CONE_SEGMENTS = 12;
        private const int CONE_RINGS = 3;

        protected override IEnumerator CheckInteractor(float i)
        {
            while (true)
            {
                PointerPosition = transform.position;
                PointerDirection = transform.forward;
                float visionDistance = PointerDistance;

                Ray ray = new(PointerPosition, PointerDirection);
                RaycastHit[] hits;

                if (PointerConeAngle > 0f)
                {
                    hits = GetConeCastHits(PointerPosition, PointerDirection, PointerDistance, PointerConeAngle);
                }
                else
                {
                    hits = Physics.RaycastAll(ray, visionDistance);
                }

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

        private RaycastHit[] GetConeCastHits(Vector3 origin, Vector3 direction, float distance, float coneAngleDegrees)
        {
            Dictionary<Collider, RaycastHit> uniqueHits = new();
            float coneAngleRad = coneAngleDegrees * Mathf.Deg2Rad;

            // Calcule un rayon de sphère qui englobe tout le cône
            float coneRadius = Mathf.Tan(coneAngleRad) * distance;
            float sphereRadius = Mathf.Sqrt(distance * distance + coneRadius * coneRadius);
            Vector3 sphereCenter = origin + direction.normalized * (distance * 0.5f);

            // Récupère tous les colliders dans une sphère englobant le cône
            Collider[] allColliders = Physics.OverlapSphere(sphereCenter, sphereRadius);

            foreach (Collider collider in allColliders)
            {
                // Trouve le point le plus proche sur le collider
                Vector3 closestPoint = collider.ClosestPoint(origin);

                // Vérifie si ce point est dans le cône
                if (IsPointInCone(origin, direction, closestPoint, coneAngleRad, distance))
                {
                    // Lance un rayon vers ce point pour obtenir un RaycastHit précis
                    Vector3 rayDirection = (closestPoint - origin).normalized;
                    float rayDistance = Vector3.Distance(origin, closestPoint) + collider.bounds.extents.magnitude;

                    Ray ray = new(origin, rayDirection);
                    RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);

                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider == collider && IsPointInCone(origin, direction, hit.point, coneAngleRad, distance))
                        {
                            if (!uniqueHits.ContainsKey(hit.collider))
                            {
                                uniqueHits.Add(hit.collider, hit);
                            }
                            break;
                        }
                    }
                }
            }

            return uniqueHits.Values.ToArray();
        }

        private void CastRayAndAddHits(Ray ray, float distance, float coneAngleRad, Vector3 origin, Vector3 direction, Dictionary<Collider, RaycastHit> uniqueHits)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, distance);
            foreach (RaycastHit hit in hits)
            {
                if (IsPointInCone(origin, direction, hit.point, coneAngleRad, distance))
                {
                    if (!uniqueHits.ContainsKey(hit.collider))
                        uniqueHits.Add(hit.collider, hit);
                }
            }
        }

        private bool IsPointInCone(Vector3 coneOrigin, Vector3 coneDirection, Vector3 point, float coneAngleRad, float maxDistance)
        {
            Vector3 pointVector = point - coneOrigin;
            float pointDistance = pointVector.magnitude;

            // Vérifie la distance maximale
            if (pointDistance > maxDistance)
                return false;

            // Évite la division par zéro
            if (pointDistance < 0.01f)
                return true;

            // Calcule l'angle entre le point et la direction du cône
            float angle = Vector3.Angle(coneDirection, pointVector) * Mathf.Deg2Rad;

            // Le point est dans le cône si l'angle est <= angle du cône
            return angle <= coneAngleRad;
        }

        protected new void OnDrawGizmos()
        {
            if (!_debug) return;
            Gizmos.color = SvenSettings.PointerDebugColor;
            base.OnDrawGizmos();

            Vector3 direction = transform.forward;
            Vector3 origin = transform.position;
            Vector3 destination = origin + direction * PointerDistance;

            if (PointerConeAngle > 0f)
            {
                DrawCone(origin, direction, PointerDistance, PointerConeAngle);
            }
            else
            {
                Gizmos.DrawLine(origin, destination);
            }
        }

        private void DrawCone(Vector3 origin, Vector3 direction, float distance, float coneAngleDegrees)
        {
            float coneAngleRad = coneAngleDegrees * Mathf.Deg2Rad;

            // Calcul des vecteurs perpendiculaires
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(direction, Vector3.right).normalized;

            Vector3 up = Vector3.Cross(right, direction).normalized;

            // Ligne centrale
            Vector3 destination = origin + direction.normalized * distance;
            Gizmos.DrawLine(origin, destination);

            // Dessiner les anneaux du cône
            for (int ring = 1; ring <= CONE_RINGS; ring++)
            {
                float ringDistance = distance * (ring / (float)(CONE_RINGS + 1));
                float ringRadius = Mathf.Tan(coneAngleRad) * ringDistance;
                Vector3 ringCenter = origin + direction.normalized * ringDistance;

                DrawCircle(ringCenter, direction, ringRadius, CONE_SEGMENTS);
            }

            // Dessiner le cercle de la base du cône
            float baseRadius = Mathf.Tan(coneAngleRad) * distance;
            DrawCircle(destination, direction, baseRadius, CONE_SEGMENTS);

            // Dessiner les lignes depuis l'origine vers les points du cercle de base
            for (int i = 0; i < CONE_SEGMENTS; i++)
            {
                float angle = (i / (float)CONE_SEGMENTS) * 360f * Mathf.Deg2Rad;
                Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * baseRadius;
                Vector3 pointOnCircle = destination + offset;
                Gizmos.DrawLine(origin, pointOnCircle);
            }
        }

        private void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments)
        {
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(normal, Vector3.right).normalized;

            Vector3 up = Vector3.Cross(right, normal).normalized;

            Vector3 previousPoint = center + right * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * radius;
                Vector3 currentPoint = center + offset;

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        private static List<string> _availableNames;
        private static List<string> _availableDeictics;
        private static string _cachedLocale;

        public static async Task<List<string>> GetAvailableNamesAsync(string locale)
        {
            if (_availableNames == null || _cachedLocale != locale)
            {
                _availableNames = await GetAllAvailableNames(locale);
                _cachedLocale = locale;
            }
            return _availableNames;
        }

        public static async Task<List<string>> GetAllAvailableNames(string locale)
        {
            // load a graph with colors from resources
            Graph graph = new();
            // load ontology like GraphManager
            Dictionary<string, string> ontologies = await SvenSettings.GetOntologiesAsync();
            TurtleParser turtleParser = new();
            foreach (KeyValuePair<string, string> ontology in ontologies)
            {
                turtleParser.Load(graph, ontology.Value);
            }

            string query = $@"
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?label
WHERE {{
    sven:Pointer rdfs:label ?label .
    FILTER(langMatches(lang(?label), ""{locale}""))
}}";

            if (graph.ExecuteQuery(query) is SparqlResultSet results)
            {
                return results.Select(result => (result["label"] as ILiteralNode)?.Value).Where(label => label != null).ToList();
            }

            return new List<string>();
        }

        public static async Task<List<string>> GetAvailableDeicticsAsync(string locale)
        {
            if (_availableDeictics == null || _cachedLocale != locale)
            {
                _availableDeictics = await GetAllAvailableDeictics(locale);
                _cachedLocale = locale;
            }
            return _availableDeictics;
        }

        public static async Task<List<string>> GetAllAvailableDeictics(string locale)
        {
            // load a graph with colors from resources
            Graph graph = new();
            // load ontology like GraphManager
            Dictionary<string, string> ontologies = await SvenSettings.GetOntologiesAsync();
            TurtleParser turtleParser = new();
            foreach (KeyValuePair<string, string> ontology in ontologies)
            {
                turtleParser.Load(graph, ontology.Value);
            }
            string query = $@"
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
SELECT ?label
WHERE {{
    sven:Pointer sven:deicticWord ?label .
    FILTER(langMatches(lang(?label), ""{locale}""))
}}";
            if (graph.ExecuteQuery(query) is SparqlResultSet results)
            {
                return results.Select(result => (result["label"] as ILiteralNode)?.Value).Where(label => label != null).ToList();
            }
            return new List<string>();
        }
    }
}