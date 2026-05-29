using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class GameStateIntro : IFSMState<GameManager>
{
    
    public void Enter(GameManager e)
    {
        e.LockCursor(true);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Intro));
    }

    public void Reason(GameManager e)
    {
        
        // Paused?
        if(e.isGamePaused)
            e.ChangeState(new GameStatePaused());
        
        // TODO: implement Intro??
        e.ChangeState(new GameStatePlay());
    }

    public void Update(GameManager e)
    {
        
    }

    public void Exit(GameManager e) {}
}