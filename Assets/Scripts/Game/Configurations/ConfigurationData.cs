using System;
using System.Collections.Generic;
using UnityEngine;

namespace Configurations
{
    [Serializable]
    public class InventoryEntrySaveData
    {
        public string itemId;
        public int amount;
        public InventoryEntrySaveData(string id, int count) { itemId = id; amount = count; }
    }
    
    public class Configuration
    {
        // Changeable
        public float mouseSensitivityX = 1.8f;
        public float mouseSensitivityY = 0.85f;
        public float gamepadSensitivityX = 0.5f;
        public float gamepadSensitivityY = 0.3f;
        
        public bool fullscreen = true;
        
        public GameDifficulty gameDifficulty = GameDifficulty.Easy;
        
        public Vector3 playerPos = new Vector3(42.65f, 1.42932f, 100.41f);

        public int coinAmount = 0;
        public int health = 100;

        public string itemIdForLeftHand = "";
        public string itemIdForRightHand = "";
        public List<InventoryEntrySaveData> inventory = new List<InventoryEntrySaveData>();
    }
}