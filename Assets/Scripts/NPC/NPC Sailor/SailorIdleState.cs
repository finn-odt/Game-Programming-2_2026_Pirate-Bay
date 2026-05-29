using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class SailorIdleState : IFSMState<NPCSailorBehaviour>
{
    
    public void Enter(NPCSailorBehaviour e) {}

    public void Reason(NPCSailorBehaviour e)
    {
        if (e.isGamePaused)
            return;

        if (e.playerInTrigger && e.playerWantsToTalk)
        {
            e.ChangeState(new SailorAddressedState());
        }
    }


    public void Update(NPCSailorBehaviour e)
    {
        e.playerWantsToTalk = false;  // reset to avoid interacting somewhere else and here activating
    }

    public void Exit(NPCSailorBehaviour e)
    {
        // reset
        e.playerWantsToTalk = false;
        GameEventManager.Raise(new InteractionPossibleEvent(false, e.gameObject));
    }
}