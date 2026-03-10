using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopTab { Packages, TubeCaps, Backgrounds }

public enum UnlockType { Free, LevelRequired, CoinPurchase, AdClaim, IAPPurchase }

public enum IAPCategory { StarterPack, NoAds, NoAdsJustFun, Bundle, CoinPack }

[Serializable]
public class CosmeticItemData
{
    public string id;   // unique key, vd: "cap_crown", "bg_sunset"
    public string displayName;
    public Sprite icon;
    public Sprite preview; // Ảnh preview lớn (dùng cho background / tube skin)

    public UnlockType unlockType;
    public int unLockLevel; // Nếu unlockType == LevelRequired, thì đây là level cần đạt để mở khóa
    public int coinCost; // Nếu unlockType == CoinPurchase, thì đây là số coin cần để mua

    [HideInInspector] public bool isOwned;
}

[Serializable]
public class IAPPackageData
{
    public string id; // IAP product id, vd: "com.game.starterpack"
    public string displayName;
    public Sprite icon;
    public IAPCategory category;

    [Header("Contents")]
    public int coinReward; // Số coin nhan duoc
    public int undoReward; // Số undoItem nhan duoc
    public int addTubeReward; // Số addTubeItem nhan duoc
    public int shuffleReward; // Số shuffleItem nhan duoc
    public bool removesAds; // Có xóa quảng cáo không

    [Header("Pricing")]
    public string priceDisplay; // Giá hiển thị, vd: "$4.99"
    public float priceValue; // Giá trị thực (dùng cho IAP)

    [Header("UI")]
    public bool isHighlighted;       // Nổi bật (Starter Pack)
    public string ribbonText;        // "Best Value", "Popular", etc.
}
