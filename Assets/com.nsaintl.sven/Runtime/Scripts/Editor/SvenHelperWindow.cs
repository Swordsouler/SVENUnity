#if UNITY_EDITOR
using Sven.Utils;
using UnityEditor;
using UnityEngine;

namespace Sven.Editor
{
    /// <summary>
    /// Helper window to manage SVEN more easily.
    /// </summary>
    public class SvenHelperWindow : EditorWindow
    {
        [MenuItem("Tools/SVEN Helper")]
        public static void ShowWindow()
        {
            GetWindow<SvenHelperWindow>("SVEN Helper");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SVEN Helper", EditorStyles.boldLabel);

            bool refresh = false;

            bool debug = SvenHelper.Debug;
            bool newDebug = EditorGUILayout.Toggle("Show debug logs", debug);

            if (newDebug != debug)
            {
                EditorPrefs.SetBool(SvenHelper._debugKey, newDebug);
                refresh = true;
            }

            Color pointOfViewDebugColor = SvenHelper.PointOfViewDebugColor;
            Color newPointOfViewDebugColor = EditorGUILayout.ColorField("Point of View Debug Color", pointOfViewDebugColor);

            if (newPointOfViewDebugColor != pointOfViewDebugColor)
            {
                EditorPrefs.SetString(SvenHelper._pointOfViewDebugColorKey, ColorUtility.ToHtmlStringRGB(newPointOfViewDebugColor));
                refresh = true;
            }

            Color pointerDebugColor = SvenHelper.PointerDebugColor;
            Color newPointerDebugColor = EditorGUILayout.ColorField("Pointer Debug Color", pointerDebugColor);

            if (newPointerDebugColor != pointerDebugColor)
            {
                EditorPrefs.SetString(SvenHelper._pointerDebugColorKey, ColorUtility.ToHtmlStringRGB(newPointerDebugColor));
                refresh = true;
            }

            Color graspAreaDebugColor = SvenHelper.GraspAreaDebugColor;
            Color newGraspAreaDebugColor = EditorGUILayout.ColorField("Grasp Area Debug Color", graspAreaDebugColor);

            if (newGraspAreaDebugColor != graspAreaDebugColor)
            {
                EditorPrefs.SetString(SvenHelper._graspAreaDebugColorKey, ColorUtility.ToHtmlStringRGB(newGraspAreaDebugColor));
                refresh = true;
            }

            if (refresh)
            {
                SvenHelper.RefreshHelper();
            }
        }
    }
}
#endif