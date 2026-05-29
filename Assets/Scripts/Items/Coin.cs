using GameEvents;
using TriInspector;
using Unity.VisualScripting;
using UnityEngine;

public class Coin : ICollectable<int>
{
    [InfoBox("Data = Amount of Coins")]
    [SerializeField] private AudioClip coinSound;
    
    // Scriptable Object of Player
    public IntegerSO collectedCoins;
    
    protected override void Collect()
    {
        collectedCoins.Add(data);  // SO update for UI
        
        // invoke event
        GameEventManager.Raise(new OneShotAudioEvent(coinSound, transform.position, 1f));
    
        // destroy this collectible
        Destroy(gameObject);
    }
}
