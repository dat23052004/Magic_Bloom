using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi package item fix cứng trong scene.
/// Cấu hình rewards trong Inspector.
/// </summary>
public class PackageSlotUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string iapId;            // "com.game.starterpack"
    [SerializeField] private int coinReward;
    [SerializeField] private int undoReward;
    [SerializeField] private int addTubeReward;
    [SerializeField] private int shuffleReward;
    [SerializeField] private bool removeAds;

    [Header("UI References")]
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject purchasedOverlay;  // Hiện khi đã mua

    private void OnEnable()
    {
        Refresh();
    }

    private void Awake()
    {
        if (buyButton) buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void Refresh()
    {
        bool purchased = IsOneTimePurchase() && ShopSaveService.IsIAPPurchased(iapId);

        if (purchasedOverlay != null)
        {
            purchasedOverlay.SetActive(purchased);
        }

        if (buyButton)
        {
            buyButton.interactable = !purchased;
        }
    }

    private void OnBuyClicked()
    {
        if (IsOneTimePurchase() && ShopSaveService.IsIAPPurchased(iapId))
        {
            Refresh();
            return;
        }

        // TODO: Gọi Unity IAP flow thật
        // Khi IAP thành công, gọi:
        // CompletePurchase();

        Debug.Log($"[Shop] IAP requested: {iapId}");

            CompletePurchase();
    }

    /// <summary>
    /// Gọi sau khi IAP thành công (từ IAP callback).
    /// </summary>
    public void CompletePurchase()
    {
        ShopService shop = ShopService.Ins;
        if (shop == null) return;

        bool granted = shop.CompleteIAP(
            iapId,
            coinReward,
            undoReward,
            addTubeReward,
            shuffleReward,
            removeAds,
            IsOneTimePurchase());

        if (!granted)
        {
            Refresh();
            return;
        }

        Refresh();
        AudioManager.Ins?.PlaySFX(SfxCue.Purchase);
    }

    private bool IsOneTimePurchase()
    {
        return purchasedOverlay != null;
    }
}
