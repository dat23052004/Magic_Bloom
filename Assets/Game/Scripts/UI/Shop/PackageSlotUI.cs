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
        bool purchased = ShopSaveService.IsIAPPurchased(iapId);

        if (buyButton) buyButton.interactable = !purchased;
        if (purchasedOverlay) purchasedOverlay.SetActive(purchased);
    }

    private void OnBuyClicked()
    {
        // TODO: Gọi Unity IAP flow thật
        // Khi IAP thành công, gọi:
        // CompletePurchase();

        Debug.Log($"[Shop] IAP requested: {iapId}");

#if UNITY_EDITOR
        // Test trong Editor: mua luôn
        CompletePurchase();
#endif
    }

    /// <summary>
    /// Gọi sau khi IAP thành công (từ IAP callback).
    /// </summary>
    public void CompletePurchase()
    {
        ShopService.Ins?.CompleteIAP(iapId, coinReward, undoReward, addTubeReward, shuffleReward, removeAds);
        Refresh();
        AudioManager.Ins?.PlaySFX("Purchase");
    }
}