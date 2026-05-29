using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameEvents
{
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
            Water
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
    

}
