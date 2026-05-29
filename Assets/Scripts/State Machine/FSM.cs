using System.Collections.Generic;
using GameEvents;
using UnityEngine;

public class FSM<T>
{

    private T Owner;
    public IFSMState<T> CurrentState { get; private set; }
    
    
    //private Stack<IFSMState<T>> _previousStates = new();
    private IFSMState<T> _previousState;
    public IFSMState<T> PreviousState => _previousState;
    /*{
        get
        {
            if (_previousStates.Count == 0)
                //return null;
            return _previousStates.Pop();
        }
    }*/

    public void Configure(T owner, IFSMState<T> initialState)
    {
        Owner = owner;  // who am I attached to
        ChangeState(initialState);
    }

    public void Update()
    {
        if(CurrentState != null)  // makes current state sense?
        {
            CurrentState.Reason(Owner);
            CurrentState.Update(Owner);
        }
    }
    
    public void ChangeState(IFSMState<T> newState)
    {
        if(CurrentState != null)
            CurrentState.Exit(Owner);

        _previousState = CurrentState;  // push last state to stack (not used)
        CurrentState = newState;

        if(_previousState != null && CurrentState != null)
            Debug.Log($"{_previousState.GetType().ToString()} -> {CurrentState.GetType().ToString()}");

        if(CurrentState != null)
            CurrentState.Enter(Owner);
    }
}
