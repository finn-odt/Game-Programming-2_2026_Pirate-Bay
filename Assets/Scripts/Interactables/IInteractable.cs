using System;
using System.Collections.Generic;
using GameEvents;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityConstantsGenerator;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class IInteractable : MonoBehaviour
{
    protected bool playerInTrigger = false;
    protected bool isGamePaused = false;
    
    protected bool canBeInteractedWith = true;
    
    [SerializeField] protected GameEventTypeReference eventOnTriggerEnter;
    
    protected void OnEnable()
    {
        GameEventManager.AddListener<PlayerInteractionRequestEvent>(OnInteraction);  // called by ThirdPersonController
        
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        
        OnEnabled();  // for inheritance
        OnDisabled();  // for inheritance
    }

    protected virtual void OnEnabled()
    {
        // Optional extension point for subclasses
    }
    protected virtual void OnDisabled()
    {
        // Optional extension point for subclasses
    }

    protected void OnDisable()
    {
        GameEventManager.RemoveListener<PlayerInteractionRequestEvent>(OnInteraction);  // called by ThirdPersonController
        
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
    }

    private void OnInteraction(PlayerInteractionRequestEvent e)  // wrapper for Pause-Handling
    {
        if (isGamePaused || !playerInTrigger || !canBeInteractedWith)
            return;
        
        OnPlayerInteraction(e);
    }
    protected abstract void OnPlayerInteraction(PlayerInteractionRequestEvent e);


    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (isGamePaused)
            return;
        
        if(other.gameObject.layer == (int)LayerId.Player) {
            playerInTrigger = true;
            if (canBeInteractedWith)
            {
                GameEventManager.Raise(new InteractionPossibleEvent(true, gameObject));
                if (eventOnTriggerEnter != null && eventOnTriggerEnter.Type != null)
                    eventOnTriggerEnter.Raise(); // raise this Game Event Reference
            }
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (isGamePaused)
            return;
        
        if(other.gameObject.layer == (int)LayerId.Player) {
            playerInTrigger = false;
            GameEventManager.Raise(new InteractionPossibleEvent(false, gameObject));
        }
    }
}