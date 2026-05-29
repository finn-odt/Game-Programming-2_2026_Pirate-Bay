using System;
using System.Collections.Generic;
using System.Linq;
using GameEvents;
using StarterAssets;
using TriInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class SailorAddressedState : IFSMState<NPCSailorBehaviour>
{
    private static readonly string[] possibleTexts =
    {
        "Need a ride across the waters, aye?",
        "Climb aboard, matey — shall we make for the other islands?",
        "Want me to ship ye over to the other islands?",
        "Ready to set sail for the other islands?",
        "Shall I take ye across to the other islands, aye?",
        "Need passage to the other islands, matey?",
        "Shall I ferry ye to the other islands, aye?"
    };

    private static readonly string priceTextInit = "Only XXX Doubloons, aye!";
    private static string priceText = "Only XXX Doubloons, aye!";

    // if true, conversationAnswered was set false again and is the trigger for departure
    private bool waitingForDeparture = false;
    
    public void Enter(NPCSailorBehaviour e)
    {
        e.conversationAnswered = false;
        
        // set costs for sailing into text
        priceText = priceTextInit.Replace("XXX", e.sailingCost.ToString());
        
        // use random conversation text
        string talkText = possibleTexts[Random.Range(0, possibleTexts.Length)] + "\n" + priceText;
        GameEventManager.Raise(new ConversationUIEvent(talkText, true));
    }

    public void Reason(NPCSailorBehaviour e)
    {
        if (e.isGamePaused)
            return;

        if (Vector3.Distance(e.player.Position, e.transform.position) > e.maxDistanceWhileConversation)
        {
            e.ChangeState(new SailorIdleState());
            GameEventManager.Raise(new ConversationUIEvent("", false));
            return;
        }

        if (waitingForDeparture)  // wait for conversation end
        {
            if(e.conversationAnswered)
                e.ChangeState(new SailorSailingState());  // DEPARTURE
            return;
        }

        Debug.Log((e.conversationAnswered ? "Answered" : "Not Answered") + " -> " + (e.conversationAccepted ? "Accepted" : "Rejected"));
        if (e.conversationAnswered && e.conversationAccepted)
        {
            if (e.player.CollectedCoins > e.sailingCost)
            {
                // enough money??
                waitingForDeparture = true;
                e.conversationAnswered = false;
                GameEventManager.Raise(new ConversationUIEvent("Yeehah, let's go!", true));
            }
            else
            {
                GameEventManager.Raise(new ConversationUIEvent("Ye don't 'ave enough doubloons! Walk the plank!", true));
                e.ChangeState(new SailorIdleState());
            }
        } else if(e.conversationAnswered && !e.conversationAccepted)
            e.ChangeState(new SailorIdleState());
    }


    public void Update(NPCSailorBehaviour e) {}

    public void Exit(NPCSailorBehaviour e)
    {
        e.conversationAnswered = false;
        e.conversationAccepted = false;
    }
}