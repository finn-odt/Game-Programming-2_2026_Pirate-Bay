using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using Unity.VectorGraphics;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GameStateLost : IFSMState<GameManager>
{
    
    public void Enter(GameManager e)
    {
        e.LockCursor(true);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Lost));
    }

    public void Reason(GameManager e)
    {
        
    }

    public void Update(GameManager e)
    {
        
    }

    public void Exit(GameManager e) {}
}