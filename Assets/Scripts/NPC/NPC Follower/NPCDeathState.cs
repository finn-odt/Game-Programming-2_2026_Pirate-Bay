using System;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;

public class NPCDeathState : IFSMState<NPCFollowerBehaviour>
{
    
    public void Enter(NPCFollowerBehaviour e)
    {
        Debug.LogWarning("NPC is DEAD !");
    }

    public void Reason(NPCFollowerBehaviour e)
    {
        if (e.isGamePaused)
            return;
        
        
    }

    public void Update(NPCFollowerBehaviour e)
    {
        
    }

    public void Exit(NPCFollowerBehaviour e) {}

}