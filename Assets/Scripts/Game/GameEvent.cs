using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameEvents
{
    [Serializable]
    public abstract class GameEvent
    {
        public virtual bool isValid() { return true; }
    }

    public class PlayerReachCanonEvent : GameEvent
    {
        public readonly Transform player;  
        public readonly Transform canon;

        public PlayerReachCanonEvent(Transform p, Transform c) {
            player = p;
            canon = c;
        }
        
        public override bool isValid ()
        {
            return player != null && canon != null;
        }
    }

    public class CollectedCoinEvent : GameEvent
    {
        public readonly int amount;

        public CollectedCoinEvent(int a) {
           amount = a;
        }
        
        public override bool isValid ()
        {
            return amount > 0;
        }
    }

    public class SpentCoinsEvent : GameEvent
    {
        public readonly int amount;

        public SpentCoinsEvent(int a) {
            amount = a;
        }
        
        public override bool isValid ()
        {
            return amount > 0;
        }
    }

    public class OneShotAudioEvent : GameEvent
    {
        public readonly AudioClip clip;
        public readonly Vector3 position;
        public readonly float volume;

        public OneShotAudioEvent(AudioClip sound, Vector3 pos, float v) {
           clip = sound;
           position = pos;
           volume = v;
        }
        
        public override bool isValid ()
        {
            return (clip != null && volume > 0);
        }
    }

    public class PlayerInteractionRequestEvent : GameEvent
    {

        public PlayerInteractionRequestEvent() {}
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class InteractionPossibleEvent : GameEvent
    {
        public readonly bool interactionPossible;
        public readonly GameObject interactable;

        public InteractionPossibleEvent(bool p, GameObject o)
        {
            interactionPossible = p;
            interactable = o;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class GameDifficultyChangedEvent : GameEvent
    {
        public readonly GameDifficulty newDifficulty;

        public GameDifficultyChangedEvent(GameDifficulty difficulty)
        {
            newDifficulty = difficulty;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class SensitivityChangeEvent : GameEvent
    {
        public readonly float newSensitivityX;
        public readonly float newSensitivityY;
        public readonly bool mouseOrGamepad;

        public SensitivityChangeEvent(bool mouseOrGamepad, float sensitivityX, float sensitivityY)
        {
            newSensitivityX = sensitivityX;
            newSensitivityY = sensitivityY;
            this.mouseOrGamepad = mouseOrGamepad;
        }
        
        public override bool isValid ()
        {
            return newSensitivityX > 0;
        }
    }

    public class GameOverEvent : GameEvent
    {
        public enum Killer
        {
            Npc,
            Zombie,
            Shark,
            Drowned
        }
        
        public readonly Vector3 deathPosition;
        public readonly Killer killer;

        public GameOverEvent(Vector3 pos, Killer killedBy)
        {
            deathPosition = pos;
            killer = killedBy;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class ReachedGoalEvent : GameEvent
    {
        public ReachedGoalEvent() {}
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class GameStateChangedEvent : GameEvent
    {
        public enum GameState
        {
            Intro,
            Play,
            Paused,
            Won,
            Lost,
            Inventory
        }

        public readonly GameState newState;

        public GameStateChangedEvent(GameState newState)
        {
            this.newState = newState;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class InventoryChangedEvent : GameEvent
    {
        public readonly List<Inventory.InventoryListItem> items;
        public readonly InventoryItemDatabaseSO itemDatabase;

        public InventoryChangedEvent(List<Inventory.InventoryListItem> items, InventoryItemDatabaseSO itemDatabase)
        {
            this.items = items;
            this.itemDatabase = itemDatabase;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class InventoryDropRequestedEvent : GameEvent
    {
        public readonly string itemId;

        public InventoryDropRequestedEvent(string itemId)
        {
            this.itemId = itemId;
        }
        
        public override bool isValid ()
        {
            return !String.IsNullOrEmpty(itemId);
        }
    }

    public class InventoryVisibilityChangeEvent : GameEvent
    {
        public readonly bool isOpen;

        public InventoryVisibilityChangeEvent(bool isOpen)
        {
            this.isOpen = isOpen;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class CloseInventoryEvent : GameEvent
    {
        public CloseInventoryEvent() {}
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class OpenInventoryEvent : GameEvent
    {
        public OpenInventoryEvent() {}
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class DestroyItemEvent : GameEvent
    {
        public readonly GameObject item;

        public DestroyItemEvent(GameObject item)
        {
            this.item = item;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class ConversationUIEvent : GameEvent
    {
        public readonly string message;
        public readonly bool isOpen;

        public ConversationUIEvent(string message, bool isOpen)
        {
            this.message = message;
            this.isOpen = isOpen;
        }
        
        public override bool isValid ()
        {
            return message.Length > 0;
        }
    }

    public class ConversationAnswerEvent : GameEvent
    {
        public readonly bool isAccepted;
        public ConversationAnswerEvent(bool isAccepted)
        {
            this.isAccepted = isAccepted;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }

    public class PlayerDamageEvent : GameEvent
    {
        public enum DamagedBy
        {
            NPC,
            Shark,
            Water,
            Explosion
        }
        
        public readonly DamagedBy damagedBy;
        public readonly int healthPoints;
        public PlayerDamageEvent(int healthPoints, DamagedBy damagedBy)
        {
            this.healthPoints = healthPoints;
            this.damagedBy = damagedBy;
        }
        
        public override bool isValid ()
        {
            return healthPoints > 0;
        }
    }
    public class PlayerHealEvent : GameEvent
    {
        public readonly int healthPoints;
        public PlayerHealEvent(int healthPoints)
        {
            this.healthPoints = healthPoints;
        }
        
        public override bool isValid ()
        {
            return healthPoints > 0;
        }
    }
    
    public class PlayerBerserkEvent : GameEvent
    {
        public readonly float factor, duration;
        public PlayerBerserkEvent(float factor, float duration)
        {
            this.factor = factor;
            this.duration = duration;
        }
        
        public override bool isValid ()
        {
            return factor > 1f;
        }
    }
    
    public class PlayerSwordAttackEvent : GameEvent
    {
        public PlayerSwordAttackEvent()
        {
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class PlayerEquipItemEvent : GameEvent
    {
        public readonly GameObject item;
        public readonly bool leftHand;
        
        public PlayerEquipItemEvent(GameObject item, bool leftHand)
        {
            this.item = item;
            this.leftHand = leftHand;
        }
        
        public override bool isValid ()
        {
            return item != null;
        }
    }
    
    public class PlayerStripItemEvent : GameEvent
    {
        public readonly bool leftHand;
        public readonly bool doNotDestroy;
        
        public PlayerStripItemEvent(bool leftHand, bool doNotDestroy=false)
        {
            this.leftHand = leftHand;
            this.doNotDestroy = doNotDestroy;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class PlayerUseHandRequestEvent : GameEvent
    {
        public readonly bool leftHand;
        
        public PlayerUseHandRequestEvent(bool leftHand)
        {
            this.leftHand = leftHand;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class RemoveItemFromHandForUseEvent : GameEvent
    {
        public readonly InventoryItemDataSO itemData;
        
        public RemoveItemFromHandForUseEvent(InventoryItemDataSO itemData)
        {
            this.itemData = itemData;
        }
        
        public override bool isValid ()
        {
            return itemData != null;
        }
    }
    
    public class PlayerSwimEvent : GameEvent
    {
        public readonly bool isInWater;
        
        public PlayerSwimEvent(bool isInWater)
        {
            this.isInWater = isInWater;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class BroadcastMeanWaterSurfaceEvent : GameEvent
    {
        public readonly float waterSurfaceY;
        
        public BroadcastMeanWaterSurfaceEvent(float waterSurfaceY)
        {
            this.waterSurfaceY = waterSurfaceY;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UpdateHealthUIEvent : GameEvent
    {
        public readonly int health;
        
        public UpdateHealthUIEvent(int health)
        {
            this.health = health;
        }
        
        public override bool isValid ()
        {
            return health >= 0 && health <= 1f;
        }
    }
    
    public class CameraUnderWaterEvent : GameEvent
    {
        public readonly bool isUnderWater;
        
        public CameraUnderWaterEvent(bool isUnderWater)
        {
            this.isUnderWater = isUnderWater;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class ToggleInventoryEvent : GameEvent
    {
        public ToggleInventoryEvent() {}
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class BroadcastInputControlSchemeEvent : GameEvent
    {
        public readonly string controlScheme;

        public BroadcastInputControlSchemeEvent(string controlScheme)
        {
            this.controlScheme = controlScheme;
        }
        
        public override bool isValid ()
        {
            return controlScheme != null && controlScheme.Length > 0;
        }
    }
    
    public class ConnectionModeChangedEvent : GameEvent
    {
        public readonly DeviceConnectionMode newMode;

        public ConnectionModeChangedEvent(DeviceConnectionMode newMode)
        {
            this.newMode = newMode;
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UIPickupIndicatorEvent : GameEvent
    {

        public UIPickupIndicatorEvent()
        {
            Debug.Log("EVENT - UIPickupIndicatorEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UIInteractIndicatorEvent : GameEvent
    {

        public UIInteractIndicatorEvent()
        {
            Debug.Log("EVENT - UIInteractIndicatorEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UCCAbilityStatusEvent : GameEvent
    {
        public UCCAbilityStatusEvent()
        {}
        
        public override bool isValid ()
        {
            return true;
        }
    }
    public class UCCAbilityPossibleEvent : GameEvent
    {

        public UCCAbilityPossibleEvent()
        {}
        
        public override bool isValid ()
        {
            return true;
        }
    }
    public class UCCAbilityImpossibleEvent : GameEvent
    {

        public UCCAbilityImpossibleEvent()
        {}
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UCCDivePossibleEvent : UCCAbilityPossibleEvent
    {

        public UCCDivePossibleEvent()
        {
            Debug.Log("EVENT - UCCDivePossibleEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    public class UCCDiveImpossibleEvent : UCCAbilityImpossibleEvent
    {

        public UCCDiveImpossibleEvent()
        {
            Debug.Log("EVENT - UCCDiveImpossibleEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UCCLadderClimbPossibleEvent : UCCAbilityPossibleEvent
    {

        public UCCLadderClimbPossibleEvent()
        {
            Debug.Log("EVENT - UCCLadderClimbPossibleEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    public class UCCLadderClimbImpossibleEvent : UCCAbilityImpossibleEvent
    {

        public UCCLadderClimbImpossibleEvent()
        {
            Debug.Log("EVENT - UCCLadderClimbImpossibleEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    public class UCCClimbFromWaterPossibleEvent : UCCAbilityPossibleEvent
    {

        public UCCClimbFromWaterPossibleEvent()
        {
            Debug.Log("EVENT - UCCClimbFromWaterPossibleEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    public class UCCClimbFromWaterImpossibleEvent : UCCAbilityImpossibleEvent
    {

        public UCCClimbFromWaterImpossibleEvent()
        {
            Debug.Log("EVENT - UCCClimbFromWaterImpossibleEvent");
        }
        
        public override bool isValid ()
        {
            return true;
        }
    }
    
    

}
