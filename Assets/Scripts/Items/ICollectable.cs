using GameEvents;
using UnityConstantsGenerator;
using UnityEngine;

public abstract class ICollectable<T> : MonoBehaviour
{
    [SerializeField] protected T data;
    
    protected bool playerInTrigger = false;
    protected bool isGamePaused = false;
    
    protected void OnEnable()
    {
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
    }

    protected void OnDisable()
    {
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
    }

    protected abstract void Collect();


    private void OnGameStateChange(GameStateChangedEvent e)
    {
        isGamePaused = e.newState == GameStateChangedEvent.GameState.Paused;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (isGamePaused)
            return;
        
        if(other.gameObject.layer == (int)LayerId.Player)
            Collect();
    }
}
