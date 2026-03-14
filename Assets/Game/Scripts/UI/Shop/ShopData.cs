using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopTab { Packages, TubeCaps, Backgrounds }

public enum UnlockType { Free, LevelRequired, CoinPurchase, AdClaim, IAPPurchase }

public enum IAPCategory { StarterPack, NoAds, NoAdsJustFun, Bundle, CoinPack }

public enum CosmeticStatus
{
    Equipped,        // Đang dùng
    Owned,           // Đã sở hữu (chưa equip)
    Claimable,       // Đủ level, chưa claim
    Purchasable,     // Đủ coin
    NotEnoughCoins,  // Thiếu coin
    AdRequired,      // Xem ads
    Locked           // Chưa đủ level
}