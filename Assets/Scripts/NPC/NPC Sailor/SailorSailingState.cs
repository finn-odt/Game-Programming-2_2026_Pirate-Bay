using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class SailorSailingState : IFSMState<NPCSailorBehaviour>
{
    
    public void Enter(NPCSailorBehaviour e)
    {
        GameEventManager.Raise(new SpentCoinsEvent(e.sailingCost));
        e.shipBehaviour.sailingActive = true;
        e.shipBehaviour.reachedDestination = false;
    }

    public void Reason(NPCSailorBehaviour e)
    {
        if (e.isGamePaused)
            return;
        
        if(e.shipBehaviour.reachedDestination)
            e.ChangeState(new SailorIdleState());
    }


    public void Update(NPCSailorBehaviour e)
    {
        
    }

    public void Exit(NPCSailorBehaviour e)
    {
        e.shipBehaviour.sailingActive = false;
        e.shipBehaviour.reachedDestination = false;
    }
}