using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GameStateWon : IFSMState<GameManager>
{
    
    public void Enter(GameManager e)
    {
        e.LockCursor(true);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Won));
    }

    public void Reason(GameManager e)
    {
        // never change, GameManager.Reset() will change state
        if(e.restartRequested)
        {
            // reset GameManager
            e.ResetGame();
        }
    }

    public void Update(GameManager e)
    {
        
    }

    public void Exit(GameManager e) {}
}