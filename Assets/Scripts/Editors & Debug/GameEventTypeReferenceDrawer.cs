#if UNITY_EDITOR
using System;
using System.Linq;
using GameEvents;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameEventTypeReference))]
public class GameEventTypeReferenceDrawer : PropertyDrawer
{
    private Type[] eventTypes;
    private GUIContent[] displayNames;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureTypesLoaded();

        SerializedProperty typeNameProperty =
            property.FindPropertyRelative("assemblyQualifiedTypeName");

        int currentIndex = GetCurrentIndex(typeNameProperty.stringValue);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();

        int newIndex = EditorGUI.Popup(position, label, currentIndex, displayNames);

        if (EditorGUI.EndChangeCheck())
        {
            typeNameProperty.stringValue = newIndex == 0
                ? string.Empty
                : eventTypes[newIndex - 1].AssemblyQualifiedName;
        }

        EditorGUI.EndProperty();
    }

    private int GetCurrentIndex(string assemblyQualifiedTypeName)
    {
        if (string.IsNullOrEmpty(assemblyQualifiedTypeName))
            return 0;

        Type currentType = Type.GetType(assemblyQualifiedTypeName);

        if (!GameEventTypeReference.IsRaiseableEventType(currentType))
            return 0;

        int foundIndex = Array.IndexOf(eventTypes, currentType);
        return foundIndex >= 0 ? foundIndex + 1 : 0;
    }

    private void EnsureTypesLoaded()
    {
        if (eventTypes != null && displayNames != null)
            return;

        eventTypes = TypeCache.GetTypesDerivedFrom<GameEvent>()
            .Where(GameEventTypeReference.IsRaiseableEventType)
            .OrderBy(type => type.FullName)
            .ToArray();

        displayNames = new GUIContent[eventTypes.Length + 1];
        displayNames[0] = new GUIContent("[None]");

        for (int i = 0; i < eventTypes.Length; i++)
        {
            displayNames[i + 1] = new GUIContent(GetDisplayName(eventTypes[i]));
        }
    }

    private static string GetDisplayName(Type type)
    {
        if (string.IsNullOrEmpty(type.Namespace))
            return type.Name;

        return $"{type.Namespace.Replace('.', '/')}/{type.Name}";
    }
}
#endif