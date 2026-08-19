using System;
using System.Collections;
using System.Collections.Generic;
using GameEvents;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextUIInfillHelper : MonoBehaviour
{
    public enum DeactivateTriggerMode
    {
        OnlyWithConnection,
        AlsoAfterTime,
        AlsoOnEvent
    }
    
    [Serializable]
    public class TextPart
    {
        [SerializeField, HideInInspector] private string id;
        public string Id => id;

        public void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }
        
        [Serializable]
        public class DeactivateTrigger
        {
            public DeactivateTriggerMode deactivateTrigger;
            public bool ShouldDeactivateAfterSeconds => deactivateTrigger == DeactivateTriggerMode.AlsoAfterTime;
            public bool ShouldDeactivateOnEvent => deactivateTrigger == DeactivateTriggerMode.AlsoOnEvent;
            public bool StaysDeactivated => deactivateTrigger == DeactivateTriggerMode.OnlyWithConnection;
            [ShowIf(nameof(ShouldDeactivateOnEvent))] public GameEventTypeReference deactivateOnThisEvent;
            public bool HasEventStopTrigger => deactivateOnThisEvent != null && deactivateOnThisEvent.Type != null;
            [ShowIf(nameof(ShouldDeactivateAfterSeconds))] public float deactivateAfterSeconds;
        }

        [Serializable]
        public class ActivateTrigger
        {
            public GameEventTypeReference activateOnThisEvent;
            public bool HasEventStartTrigger => activateOnThisEvent != null && activateOnThisEvent.Type != null;
            [HideInInspector] public bool isActivated;

            [ShowIf(nameof(HasEventStartTrigger))] public DeactivateTrigger deactivateTrigger;
        }
        
        public string text;
        public DeviceConnectionMode inputCondition;

        [Header("Additional Triggers")]
        public ActivateTrigger eventTriggers;

        [InfoBox("If there is no critical active TextPart, nothing is shown.")]
        public bool critical;
        
        public TextPart(string text, DeviceConnectionMode inputCondition, ActivateTrigger eventTriggers)
        {
            this.text = text;
            this.inputCondition = inputCondition;
            this.eventTriggers = eventTriggers;
        }
    }

    [SerializeField] private List<TextPart> TextParts;
    [SerializeField] private string fillerBetweenTextParts = "";
    
    private TextMeshProUGUI uiText;
    
    private readonly HashSet<Type> registeredEventTypes = new();

    private DeviceConnectionMode _lastConnectionMode;
    
    private void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        GameEventManager.AddListener<ConnectionModeChangedEvent>(UseControlScheme);
        
        // check whether any TextParts need EventListeners (& add them)
        for (int i = 0; i < TextParts.Count; i++)
        {
            TextParts[i].EnsureId();  // ensure there is a unique id for every TextPart
            
            Type eventType = TextParts[i].eventTriggers.activateOnThisEvent.Type;

            if (eventType == null)
                continue;

            if (!registeredEventTypes.Add(eventType))
                continue;

            GameEventManager.AddListener(eventType, OnEventTrigger);
        }
    }

    private void OnEventTrigger(GameEvent e)
    {
        Type eventType = e.GetType();
        
        for (int i = 0; i < TextParts.Count; i++)
        {
            TextPart part = TextParts[i];  // only for GET (shorter variable form/name)
            
            if (!part.eventTriggers.isActivated && part.eventTriggers.HasEventStartTrigger &&  // not activated & has Start-Event
                eventType == part.eventTriggers.activateOnThisEvent.Type)  // & event fired
            {
                // activate due to occured event
                TextParts[i].eventTriggers.isActivated = true;

                if (part.eventTriggers.deactivateTrigger.ShouldDeactivateAfterSeconds)
                    StartCoroutine(DeactivatePartAfterDelay(TextParts[i], part.eventTriggers.deactivateTrigger.deactivateAfterSeconds));
            }

            if (part.eventTriggers.isActivated &&  // activated
                part.eventTriggers.deactivateTrigger.HasEventStopTrigger && part.eventTriggers.deactivateTrigger.ShouldDeactivateOnEvent && // & has Stop-Event
                eventType == part.eventTriggers.deactivateTrigger.deactivateOnThisEvent.Type)  // & event fired
            {
                // deactivate due to occured event
                TextParts[i].eventTriggers.isActivated = false;
            }
        }
        
        RefreshText();
    }

    private IEnumerator DeactivatePartAfterDelay(TextPart part, float delay)
    {
        yield return new WaitForSeconds(delay);

        int index = TextParts.FindIndex(p => p.Id == part.Id);

        if (index < 0)
            yield break;

        TextParts[index].eventTriggers.isActivated = false;
        
        Debug.Log($"Part '{TextParts[index].text}' deactivated after {delay} seconds.");
        
        RefreshText();
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

    private void UseControlScheme(ConnectionModeChangedEvent e)
    {
        _lastConnectionMode = e.newMode;
        RefreshText();
    }

    private bool DoesAnyActiveCriticalPartExist()
    {
        for (int i = 0; i < TextParts.Count; i++)
            if (ShouldBeDisplayed(TextParts[i]) && TextParts[i].critical)
                return true;
        return false;
    }

    private bool ShouldBeDisplayed(TextPart part)
    {
        return (part.inputCondition == DeviceConnectionMode.Mixed || part.inputCondition == _lastConnectionMode) // is connection mode compatible
               && (!part.eventTriggers.HasEventStartTrigger || // & (has no event trigger
                   part.eventTriggers.HasEventStartTrigger && part.eventTriggers.isActivated);  // OR is activated)
    }
    
    private void RefreshText() 
    {
        if(uiText == null)
            return;

        if (!DoesAnyActiveCriticalPartExist())
        {
            uiText.text = "";
            return;
        }
        
        string output = "";
        for (int i = 0; i < TextParts.Count; i++)
        {
            TextPart part = TextParts[i];  // just a copy ! (only for GET, not SET)
            
            Debug.Log($"Connection Mode Compatible: {(part.inputCondition == DeviceConnectionMode.Mixed || part.inputCondition == _lastConnectionMode)}, No Even Trigger {!part.eventTriggers.HasEventStartTrigger}, Is activated: {part.eventTriggers.HasEventStartTrigger && part.eventTriggers.isActivated}");
            if (ShouldBeDisplayed(part))
                output += part.text + fillerBetweenTextParts;
        }
        uiText.text = output;
    }
}
