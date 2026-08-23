using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using Systems.SceneManagement;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GameStateWon : IFSMState<GameManager>
{
    private float timeOfWinning = -1;
    
    public void Enter(GameManager e)
    {
        e.LockCursor(true);
        GameEventManager.Raise(new GameStateChangedEvent(GameStateChangedEvent.GameState.Won));
        timeOfWinning = Time.time;
    }

    public void Reason(GameManager e)
    {
        
    }

    public void Update(GameManager e)
    {
        if((Time.time - timeOfWinning) >= 3f)  // wait 3 seconds
            SceneLoader.Instance.LoadSceneGroup(SceneLoader.Instance.ActiveSceneGroupIndex + 1);  // go to Outro Cut Scene
    }

    public void Exit(GameManager e) {}
}