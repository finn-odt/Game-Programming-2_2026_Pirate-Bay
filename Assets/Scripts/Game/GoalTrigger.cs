using GameEvents;
using SLTypes;
using UnityConstantsGenerator;
using UnityEngine;
using UnityServiceLocator;

public class GoalTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != (int)LayerId.Player)
            return;
        
        GameEventManager.Raise(new PlayerReachedGoalEvent());
    }
}