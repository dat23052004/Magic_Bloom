
using UnityEngine;

public static class SaveService
{
    public static int LoadItemCount(ItemType itemType)
    {
        string key = Constant.ITEM_PREFIX + itemType.ToString();
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, Constant.DEFAULT_ITEM_COUNT);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetInt(key, Constant.DEFAULT_ITEM_COUNT);
    }

    public static void SaveItemCount(ItemType itemType, int count)
    {
        string key = Constant.ITEM_PREFIX + itemType.ToString();
        PlayerPrefs.SetInt(key, Mathf.Max(0, count));
        PlayerPrefs.Save();
    }

    public static int AddItemCount(ItemType itemType, int amount)
    {
        if (amount <= 0) return LoadItemCount(itemType);

        int newCount = LoadItemCount(itemType) + amount;
        SaveItemCount(itemType, newCount);
        return newCount;
    }

    public static void ResetAllItems()
    {
        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            PlayerPrefs.SetInt(Constant.ITEM_PREFIX + itemType.ToString(), Constant.DEFAULT_ITEM_COUNT);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Reset tất cả: coin về 100, mỗi item về 5.
    /// </summary>
    public static void ResetAll()
    {
        // Xóa sạch tất cả PlayerPrefs (coins, items, cosmetics, IAP, equipped, noAds...)
        PlayerPrefs.DeleteAll();

        // Set lại default
        PlayerPrefs.SetInt(Constant.COINS_KEY, Constant.DEFAULT_COINS);
        PlayerPrefs.SetInt(Constant.LEVEL_KEY, GameManager.Ins.startLevel);
        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            PlayerPrefs.SetInt(Constant.ITEM_PREFIX + itemType.ToString(), Constant.DEFAULT_ITEM_COUNT);
        }

        PlayerPrefs.Save();
    }
}
