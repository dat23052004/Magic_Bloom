using UnityEngine;

public static class ShopSaveService 
{
    // ── Coins ──
    public static int LoadCoins()
    {
        if (!PlayerPrefs.HasKey(Constant.COINS_KEY))
        {
            PlayerPrefs.SetInt(Constant.COINS_KEY, Constant.DEFAULT_COINS);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetInt(Constant.COINS_KEY, Constant.DEFAULT_COINS);
    }

    public static void SaveCoins(int amount)
    {
        PlayerPrefs.SetInt(Constant.COINS_KEY, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    // ── Cosmetic Ownership ──
    public static bool IsOwned(string itemId) => PlayerPrefs.GetInt(Constant.OWNED_PREFIX + itemId, 0) == 1;
    public static void SetOwned(string itemId)
    {
        PlayerPrefs.SetInt(Constant.OWNED_PREFIX + itemId, 1);
        PlayerPrefs.Save();
    }

    // ── Equipped ──
    public static string GetEquippedCap() => PlayerPrefs.GetString(Constant.EQUIP_CAP, "");
    public static string GetEquippedBg() => PlayerPrefs.GetString(Constant.EQUIP_BG, "");

    public static void SaveEquippedCap(string id) { PlayerPrefs.SetString(Constant.EQUIP_CAP, id); PlayerPrefs.Save(); }
    public static void SaveEquippedBg(string id) { PlayerPrefs.SetString(Constant.EQUIP_BG, id); PlayerPrefs.Save(); }

    // ── No Ads ──
    public static bool HasNoAds() => PlayerPrefs.GetInt(Constant.NO_ADS_KEY, 0) == 1;
    public static void SetNoAds(bool value) { PlayerPrefs.SetInt(Constant.NO_ADS_KEY, value ? 1 : 0); PlayerPrefs.Save(); }

    // ── IAP purchased ──
    public static bool IsIAPPurchased(string iapId) => PlayerPrefs.GetInt(Constant.IAP_PREFIX + iapId, 0) == 1;
    public static void SetIAPPurchased(string iapId) { PlayerPrefs.SetInt(Constant.IAP_PREFIX + iapId, 1); PlayerPrefs.Save(); }
}
