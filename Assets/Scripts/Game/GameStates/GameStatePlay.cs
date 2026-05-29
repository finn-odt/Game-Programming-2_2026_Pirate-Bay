using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class GameStatePlay : IFSMState<GameManager>
{
    
    public void Enter(GameManager e)
    {
        e.LockCursor(true);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Play));
    }

    public void Reason(GameManager e)
    {
        // Inventory Open?
        if(e.isInventoryOpen)
            e.ChangeState(new GameStateInventory());
        
        // Lost?
        if(e.gameOver)
            e.ChangeState(new GameStateLost());
        
        // Won?
        if(e.reachedGoal)
            e.ChangeState(new GameStateWon());
        
        // Paused?
        if(e.isGamePaused)
            e.ChangeState(new GameStatePaused());
        
        //e.ChangeState(new GameStatePlay());
    }

    public void Update(GameManager e)
    {
        
    }

    public void Exit(GameManager e) {}
}