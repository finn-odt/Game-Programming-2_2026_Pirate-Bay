using System;
using System.Collections.Generic;
using Configurations;
using GameEvents;
using Unity.VisualScripting;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public InventoryItemDatabaseSO itemDatabase;
    public static Inventory Instance { get; private set; }

    public class InventoryListItem
    {
        public int amount;
        public InventoryItemDataSO saveData;
        public InventoryListItem(InventoryItemDataSO item, int value) {
            amount = value;
            saveData = item;
        }
    }

    public class NotEnoughItemsException : Exception
    {
        public NotEnoughItemsException(string itemName) : base($"Not enough items of '{itemName}'.") { }
    }

    public class InventoryFullException : Exception
    {
        public InventoryFullException() : base($"Inventory capacity reached, no more Items can be collected.") { }
    }

    private List<InventoryListItem> baggedItems;
    private int totalAmountOfItems = 0;

    public int inventoryCapacity = 200;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeInventory()
    {
        baggedItems = new List<InventoryListItem>();

        // read out config file for rebuilding inventory
        List<InventoryEntrySaveData> temp = GameConfiguration.GetInventoryData();
        foreach (InventoryEntrySaveData entry in temp)
        {
            InventoryItemDataSO itemData = itemDatabase.GetItemById(entry.itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"Unknown inventory item ID in save file: {entry.itemId}");
                continue;
            }
            InventoryListItem newItem = new InventoryListItem(itemData, entry.amount);
            
            baggedItems.Add(newItem);
            Debug.Log("Restored Inventory Item from config");
        }
        // raise Inventory Changed Event for UI
        GameEventManager.Raise(new InventoryChangedEvent(baggedItems, itemDatabase));
    }

    public List<InventoryListItem> GetCollectedItems()
    {
        List<InventoryListItem> collectedItems = baggedItems;

        return collectedItems;
    }
    
    void Update()
    {
        
    }

    public void SaveInventory()
    {
        GameConfiguration.SaveInventory(baggedItems);
    }

    public void Clear()
    {
        baggedItems.Clear();
            
        // raise Inventory Changed Event for UI
        GameEventManager.Raise(new InventoryChangedEvent(baggedItems, itemDatabase));
        SaveInventory();
    }

    public void Add(CollectableItem item)
    {
        if (totalAmountOfItems >= inventoryCapacity)
            throw new InventoryFullException();
        
        int count = item.amount;
        InventoryItemDataSO itemData = item.InventoryItemData;

        if (itemData == null || count <= 0)
            return;

        int storedItemIdx = InventoryItemAlreadyStored(itemData);
        if (storedItemIdx != -1)  // already in possession
        {
            baggedItems[storedItemIdx].amount++;  // increment amount in possession
            return;
        }
        
        totalAmountOfItems += count;  // for checking capacity of items
        
        // add new item
        InventoryListItem newItem = new InventoryListItem(itemData, count);
        baggedItems.Add(newItem);
            
        // raise Inventory Changed Event for UI
        GameEventManager.Raise(new InventoryChangedEvent(baggedItems, itemDatabase));
        SaveInventory();
    }

    private int InventoryItemAlreadyStored(InventoryItemDataSO itemData)
    {
        for(int i = 0; i < baggedItems.Count; i++)
        {
            if (baggedItems[i].saveData.ItemId == itemData.ItemId)
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Removes the given amount of the given target
    /// item from the inventory.
    /// </summary>
    /// <exception cref="NotEnoughItemsException">
    /// Thrown when the inventory hosts fewer instances
    /// of targetItem than the amount by that it should
    /// be reduced.
    /// </exception>
    public int Remove(InventoryItemDataSO itemData, int amount)
    {
        int endAmount = 0;
        
        for(int i = baggedItems.Count - 1; i >= 0; i--)
        {
            InventoryListItem item = baggedItems[i];
            if (item.saveData.ItemId == itemData.ItemId)
            {
                endAmount = item.amount - amount;
                if(item.amount < amount)
                    throw new NotEnoughItemsException(item.saveData.ItemName);

                if (item.amount > amount)
                {
                    item.amount -= amount;
                    baggedItems[i] = item;  // reduce ItemAmount by given int[amount]
                }
                else
                {
                    baggedItems.Remove(item);  // remove completely
                }
                break;
            }
        }
            
        // raise Inventory Changed Event for UI
        GameEventManager.Raise(new InventoryChangedEvent(baggedItems, itemDatabase));
        SaveInventory();
        
        return endAmount;
    }
    
    /// <summary>
    /// Removes the given amount of the given target
    /// item from the inventory.
    /// </summary>
    /// <exception cref="NotEnoughItemsException">
    /// Thrown when the inventory hosts fewer instances
    /// of targetItem than the amount by that it should
    /// be reduced.
    /// </exception>
    public int Remove(CollectableItem targetItem, int amount)
    {
        int endAmount = 0;
        
        for(int i = baggedItems.Count - 1; i >= 0; i--)
        {
            InventoryListItem item = baggedItems[i];
            
            if (item.saveData.ItemId == targetItem.InventoryItemData.ItemId)
            {
                endAmount = item.amount - amount;
                if(item.amount < amount)
                    throw new NotEnoughItemsException(item.saveData.ItemName);
                
                if (item.amount > amount)
                {
                    item.amount -= amount;
                    baggedItems[i] = item;  // reduce ItemAmount by given int[amount]
                }
                else
                {
                    baggedItems.Remove(item);  // remove completely
                }
            }
        }
            
        // raise Inventory Changed Event for UI
        GameEventManager.Raise(new InventoryChangedEvent(baggedItems, itemDatabase));
        SaveInventory();
        
        return endAmount;
    }
}
