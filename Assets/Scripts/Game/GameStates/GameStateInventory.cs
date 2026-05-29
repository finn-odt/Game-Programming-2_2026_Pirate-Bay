using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class GameStateInventory : IFSMState<GameManager>
{
    
    public void Enter(GameManager e)
    {
        e.LockCursor(false);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Inventory));
    }

    public void Reason(GameManager e)
    {
        // Inventory Closed?
        if (!e.isInventoryOpen)
            e.ChangeState(new GameStatePlay());
        // Lost?
        else if(e.gameOver)
            e.ChangeState(new GameStateLost());
        // Won?
        else if(e.reachedGoal)
            e.ChangeState(new GameStateWon());
        // Paused?
        else if(e.isGamePaused)
            e.ChangeState(new GameStatePaused());
    }

    public void Update(GameManager e)
    {
        
    }

    public void Exit(GameManager e)
    {
        GameEventManager.Raise(new CloseInventoryEvent());
    }
}