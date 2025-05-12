// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using UnityEngine;
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
        public static readonly string _useInsideKey = "SVEN_UseInside";
        public static readonly string _debugKey = "SVEN_Debug";
        public static readonly string _pointOfViewDebugColorKey = "SVEN_PointOfViewDebugColor";
        public static readonly string _pointerDebugColorKey = "SVEN_PointerDebugColor";
        public static readonly string _graspAreaDebugColorKey = "SVEN_GraspAreaDebugColor";

        public static bool UseInside => _useInside;
        public static bool Debug => _debug;
        public static Color PointOfViewDebugColor => _pointOfViewDebugColor;
        public static Color PointerDebugColor => _pointerDebugColor;
        public static Color GraspAreaDebugColor => _graspAreaDebugColor;

        private static bool _useInside = false;
        private static bool _debug = false;
        private static Color _pointOfViewDebugColor = Color.red;
        private static Color _pointerDebugColor = Color.blue;
        private static Color _graspAreaDebugColor = Color.green;

#if UNITY_EDITOR

        public static void RefreshHelper()
        {
            _useInside = EditorPrefs.GetBool(_useInsideKey, false);
            _debug = EditorPrefs.GetBool(_debugKey, false);
            _pointOfViewDebugColor = ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(_pointOfViewDebugColorKey, "FF0000"), out Color pointOfViewDebugColor) ? pointOfViewDebugColor : Color.red;
            _pointerDebugColor = ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(_pointerDebugColorKey, "0000FF"), out Color pointerDebugColor) ? pointerDebugColor : Color.blue;
            _graspAreaDebugColor = ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(_graspAreaDebugColorKey, "00FF00"), out Color graspAreaDebugColor) ? graspAreaDebugColor : Color.green;
        }

        static SvenHelper()
        {
            RefreshHelper();
        }
#endif
    }
}