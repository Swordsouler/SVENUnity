using UnityEngine;

namespace SVEN.Content
{
    /// <summary>
    /// Example of a simple atom that have some properties semantizable.
    /// </summary>
    public class Atom : MonoBehaviour
    {
        [field: SerializeField]
        public string AtomType { get; set; } = "Carbon";

    }
}