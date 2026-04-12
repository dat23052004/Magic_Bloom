using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab TubeCap: grid slots bên dưới, preview + action area bên trên.
/// Action area có 2 group: Equipped (đang dùng) và Unequipped (4 button).
/// </summary>
public class SkinCapTabUI : MonoBehaviour
{
    [Header("Preview Area")]
    [SerializeField] private Image previewCapImage;
    [Header("Action - Equipped Group")]
    [SerializeField] private GameObject equippedGroup;     // Hiện khi cap đang equip
    [Header("Action - Unequipped Group")]
    [SerializeField] private GameObject unequippedGroup;   // Hiện khi cap chưa equip
    [SerializeField] private Button btnEquip;              // Đã sở hữu → bấm để equip
    [SerializeField] private Button btnSpendCoins;         // Mua bằng coin
    [SerializeField] private TMP_Text spendCoinsText;      // Hiện giá coin
    [SerializeField] private Button btnClaimAd;            // Xem ad để nhận
    [SerializeField] private Button btnLocked;             // Chưa đủ level
    [SerializeField] private Button btnClaim;             // Đã mở khóa đủ level
    [SerializeField] private TMP_Text lockedText;          // Hiện "Level X"

    [Header("Slots")]
    [SerializeField] private CosmeticSlotUI[] capSlots;

    private CosmeticSlotUI selectedSlot;

    private void OnEnable()
    {
        foreach (var slot in capSlots)
        {
            if (slot == null) continue;
            slot.OnSlotSelected += HandleSlotSelected;
            slot.RefreshIndicators();
        }

        // Button listeners
        if (btnEquip) btnEquip.onClick.AddListener(EquipSelected);
        if (btnSpendCoins) btnSpendCoins.onClick.AddListener(BuyWithCoins);
        if (btnClaimAd) btnClaimAd.onClick.AddListener(RequestAdUnlock);
        if (btnLocked) btnLocked.onClick.AddListener(GoToIngame);
        if (btnClaim) btnClaim.onClick.AddListener(ClaimLevelUnlock);

        if (ShopService.Ins != null)
            ShopService.Ins.OnCapChanged += HandleCapChanged;

        SelectDefault();
    }

    private void OnDisable()
    {
        foreach (var slot in capSlots)
        {
            if (slot != null)
                slot.OnSlotSelected -= HandleSlotSelected;
        }

        if (btnEquip) btnEquip.onClick.RemoveListener(EquipSelected);
        if (btnSpendCoins) btnSpendCoins.onClick.RemoveListener(BuyWithCoins);
        if (btnClaimAd) btnClaimAd.onClick.RemoveListener(RequestAdUnlock);
        if (btnLocked) btnLocked.onClick.RemoveListener(GoToIngame);
        if (btnClaim) btnClaim.onClick.RemoveListener(ClaimLevelUnlock);

        if (ShopService.Ins != null)
            ShopService.Ins.OnCapChanged -= HandleCapChanged;
    }

    private void SelectDefault()
    {
        CosmeticSlotUI defaultSlot = null;

        if (ShopService.Ins != null)
        {
            string equippedId = ShopService.Ins.EquippedCap;
            foreach (var slot in capSlots)
            {
                if (slot != null && slot.ItemId == equippedId)
                {
                    defaultSlot = slot;
                    break;
                }
            }
        }

        if (defaultSlot == null && capSlots.Length > 0)
            defaultSlot = capSlots[0];

        if (defaultSlot != null)
            HandleSlotSelected(defaultSlot);
    }

    private void HandleSlotSelected(CosmeticSlotUI slot)
    {
        selectedSlot = slot;

        foreach (var s in capSlots)
        {
            if (s != null) s.SetSelected(s == slot);
        }
        UpdatePreviewArea();
    }

    private void HandleCapChanged(string capId)
    {
        foreach (var slot in capSlots)
        {
            if (slot != null) slot.RefreshIndicators();
        }
        UpdatePreviewArea();
    }

