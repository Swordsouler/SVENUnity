#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sven.Utils
{
        /// <summary>
        /// Helper class to manage SVEN settings.
        /// </summary>
        public static class SvenHelper
        {
                private const string _debugKey = "SVEN_Debug";

#if UNITY_EDITOR
                public static bool Debug => EditorPrefs.GetBool(_debugKey, false);
#else
        public static bool Debug => false;
#endif
        }
}