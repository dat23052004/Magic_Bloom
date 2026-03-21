using System;
using UnityEngine;

public class ShopService : Singleton<ShopService>
{
    private int coins;
    public int Coins => coins;

    // Equipped IDs
    public string EquippedSkin { get; private set; }
    public string EquippedCap { get; private set; }
    public string EquippedBg { get; private set; }

    // Events
    public event Action<int> OnCoinsChanged;
    public event Action<string> OnCapChanged;
    public event Action<string> OnBgChanged;
    public event Action<string> OnItemUnlocked;

    protected override void OnInit()
    {
        coins = ShopSaveService.LoadCoins();
        EquippedCap = ShopSaveService.GetEquippedCap();
        EquippedBg = ShopSaveService.GetEquippedBg();
    }

    public void ReloadCoins()
    {
        coins = ShopSaveService.LoadCoins();
        OnCoinsChanged?.Invoke(coins);
    }

    public void ReloadAll()
    {
        coins = ShopSaveService.LoadCoins();
        EquippedCap = ShopSaveService.GetEquippedCap();
        EquippedBg = ShopSaveService.GetEquippedBg();
        OnCoinsChanged?.Invoke(coins);
        OnCapChanged?.Invoke(EquippedCap);
        OnBgChanged?.Invoke(EquippedBg);
    }

    //  COINS
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        ShopSaveService.SaveCoins(coins);
        OnCoinsChanged?.Invoke(coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount) return false;
        coins -= amount;
        ShopSaveService.SaveCoins(coins);
        OnCoinsChanged?.Invoke(coins);
        return true;
    }

    //  OWNERSHIP
    public bool IsOwned(string id) => ShopSaveService.IsOwned(id);

    /// <summary>
    /// Trả về trạng thái hiện tại của 1 cosmetic slot.
    /// </summary>
    public CosmeticStatus GetStatus(string id, UnlockType unlockType, int unlockLevel, int coinPrice, int currentPlayerLevel)
    {
        if (unlockType == UnlockType.Free || IsOwned(id))
        {
            // Đã sở hữu → check equip
            return IsEquipped(id) ? CosmeticStatus.Equipped : CosmeticStatus.Owned;
        }

        return unlockType switch
        {
            UnlockType.LevelRequired => currentPlayerLevel >= unlockLevel
                ? CosmeticStatus.Claimable
                : CosmeticStatus.Locked,

            UnlockType.CoinPurchase => coins >= coinPrice
                ? CosmeticStatus.Purchasable
                : CosmeticStatus.NotEnoughCoins,

            UnlockType.AdClaim => CosmeticStatus.AdRequired,

            _ => CosmeticStatus.Locked
        };
    }

    /// <summary>
    /// Thử unlock 1 cosmetic. Return true nếu thành công.
    /// </summary>
    public bool TryUnlock(string id, UnlockType unlockType, int unlockLevel, int coinPrice, int currentPlayerLevel)
    {
        if (IsOwned(id)) return false;

        switch (unlockType)
        {
            case UnlockType.Free:
                ShopSaveService.SetOwned(id);
                OnItemUnlocked?.Invoke(id);
                return true;

            case UnlockType.LevelRequired:
                if (currentPlayerLevel >= unlockLevel)
                {
                    ShopSaveService.SetOwned(id);
                    OnItemUnlocked?.Invoke(id);
                    return true;
                }
                return false;

            case UnlockType.CoinPurchase:
                if (SpendCoins(coinPrice))
                {
                    ShopSaveService.SetOwned(id);
                    OnItemUnlocked?.Invoke(id);
                    return true;
                }
                return false;

            case UnlockType.AdClaim:
                // Gọi sau khi xem ads xong
                ShopSaveService.SetOwned(id);
                OnItemUnlocked?.Invoke(id);
                return true;

            default:
                return false;
        }
    }

    //  EQUIP
    public bool IsEquipped(string id)
    {
        return id == EquippedSkin || id == EquippedCap || id == EquippedBg;
    }

    public void EquipCap(string id)
    {
        EquippedCap = id;
        ShopSaveService.SaveEquippedCap(id);
        OnCapChanged?.Invoke(id);
    }

    public void EquipBg(string id)
    {
        EquippedBg = id;
        ShopSaveService.SaveEquippedBg(id);
        OnBgChanged?.Invoke(id);
    }

    //  IAP
    public bool HasNoAds() => ShopSaveService.HasNoAds();

    /// <summary>
    /// Gọi sau khi IAP purchase thành công (callback từ Unity IAP).
    /// </summary>
    public void CompleteIAP(string iapId, int coinReward, int undoReward, int addTubeReward, int shuffleReward, bool removeAds)
    {
        ShopSaveService.SetIAPPurchased(iapId);

        if (coinReward > 0) AddCoins(coinReward);
        if (undoReward > 0) InventoryService.Ins?.AddItem(ItemType.Undo, undoReward);
        if (addTubeReward > 0) InventoryService.Ins?.AddItem(ItemType.AddTube, addTubeReward);
        if (shuffleReward > 0) InventoryService.Ins?.AddItem(ItemType.ShuffleTube, shuffleReward);
        if (removeAds) ShopSaveService.SetNoAds(true);

        Debug.Log($"[Shop] IAP completed: {iapId}");
    }
}