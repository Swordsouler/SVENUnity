#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SVEN
{
    /// <summary>
    /// Settings for SVEN.
    /// </summary>
    public static class Settings
    {
        private const string DebugKey = "SVEN_Debug";

#if UNITY_EDITOR
        public static bool Debug
        {
            get { return EditorPrefs.GetBool(DebugKey, false); }
        }
#else
        public static bool Debug
        {
            get { return false; } // Valeur par défaut pour les builds
        }
#endif
    }
}