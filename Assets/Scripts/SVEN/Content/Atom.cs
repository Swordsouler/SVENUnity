using UnityEngine;

namespace SVEN.Content
{
    // Juste pour tester
    public class Atom : MonoBehaviour
    {
        [SerializeField]
        private string atomType = "Carbon";
        public string AtomType
        {
            get => atomType;
            set => atomType = value;
        }

    }
}