    private void UpdatePreviewArea()
    {
        if (selectedSlot == null) return;

        if (previewCapImage && selectedSlot.ItemSprite)
            previewCapImage.sprite = selectedSlot.ItemSprite;

        CosmeticStatus status = selectedSlot.GetStatus();
        bool isEquipped = status == CosmeticStatus.Equipped;

        // Toggle 2 groups
        if (equippedGroup) equippedGroup.SetActive(isEquipped);
        if (unequippedGroup) unequippedGroup.SetActive(!isEquipped);

        if (isEquipped) return;

        // Ẩn tất cả button trước
        HideAllUnequippedButtons();

        // Hiện đúng button theo status
        switch (status)
        {
            case CosmeticStatus.Owned:
                ShowButton(btnEquip);
                break;

            case CosmeticStatus.Purchasable:
                ShowButton(btnSpendCoins);
                if (spendCoinsText) spendCoinsText.text = selectedSlot.CoinPrice.ToString();
                break;

            case CosmeticStatus.NotEnoughCoins:
                ShowButton(btnSpendCoins);
                if (spendCoinsText) spendCoinsText.text = selectedSlot.CoinPrice.ToString();
                break;

            case CosmeticStatus.Claimable:
                ShowButton(btnClaim);
                break;

            case CosmeticStatus.AdRequired:
                ShowButton(btnClaimAd);
                break;

            case CosmeticStatus.Locked:
                ShowButton(btnLocked);
                if (lockedText) lockedText.text = $"Level {selectedSlot.RequiredLevel}";
                break;
        }
    }

    private void HideAllUnequippedButtons()
    {
        if (btnEquip) btnEquip.gameObject.SetActive(false);
        if (btnSpendCoins) btnSpendCoins.gameObject.SetActive(false);
        if (btnClaimAd) btnClaimAd.gameObject.SetActive(false);
        if (btnLocked) btnLocked.gameObject.SetActive(false);
        if (btnClaim) btnClaim.gameObject.SetActive(false);
    }

    private void ShowButton(Button btn)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(true);
    }

    // ── Actions ──

    private void EquipSelected()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        ShopService.Ins.EquipCap(selectedSlot.ItemId);
        AudioManager.Ins?.PlaySFX(SfxCue.Equip);
        RefreshAll();
    }

    private void BuyWithCoins()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        int playerLevel = PlayerPrefs.GetInt(Constant.LEVEL_KEY);

        if (ShopService.Ins.TryUnlock(selectedSlot.ItemId, selectedSlot.SlotUnlockType,
                selectedSlot.RequiredLevel, selectedSlot.CoinPrice, playerLevel))
        {
            ShopService.Ins.EquipCap(selectedSlot.ItemId);
            AudioManager.Ins?.PlaySFX(SfxCue.Purchase);
            RefreshAll();
        }
    }

    private void RequestAdUnlock()
    {
        if (selectedSlot == null) return;
        Debug.Log($"[Shop] Request ad for cap: {selectedSlot.ItemId}");
#if UNITY_EDITOR
        CompleteAdUnlock();
#endif
    }

    public void CompleteAdUnlock()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        ShopService.Ins.TryUnlock(selectedSlot.ItemId, UnlockType.AdClaim, 0, 0, 0);
        ShopService.Ins.EquipCap(selectedSlot.ItemId);
        AudioManager.Ins?.PlaySFX(SfxCue.Purchase);
        RefreshAll();
    }

    private void ClaimLevelUnlock()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        int playerLevel = PlayerPrefs.GetInt(Constant.LEVEL_KEY);

        if (ShopService.Ins.TryUnlock(selectedSlot.ItemId, selectedSlot.SlotUnlockType,
                selectedSlot.RequiredLevel, selectedSlot.CoinPrice, playerLevel))
        {
            AudioManager.Ins?.PlaySFX(SfxCue.Purchase);
            RefreshAll();
        }
    }

    private void GoToIngame()
    {
        // Đóng shop, chuyển về InGame để cày level
        if (GameManager.Ins != null)
            GameManager.Ins.SwitchState(GameState.InGame);
    }

    private void RefreshAll()
    {
        foreach (var slot in capSlots)
        {
            if (slot != null) slot.RefreshIndicators();
        }
        UpdatePreviewArea();
    }
}
