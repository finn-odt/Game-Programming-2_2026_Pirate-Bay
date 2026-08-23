


using System.Collections.Generic;
using System.IO;
using Player;
using UnityEngine;

namespace Configurations
{
    public enum SaveDataState
    {
        NoSaveFile,
        Invalid,
        Default,
        Modified
    }
    
    public static class GameConfiguration
    {
        private static readonly string ConfigPath = Path.Combine(Application.persistentDataPath, "game.config");
        private static readonly string ConfigPathEncrypted = Path.Combine(Application.persistentDataPath, "PirateBay.settings");

        private static bool ConfigIsLoaded = false;

        public static Configuration Data { get; private set; } = new Configuration();

        public static bool ChangeConfiguration(Configuration c)
        {
            // TODO: check new config
            Data = c;
            return true;
        }

        public static void Load()
        {
            if (ConfigIsLoaded)
                return;
            
            Debug.Log(ConfigPath);
            
            try
            {
                string json = SaveEncryption.LoadEncryptedJson(ConfigPathEncrypted);
                Data = JsonUtility.FromJson<Configuration>(json);
            }
            catch
            {
                Debug.LogWarning("Save file could not be loaded. It may be corrupted or modified. New one was created.");
                Data = new Configuration();
            }
            
            ConfigIsLoaded = true;
        }

        public static void Save(bool ignoreLoadedConfig = false)
        {
            if (!ConfigIsLoaded && !ignoreLoadedConfig)
                Load();
            
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(ConfigPath, json);
            SaveEncryption.SaveEncryptedJson(ConfigPathEncrypted, json);
        }

        public static void ResetToFactory()
        {
            Data = new Configuration();
            Save(true);  // save without loading data before (if not already loaded)
        }

        public static List<InventoryEntrySaveData> GetInventoryData()
        {
            if (!ConfigIsLoaded || Data == null)
                Load();

            return Data.inventory;
        }
        
        public static void SaveInventory(List<Inventory.InventoryListItem> items)
        {
            if (!ConfigIsLoaded)
                Load();

            List<InventoryEntrySaveData> saveData = new List<InventoryEntrySaveData>();
            foreach (Inventory.InventoryListItem item in items)
            {
                InventoryItemDataSO itemSO = item.saveData;
                InventoryEntrySaveData existingEntry = new InventoryEntrySaveData(itemSO.ItemId, item.amount);
                saveData.Add(existingEntry);
            }

            Data.inventory = saveData;
            Save();
        }
        
        public static void SaveHandEquipment(string itemIdLeftHand, string itemIdRightHand)
        {
            if (!ConfigIsLoaded)
                Load();

            Data.itemIdForLeftHand = itemIdLeftHand;
            Data.itemIdForRightHand = itemIdRightHand;
            Save();
        }
        
        public static void GetHandEquipmentData(out string itemIdLeftHand, out string itemIdRightHand, out int itemQuantityLeftHand, out int itemQuantityRightHand)
        {
            if (!ConfigIsLoaded)
                Load();

            itemIdLeftHand = Data.itemIdForLeftHand;
            itemIdRightHand = Data.itemIdForRightHand;
            itemQuantityLeftHand = 0;
            itemQuantityRightHand = 0;
            foreach (InventoryEntrySaveData saveData in Data.inventory)
            {
                if(saveData.itemId == itemIdLeftHand)
                    itemQuantityLeftHand = saveData.amount;
                if(saveData.itemId == itemIdRightHand)
                    itemQuantityRightHand = saveData.amount;
            }
        }

        public static void SavePlayer(Vector3 pos)
        {
            Data.playerPos = pos;
        }

        public static void SaveCoinAmount(int coins)
        {
            Data.coinAmount = coins;
        }

        public static void SaveHealthPoints(int health)
        {
            Data.health = health;
        }

        public static void SaveDifficulty(GameDifficulty difficulty)
        {
            Data.gameDifficulty = difficulty;
        }
        
        public static SaveDataState GetSaveDataState()
        {
            if (!File.Exists(ConfigPathEncrypted))
                return SaveDataState.NoSaveFile;

            try
            {
                string json = SaveEncryption.LoadEncryptedJson(ConfigPathEncrypted);
                Configuration savedData = JsonUtility.FromJson<Configuration>(json);

                if (savedData == null)
                    return SaveDataState.Invalid;

                Configuration defaultData = new Configuration();

                string savedJson = JsonUtility.ToJson(savedData);
                string defaultJson = JsonUtility.ToJson(defaultData);

                return savedJson == defaultJson ? SaveDataState.Default : SaveDataState.Modified;
            }
            catch
            {
                return SaveDataState.Invalid;
            }
        }
    }
}