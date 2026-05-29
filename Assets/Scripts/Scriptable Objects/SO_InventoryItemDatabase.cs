using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Database")]
public class InventoryItemDatabaseSO : ScriptableObject
{
    [SerializeField] private List<InventoryItemDataSO> items;
    
    private Dictionary<string, InventoryItemDataSO> itemLookup;  // for faster access
    
    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        itemLookup = new Dictionary<string, InventoryItemDataSO>();

        if (items == null)
            return;

        foreach (InventoryItemDataSO item in items)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.ItemId))
            {
                Debug.LogWarning($"Inventory item '{item.name}' has no ItemId.", item);
                continue;
            }

            if (itemLookup.ContainsKey(item.ItemId))
            {
                Debug.LogWarning(
                    $"Duplicate inventory item id '{item.ItemId}' found. Item '{item.name}' will be ignored.",
                    item
                );
                continue;
            }

            itemLookup.Add(item.ItemId, item);
        }
    }

    public InventoryItemDataSO GetItemById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (itemLookup == null)
            BuildLookup();

        return itemLookup.TryGetValue(itemId, out InventoryItemDataSO item) ? item : null;
    }
    
    public InventoryItemDataSO GetRandomItem()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogError($"{nameof(InventoryItemDatabaseSO)} has no items.");
            return null;
        }

        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            int randomIndex = Random.Range(0, items.Count);
            InventoryItemDataSO item = items[randomIndex];

            if (item != null)
                return item;
        }

        return null;
    }
}