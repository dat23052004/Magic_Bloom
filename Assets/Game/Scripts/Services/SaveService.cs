
using UnityEngine;

public static class SaveService
{
    public static int LoadItemCount(ItemType itemType)
    {
        string key = Constant.ITEM_PREFIX + itemType.ToString();
        if(!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, Constant.DEFAULT_COUNT);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetInt(key, Constant.DEFAULT_COUNT);
    }

    public static void SaveItemCount(ItemType itemType, int count)
    {
        string key = Constant.ITEM_PREFIX + itemType.ToString();
        PlayerPrefs.SetInt(key, Mathf.Max(0, count));
        PlayerPrefs.Save();
    }

    public static void ResetAllItems()
    {
        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            PlayerPrefs.SetInt(Constant.ITEM_PREFIX + itemType.ToString(), Constant.DEFAULT_COUNT);
        }
        PlayerPrefs.Save();
    }

}
