#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(UCCAbilityAvailabilityMonitor))]
public class UCCAbilityAvailabilityMonitorEditor : Editor
{
    private SerializedProperty characterProperty;
    private SerializedProperty checkIntervalProperty;
    private SerializedProperty onlyCheckEnabledAbilitiesProperty;
    private SerializedProperty hideWhileAbilityIsActiveProperty;
    private SerializedProperty raiseInitialPossibleEventsProperty;
    private SerializedProperty raiseInitialImpossibleEventsProperty;
    private SerializedProperty bindingsProperty;

    private ReorderableList bindingsList;

    private const float Padding = 4f;

    private void OnEnable()
    {
        characterProperty = serializedObject.FindProperty("character");
        checkIntervalProperty = serializedObject.FindProperty("checkInterval");
        onlyCheckEnabledAbilitiesProperty = serializedObject.FindProperty("onlyCheckEnabledAbilities");
        hideWhileAbilityIsActiveProperty = serializedObject.FindProperty("hideWhileAbilityIsActive");
        raiseInitialPossibleEventsProperty = serializedObject.FindProperty("raiseInitialPossibleEvents");
        raiseInitialImpossibleEventsProperty = serializedObject.FindProperty("raiseInitialImpossibleEvents");
        bindingsProperty = serializedObject.FindProperty("bindings");

        bindingsList = new ReorderableList(serializedObject, bindingsProperty, true, true, true, true);

        bindingsList.drawHeaderCallback = DrawBindingsHeader;
        bindingsList.drawElementCallback = DrawBindingElement;
        bindingsList.elementHeightCallback = GetBindingElementHeight;
        bindingsList.onAddCallback = AddBinding;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(characterProperty);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Ability Checks", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(checkIntervalProperty);
        EditorGUILayout.PropertyField(onlyCheckEnabledAbilitiesProperty);
        EditorGUILayout.PropertyField(hideWhileAbilityIsActiveProperty);
        EditorGUILayout.PropertyField(raiseInitialPossibleEventsProperty);
        EditorGUILayout.PropertyField(raiseInitialImpossibleEventsProperty);
        
        EditorGUILayout.Space(8f);

        DrawAbilityInfoBox();

        EditorGUILayout.Space(4f);

        bindingsList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBindingsHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Ability Possible Effects");
    }

    private float GetBindingElementHeight(int index)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        return Padding
               + line + spacing
               + line + spacing
               + line + spacing
               + Padding;
    }

    private void DrawBindingElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = bindingsProperty.GetArrayElementAtIndex(index);

        SerializedProperty abilityTypeNameProperty =
            element.FindPropertyRelative("abilityTypeName");

        SerializedProperty onBecamePossibleProperty =
            element.FindPropertyRelative("onBecamePossible");

        SerializedProperty onBecameImpossibleProperty =
            element.FindPropertyRelative("onBecameImpossible");

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        rect.y += Padding;
        rect.height = line;

        Rect abilityRect = new Rect(rect.x, rect.y, rect.width, line);
        DrawAbilityDropdown(abilityRect, abilityTypeNameProperty);

        rect.y += line + spacing;

        Rect possibleRect = new Rect(rect.x, rect.y, rect.width, line);
        EditorGUI.PropertyField(
            possibleRect,
            onBecamePossibleProperty,
            new GUIContent("Effect on IsUsable"),
            false);

        rect.y += line + spacing;

        Rect impossibleRect = new Rect(rect.x, rect.y, rect.width, line);
        EditorGUI.PropertyField(
            impossibleRect,
            onBecameImpossibleProperty,
            new GUIContent("Effect on NotUsable"),
            false);
    }

    private void AddBinding(ReorderableList list)
    {
        bindingsProperty.arraySize++;

        SerializedProperty element =
            bindingsProperty.GetArrayElementAtIndex(bindingsProperty.arraySize - 1);

        element.FindPropertyRelative("abilityTypeName").stringValue = string.Empty;

        SerializedProperty possible =
            element.FindPropertyRelative("onBecamePossible")
                .FindPropertyRelative("assemblyQualifiedTypeName");

        SerializedProperty impossible =
            element.FindPropertyRelative("onBecameImpossible")
                .FindPropertyRelative("assemblyQualifiedTypeName");

        possible.stringValue = string.Empty;
        impossible.stringValue = string.Empty;
    }

    private void DrawAbilityDropdown(Rect rect, SerializedProperty abilityTypeNameProperty)
    {
        AbilityTypeOption[] options = GetAbilityOptions();

        if (options.Length == 0)
        {
            EditorGUI.PropertyField(
                rect,
                abilityTypeNameProperty,
                new GUIContent("Ability Type Name"));

            return;
        }

        string currentValue = abilityTypeNameProperty.stringValue;

        List<string> labels = new List<string> { "[None]" };
        List<string> values = new List<string> { string.Empty };

        foreach (AbilityTypeOption option in options)
        {
            labels.Add(option.DisplayName);
            values.Add(option.StoredValue);
        }

        int currentIndex = values.FindIndex(value => value == currentValue);

        if (currentIndex < 0)
        {
            labels.Add($"[Missing] {currentValue}");
            values.Add(currentValue);
            currentIndex = values.Count - 1;
        }

        int newIndex = EditorGUI.Popup(
            rect,
            "Ability",
            currentIndex,
            labels.ToArray());

        abilityTypeNameProperty.stringValue = values[newIndex];
    }

    private AbilityTypeOption[] GetAbilityOptions()
    {
        GameObject characterObject = characterProperty.objectReferenceValue as GameObject;

        if (characterObject == null)
        {
            UCCAbilityAvailabilityMonitor monitor =
                (UCCAbilityAvailabilityMonitor)target;

            characterObject = monitor.gameObject;
        }

        UltimateCharacterLocomotion locomotion =
            characterObject.GetComponent<UltimateCharacterLocomotion>();

        if (locomotion == null || locomotion.Abilities == null)
            return Array.Empty<AbilityTypeOption>();

        return locomotion.Abilities
            .Where(ability => ability != null)
            .Select((ability, index) =>
            {
                Type type = ability.GetType();

                return new AbilityTypeOption
                {
                    DisplayName = $"{index}: {type.Name}",
                    StoredValue = type.FullName
                };
            })
            .ToArray();
    }

    private void DrawAbilityInfoBox()
    {
        GameObject characterObject = characterProperty.objectReferenceValue as GameObject;

        if (characterObject == null)
        {
            UCCAbilityAvailabilityMonitor monitor =
                (UCCAbilityAvailabilityMonitor)target;

            characterObject = monitor.gameObject;
        }

        if (characterObject == null)
            return;

        UltimateCharacterLocomotion locomotion =
            characterObject.GetComponent<UltimateCharacterLocomotion>();

        if (locomotion == null)
        {
            EditorGUILayout.HelpBox(
                "No UltimateCharacterLocomotion found on the selected character.",
                MessageType.Warning);
            return;
        }

        int abilityCount = locomotion.Abilities != null ? locomotion.Abilities.Length : 0;

        EditorGUILayout.HelpBox(
            $"Found {abilityCount} abilities on '{characterObject.name}'. Each binding raises Start Effect when CanStartAbility() becomes true and Stop Effect when it becomes false.",
            MessageType.Info);
    }

    private struct AbilityTypeOption
    {
        public string DisplayName;
        public string StoredValue;
    }
}
#endif