using System;
using System.Collections;
using System.Collections.Generic;
using GameEvents;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIEnableOnEvent : MonoBehaviour
{
    [SerializeField] private bool enableOnConnectionMode = false;
    [SerializeField, ShowIf(nameof(enableOnConnectionMode))] private DeviceConnectionMode enableOnThisConnectionMode;
    [SerializeField] private bool disableOnConnectionMode = false;
    [SerializeField, ShowIf(nameof(disableOnConnectionMode))] private DeviceConnectionMode disableOnThisConnectionMode;
    
    [SerializeField] private bool changeOnEvent = false;
    [SerializeField, ShowIf(nameof(changeOnEvent)), LabelText("Enable on this or child")] private GameEventTypeReference enableOnThisEvent;
    private bool HasEnableEvent => changeOnEvent && enableOnThisEvent != null && enableOnThisEvent.Type != null;
    [SerializeField, ShowIf(nameof(changeOnEvent)), LabelText("Disable on this or child")] private GameEventTypeReference disableOnThisEvent;
    private bool HasDisableEvent => changeOnEvent && disableOnThisEvent != null && disableOnThisEvent.Type != null;
   
    private readonly HashSet<Type> registeredEventTypes = new();
    [SerializeField, LabelText("Event-State is enabled from the start:")] private bool enabledByEvent = false;
    private DeviceConnectionMode _lastConnectionMode;
    private bool enabledByMode = false;
    
    private void Awake()
    {
        GameEventManager.AddListener<ConnectionModeChangedEvent>(UseControlScheme);

        if (HasEnableEvent)
        {
            if (registeredEventTypes.Add(enableOnThisEvent.Type))  // returns false, if already registered
                GameEventManager.AddListener(enableOnThisEvent.Type, OnEventTrigger);
        }
        if (HasDisableEvent)
        {
            if (registeredEventTypes.Add(disableOnThisEvent.Type))  // returns false, if already registered
                GameEventManager.AddListener(disableOnThisEvent.Type, OnEventTrigger);
        }
    }
    
    private void UseControlScheme(ConnectionModeChangedEvent e)
    {
        if (_lastConnectionMode == e.newMode)
            return;
        
        _lastConnectionMode = e.newMode;

        if (enableOnConnectionMode)
        {
            bool enableIrrelevant = e.newMode == DeviceConnectionMode.Mixed || enableOnThisConnectionMode == DeviceConnectionMode.Mixed; // ENABLE
            bool enableEqual = enableOnThisConnectionMode == e.newMode; // ENABLE

            if (enableIrrelevant || enableEqual)
                enabledByMode = true;
        }

        if (disableOnConnectionMode)
        {
            bool disableIrrelevant = e.newMode == DeviceConnectionMode.Mixed || disableOnThisConnectionMode == DeviceConnectionMode.Mixed; // DISABLE
            bool disableEqual = disableOnThisConnectionMode == e.newMode; // ENABLE

            if (disableIrrelevant || disableEqual)
                enabledByMode = false;
        }

        // refresh
        RefreshState();
    }

    private void OnEventTrigger(GameEvent e)
    {
        // should not be registered and called, if no event-triggers are set
        Type eventType = e.GetType();

        if (HasEnableEvent && IsSameOrChildOf(eventType, enableOnThisEvent.Type)) // enable triggered, disable can be cleared
            enabledByEvent = true;
        if(HasDisableEvent && IsSameOrChildOf(eventType, disableOnThisEvent.Type))  // enable triggered, disable can be cleared
            enabledByEvent = false;
        
        // refresh
        RefreshState();
    }

    private void RefreshState()
    {
        bool noEvents = !HasEnableEvent && !HasDisableEvent;
        bool eventsEnable = (HasDisableEvent || HasEnableEvent) && enabledByEvent;
        
        bool noModes = !enableOnConnectionMode && !disableOnConnectionMode;
        bool modeEnable = (enableOnConnectionMode || disableOnConnectionMode) && enabledByMode;

        if ((noEvents || eventsEnable) && (noModes || modeEnable))
            gameObject.SetActive(true);
        else
            gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        GameEventManager.RemoveListener<ConnectionModeChangedEvent>(UseControlScheme);
        
        foreach (Type eventType in registeredEventTypes)
        {
            GameEventManager.RemoveListener(eventType, OnEventTrigger);
        }

        registeredEventTypes.Clear();
    }
    
    public static bool IsSameOrChildOf(
        GameEventTypeReference possibleChild,
        GameEventTypeReference possibleParent)
    {
        if (possibleChild == null || possibleParent == null)
            return false;

        Type childType = possibleChild.Type;
        Type parentType = possibleParent.Type;

        if (childType == null || parentType == null)
            return false;

        return parentType.IsAssignableFrom(childType);
    }
    
    public static bool IsSameOrChildOf(
        Type childType,
        Type parentType)
    {
        if (childType == null || parentType == null)
            return false;

        return parentType.IsAssignableFrom(childType);
    }
}
