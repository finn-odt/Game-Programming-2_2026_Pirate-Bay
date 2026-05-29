using System;
using GameEvents;
using UnityEngine;

public class StatefulMonoBehaviour<T> : MonoBehaviour
{

    protected FSM<T> fsm;
    
    public void ChangeState(IFSMState<T> e)
    {
        fsm.ChangeState(e);
    }
    
    public void BackToPreviousState()
    {
        fsm.ChangeState(fsm.PreviousState);
    }
    
    protected virtual void Update()
    {
        fsm.Update();
        Updated();  // for calling Update in child classes
    }
    
    protected virtual void Updated() {}
}
