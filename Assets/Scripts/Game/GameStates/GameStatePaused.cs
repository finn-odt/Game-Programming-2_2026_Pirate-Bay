using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class GameStatePaused : IFSMState<GameManager>
{
    
    public void Enter(GameManager e)
    {
        e.LockCursor(false);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Paused));
    }

    public void Reason(GameManager e)
    {        
        // Unpaused?
        if (!e.isGamePaused)
        {
            if (e.GetPreviousStateType() == typeof(GameStateInventory))
                GameEventManager.Raise(new OpenInventoryEvent());
            e.BackToPreviousState();
        }
    }

    public void Update(GameManager e)
    {
        
    }

    public void Exit(GameManager e) {}
}