using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Undo, AddTube, ShuffleTube}
public class InventoryService : Singleton<InventoryService>
{
    private Dictionary<ItemType, int> items = new Dictionary<ItemType, int>();

    public event Action<ItemType, int> OnItemChanged;

    protected override void OnInit()
    {
        foreach(ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            items[type] = SaveService.LoadItemCount(type);
        }
    }

    public int GetCount(ItemType itemType)
    {
        return items.TryGetValue(itemType, out int count) ? count : 0;
    }

    public bool UseItem(ItemType itemType)
    {
        if (GetCount(itemType) <= 0) return false;

        items[itemType]--;
        SaveService.SaveItemCount(itemType, items[itemType]);
        OnItemChanged?.Invoke(itemType, items[itemType]);
        return true;
    }

    public void AddItem(ItemType type, int amount = 1)
    {
        if(amount <= 0) return;

        int newCount = SaveService.AddItemCount(type, amount);
        items[type] = newCount;
        OnItemChanged?.Invoke(type, newCount);
    }
    public bool HasItem(ItemType type) => GetCount(type) > 0;

    public void ReloadFromSave()
    {
        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            items[type] = SaveService.LoadItemCount(type);
            OnItemChanged?.Invoke(type, items[type]);
        }
    }
}
