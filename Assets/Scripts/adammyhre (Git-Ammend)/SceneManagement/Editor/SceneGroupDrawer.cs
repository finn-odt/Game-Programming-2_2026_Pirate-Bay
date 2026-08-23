using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Systems.SceneManagement.Editor
{
    [CustomPropertyDrawer(typeof(SceneGroupAttribute))]
    public class SceneGroupDrawer : PropertyDrawer
    {
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(
                    position,
                    label.text,
                    "[SceneGroup] requires an int."
                );

                return;
            }

            SceneGroupCollection collection =
                FindSceneGroupCollection();

            if (collection == null || collection.Count == 0)
            {
                EditorGUI.HelpBox(
                    position,
                    "No SceneGroupCollection found.",
                    MessageType.Warning
                );

                return;
            }

            string[] names = collection.SceneGroups
                .Select(group => group.GroupName)
                .ToArray();

            property.intValue = EditorGUI.Popup(
                position,
                label.text,
                property.intValue,
                names
            );
        }

        private static SceneGroupCollection FindSceneGroupCollection()
        {
            string[] guids =
                AssetDatabase.FindAssets("t:SceneGroupCollection");

            if (guids.Length == 0)
                return null;

            string path =
                AssetDatabase.GUIDToAssetPath(guids[0]);

            return AssetDatabase.LoadAssetAtPath<SceneGroupCollection>(
                path
            );
        }
    }
}