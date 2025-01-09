#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sven.Utils
{
        /// <summary>
        /// Settings for SVEN.
        /// </summary>
        public static class SvenDebugger
        {
                private const string _debugKey = "SVEN_Debug";

#if UNITY_EDITOR
                public static bool Debug => EditorPrefs.GetBool(_debugKey, false);
#else
        public static bool Debug => false;
#endif
        }
}