using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab Background: grid slots bên dưới, preview + action button bên trên.
/// </summary>
public class BackgroundTabUI : MonoBehaviour
{
    [Header("Preview Area")]
    [SerializeField] private Image previewBgImage;
    [SerializeField] private TMP_Text statusText;

    [Header("Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private GameObject actionCoinIcon;
    [SerializeField] private GameObject actionAdIcon;

    [Header("Slots")]
    [SerializeField] private CosmeticSlotUI[] bgSlots;

    private CosmeticSlotUI selectedSlot;

    private void OnEnable()
    {
        foreach (var slot in bgSlots)
        {
            if (slot == null) continue;
            slot.OnSlotSelected += HandleSlotSelected;
            slot.RefreshIndicators();
        }

        if (ShopService.Ins != null)
            ShopService.Ins.OnBgChanged += HandleBgChanged;

        SelectDefault();
    }

    private void OnDisable()
    {
        foreach (var slot in bgSlots)
        {
            if (slot != null)
                slot.OnSlotSelected -= HandleSlotSelected;
        }

        if (ShopService.Ins != null)
            ShopService.Ins.OnBgChanged -= HandleBgChanged;
    }

    private void SelectDefault()
    {
        CosmeticSlotUI defaultSlot = null;

        if (ShopService.Ins != null)
        {
            string equippedId = ShopService.Ins.EquippedBg;
            foreach (var slot in bgSlots)
            {
                if (slot != null && slot.ItemId == equippedId)
                {
                    defaultSlot = slot;
                    break;
                }
            }
        }

        if (defaultSlot == null && bgSlots.Length > 0)
            defaultSlot = bgSlots[0];

        if (defaultSlot != null)
            HandleSlotSelected(defaultSlot);
    }

    private void HandleSlotSelected(CosmeticSlotUI slot)
    {
        selectedSlot = slot;

        foreach (var s in bgSlots)
        {
            if (s != null) s.SetSelected(s == slot);
        }

        UpdatePreviewArea();
    }

    private void HandleBgChanged(string bgId)
    {
        foreach (var slot in bgSlots)
        {
            if (slot != null) slot.RefreshIndicators();
        }

        UpdatePreviewArea();
    }

    private void UpdatePreviewArea()
    {
        if (selectedSlot == null) return;

        if (previewBgImage && selectedSlot.ItemSprite)
            previewBgImage.sprite = selectedSlot.ItemSprite;

        CosmeticStatus status = selectedSlot.GetStatus();

        if (actionCoinIcon) actionCoinIcon.SetActive(false);
        if (actionAdIcon) actionAdIcon.SetActive(false);

        actionButton.onClick.RemoveAllListeners();

        switch (status)
        {
            case CosmeticStatus.Equipped:
                if (statusText) statusText.text = "Using";
                if (actionButtonText) actionButtonText.text = "Equipped";
                SetActionButton(false);
                break;

            case CosmeticStatus.Owned:
                if (statusText) statusText.text = "Owned";
                if (actionButtonText) actionButtonText.text = "Equip";
                SetActionButton(true, () => EquipSelected());
                break;

            case CosmeticStatus.Claimable:
                if (statusText) statusText.text = "Not owned";
                if (actionButtonText) actionButtonText.text = "Claim";
                SetActionButton(true, () => UnlockAndEquip());
                break;

            case CosmeticStatus.Purchasable:
                if (statusText) statusText.text = "Not owned";
                if (actionCoinIcon) actionCoinIcon.SetActive(true);
                if (actionButtonText) actionButtonText.text = selectedSlot.CoinPrice.ToString();
                SetActionButton(true, () => UnlockAndEquip());
                break;

            case CosmeticStatus.NotEnoughCoins:
                if (statusText) statusText.text = "Not owned";
                if (actionCoinIcon) actionCoinIcon.SetActive(true);
                if (actionButtonText) actionButtonText.text = selectedSlot.CoinPrice.ToString();
                SetActionButton(false);
                break;

            case CosmeticStatus.AdRequired:
                if (statusText) statusText.text = "Not owned";
                if (actionAdIcon) actionAdIcon.SetActive(true);
                if (actionButtonText) actionButtonText.text = "Claim";
                SetActionButton(true, () => RequestAdUnlock());
                break;

            case CosmeticStatus.Locked:
                if (statusText) statusText.text = $"Level {selectedSlot.RequiredLevel}";
                if (actionButtonText) actionButtonText.text = $"Lv.{selectedSlot.RequiredLevel}";
                SetActionButton(false);
                break;
        }
    }

    private void SetActionButton(bool interactable, System.Action onClick = null)
    {
        if (actionButton == null) return;
        actionButton.interactable = interactable;
        if (onClick != null)
            actionButton.onClick.AddListener(() => onClick());
    }

    private void EquipSelected()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        ShopService.Ins.EquipBg(selectedSlot.ItemId);
        AudioManager.Ins?.PlaySFX(SfxCue.Equip);
        RefreshAll();
    }

    private void UnlockAndEquip()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        int playerLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

        if (ShopService.Ins.TryUnlock(selectedSlot.ItemId, selectedSlot.SlotUnlockType,
                selectedSlot.RequiredLevel, selectedSlot.CoinPrice, playerLevel))
        {
            ShopService.Ins.EquipBg(selectedSlot.ItemId);
            AudioManager.Ins?.PlaySFX(SfxCue.Purchase);
            RefreshAll();
        }
    }

    private void RequestAdUnlock()
    {
        if (selectedSlot == null) return;
        Debug.Log($"[Shop] Request ad for bg: {selectedSlot.ItemId}");
#if UNITY_EDITOR
        CompleteAdUnlock();
#endif
    }

    public void CompleteAdUnlock()
    {
        if (selectedSlot == null || ShopService.Ins == null) return;
        ShopService.Ins.TryUnlock(selectedSlot.ItemId, UnlockType.AdClaim, 0, 0, 0);
        ShopService.Ins.EquipBg(selectedSlot.ItemId);
        AudioManager.Ins?.PlaySFX(SfxCue.Purchase);
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (var slot in bgSlots)
        {
            if (slot != null) slot.RefreshIndicators();
        }
        UpdatePreviewArea();
    }
}
