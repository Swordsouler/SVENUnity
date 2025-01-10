using UnityEditor;

namespace Sven.Editor
{
    /// <summary>
    /// Editor window to configure SVEN settings.
    /// </summary>
    public class SvenDebuggerWindow : EditorWindow
    {
        private const string _debugKey = "SVEN_Debug";

        [MenuItem("Window/SVEN Settings")]
        public static void ShowWindow()
        {
            GetWindow<SvenDebuggerWindow>("SVEN Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SVEN Settings", EditorStyles.boldLabel);

            bool debug = EditorPrefs.GetBool(_debugKey, false);
            bool newDebug = EditorGUILayout.Toggle("Debug", debug);

            if (newDebug != debug)
            {
                EditorPrefs.SetBool(_debugKey, newDebug);
            }
        }
    }
}