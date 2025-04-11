using System.Collections.Generic;
using UnityEngine;

namespace Sven.Demo
{
    public class DemoSprayController : MonoBehaviour
    {
        private ParticleSystem ps;

        // these lists are used to contain the particles which match
        // the trigger conditions each frame.
        private readonly List<ParticleSystem.Particle> enter = new();
        private ParticleSystem.ColliderData colliderData = new();


        void Start()
        {
            ps = GetComponent<ParticleSystem>();
            // add to the trigger event callback list all object in scene with tag "Pickup"
            List<GameObject> pickups = new(GameObject.FindGameObjectsWithTag("Pickup"));
            int i = 0;
            foreach (GameObject pickup in pickups)
            {
                if (pickup.name.Contains("Pumpkin"))
                {
                    ps.trigger.SetCollider(i, pickup.transform);
                    i++;
                }
            }
        }

        void OnParticleTrigger()
        {
            // get the particles which matched the trigger conditions this frame
            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter, out colliderData);

            // iterate through the particles which entered the trigger and make them red
            for (int i = 0; i < numEnter; i++)
            {
                int numColliders = colliderData.GetColliderCount(i);
                for (int j = 0; j < numColliders; j++)
                {
                    var collider = colliderData.GetCollider(i, j);
                    if (collider != null)
                    {
                        GameObject obj = collider.gameObject;
                        if (obj.CompareTag("Pickup") && obj.name.Contains("Pumpkin"))
                        {
                            obj.GetComponent<Renderer>().material = GetComponent<Renderer>().material;
                        }
                    }
                }
            }

            // re-assign the modified particles back into the particle system
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
        }
    }
}