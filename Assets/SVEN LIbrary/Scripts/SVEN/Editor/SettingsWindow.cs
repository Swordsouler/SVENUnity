#if UNITY_EDITOR
using UnityEditor;

namespace SVEN.Editor
{
    /// <summary>
    /// Editor window to configure SVEN settings.
    /// </summary>
    public class SettingsWindow : EditorWindow
    {
        private const string DebugKey = "SVEN_Debug";

        [MenuItem("Window/SVEN Settings")]
        public static void ShowWindow()
        {
            GetWindow<SettingsWindow>("SVEN Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SVEN Settings", EditorStyles.boldLabel);

            bool debug = EditorPrefs.GetBool(DebugKey, false);
            bool newDebug = EditorGUILayout.Toggle("Debug", debug);

            if (newDebug != debug)
            {
                EditorPrefs.SetBool(DebugKey, newDebug);
            }
        }
    }
}
#endif