using UnityEngine;
using UnityEditor;
using CurvedTextEasyNamespace = CurvedTextEasy;

namespace CurvedTextEasy.Editor
{
    /// <summary>
    /// Custom Inspector editor for CurvedTextEasy
    /// </summary>
    [CustomEditor(typeof(CurvedTextEasyNamespace.CurvedTextEasy))]
    public class CurvedTextEasyEditor : UnityEditor.Editor
    {
        private SerializedProperty curveMode;
        private SerializedProperty rotationStrength;
        private SerializedProperty curveAxis;
        private SerializedProperty vertexCurve;
        private SerializedProperty curveScaling;
        private SerializedProperty radius;
        private SerializedProperty fromArcDegrees;
        private SerializedProperty toArcDegrees;
        private SerializedProperty maxDegreesPerLetter;
        private SerializedProperty showDebugInfo;

        private void OnEnable()
        {
            curveMode = serializedObject.FindProperty("curveMode");
            rotationStrength = serializedObject.FindProperty("_rotationStrength");
            curveAxis = serializedObject.FindProperty("curveAxis");
            vertexCurve = serializedObject.FindProperty("vertexCurve");
            curveScaling = serializedObject.FindProperty("_curveScaling");
            radius = serializedObject.FindProperty("_radius");
            fromArcDegrees = serializedObject.FindProperty("_fromArcDegrees");
            toArcDegrees = serializedObject.FindProperty("_toArcDegrees");
            maxDegreesPerLetter = serializedObject.FindProperty("_maxDegreesPerLetter");
            showDebugInfo = serializedObject.FindProperty("showDebugInfo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(curveMode, new GUIContent("Curve Mode", "Select the text curve mode"));

            EditorGUILayout.PropertyField(curveAxis, new GUIContent("Curve Axis", "Select the curve axis (Y = vertical curve, Z = circular wrap)"));
            EditorGUILayout.PropertyField(rotationStrength, new GUIContent("Rotation Strength", "Text rotation strength (0 = text stays horizontal, 1 = text fully follows curve rotation)"));

            if (curveMode.enumValueIndex == (int)CurvedTextEasyNamespace.CurvedTextEasy.CurveMode.Curve)
            {
                EditorGUILayout.PropertyField(vertexCurve, new GUIContent("Curve", "Animation curve for text bending"));

                EditorGUILayout.LabelField("Curve Presets", EditorStyles.boldLabel);

                float buttonWidth = (EditorGUIUtility.currentViewWidth - 30f) * 0.5f;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Arc Up", GUILayout.Width(buttonWidth)))
                {
                    ApplyCurvePreset_ArcUp();
                }
                if (GUILayout.Button("Arc Down", GUILayout.Width(buttonWidth)))
                {
                    ApplyCurvePreset_ArcDown();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("S Shape", GUILayout.Width(buttonWidth)))
                {
                    ApplyCurvePreset_SShape();
                }
                if (GUILayout.Button("Wave", GUILayout.Width(buttonWidth)))
                {
                    ApplyCurvePreset_Wave();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Left High", GUILayout.Width(buttonWidth)))
                {
                    ApplyCurvePreset_LeftHigh();
                }
                if (GUILayout.Button("Right High", GUILayout.Width(buttonWidth)))
                {
                    ApplyCurvePreset_RightHigh();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                string scalingLabel = curveAxis.enumValueIndex == (int)CurvedTextEasyNamespace.CurvedTextEasy.CurveAxis.Y ? "Y Axis Scaling" : "Z Axis Scaling";
                string scalingTooltip = curveAxis.enumValueIndex == (int)CurvedTextEasyNamespace.CurvedTextEasy.CurveAxis.Y ?
                    "Y-axis curve scaling coefficient (vertical direction)" :
                    "Z-axis curve scaling coefficient (circular wrap direction)";
                EditorGUILayout.PropertyField(curveScaling, new GUIContent(scalingLabel, scalingTooltip));
            }
            else
            {
                EditorGUILayout.PropertyField(radius, new GUIContent("Radius", "Radius of the circular arc"));
                EditorGUILayout.PropertyField(fromArcDegrees, new GUIContent("Start Angle", "Starting angle of text (degrees). 0°=left, 90°=top, 180°=right, 270°=bottom"));
                EditorGUILayout.PropertyField(toArcDegrees, new GUIContent("End Angle", "Ending angle of text (degrees). 0°=left, 90°=top, 180°=right, 270°=bottom"));
                EditorGUILayout.PropertyField(maxDegreesPerLetter, new GUIContent("Max Letter Spacing", "Maximum angular distance between each letter"));

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Angle Direction: 0°=left, 90°=top, 180°=right, 270°=bottom\nStart Angle < End Angle = Counterclockwise\nStart Angle > End Angle = Clockwise", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(showDebugInfo, new GUIContent("Show Debug Info", "Enable detailed debug logging in Console"));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Apply Arc Up preset </summary>
        private void ApplyCurvePreset_ArcUp()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0, 0, 0, 30, 0, 0.01f),
                new Keyframe(0.5f, 0.25f),
                new Keyframe(1, 0, -30, 0, 0.01f, 0)
            );
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            vertexCurve.animationCurveValue = curve;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Apply Arc Down preset </summary>
        private void ApplyCurvePreset_ArcDown()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0, 0, 0, -30, 0, 0.01f),
                new Keyframe(0.5f, -0.25f),
                new Keyframe(1, 0, 30, 0, 0.01f, 0)
            );
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            vertexCurve.animationCurveValue = curve;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Apply S Shape preset </summary>
        private void ApplyCurvePreset_SShape()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0, -0.15f, 0, 0),
                new Keyframe(0.25f, -0.1f),
                new Keyframe(0.5f, 0),
                new Keyframe(0.75f, 0.1f),
                new Keyframe(1, 0.15f, 0, 0)
            );
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            vertexCurve.animationCurveValue = curve;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Apply Wave preset </summary>
        private void ApplyCurvePreset_Wave()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.25f, 0.15f),
                new Keyframe(0.5f, 0),
                new Keyframe(0.75f, -0.15f),
                new Keyframe(1, 0)
            );
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            vertexCurve.animationCurveValue = curve;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Apply Left High preset </summary>
        private void ApplyCurvePreset_LeftHigh()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0, 0.3f, 0, -0.5f),
                new Keyframe(0.5f, 0.1f, -0.5f, -0.5f),
                new Keyframe(1, 0, -0.5f, 0)
            );
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            vertexCurve.animationCurveValue = curve;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Apply Right High preset </summary>
        private void ApplyCurvePreset_RightHigh()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0, 0, 0, 0.5f),
                new Keyframe(0.5f, 0.1f, 0.5f, 0.5f),
                new Keyframe(1, 0.3f, 0.5f, 0)
            );
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            vertexCurve.animationCurveValue = curve;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
