using UnityEditor;

namespace Sven.Editor
{
    /// <summary>
    /// Helper window to manage SVEN more easily.
    /// </summary>
    public class SvenHelperWindow : EditorWindow
    {
        private const string _debugKey = "SVEN_Debug";

        [MenuItem("Tools/SVEN Helper")]
        public static void ShowWindow()
        {
            GetWindow<SvenHelperWindow>("SVEN Helper");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SVEN Helper", EditorStyles.boldLabel);

            bool debug = EditorPrefs.GetBool(_debugKey, false);
            bool newDebug = EditorGUILayout.Toggle("Show debug logs", debug);

            if (newDebug != debug)
            {
                EditorPrefs.SetBool(_debugKey, newDebug);
            }
        }
    }
}