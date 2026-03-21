using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slot trong grid — chỉ hiển thị icon + trạng thái nhỏ.
/// Click = chọn slot, logic mua/equip nằm ở Tab.
/// </summary>
public class CosmeticSlotUI : MonoBehaviour
{
    public enum SlotCategory { TubeCap, Background }

    [Header("Config")]
    [SerializeField] private string itemId;
    [SerializeField] private SlotCategory category;
    [SerializeField] private UnlockType unlockType;
    [SerializeField] private int coinPrice;
    [SerializeField] private int requiredLevel;

    [Header("Visuals")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite itemSprite;

    [Header("Slot Indicators")]
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject lockIcon;        // ổ khóa khi chưa mở
    [SerializeField] private GameObject equippedMark;    // GameObject con có icon checkmark
    [SerializeField] private GameObject required;    // Các phần chỉ hiện thị khi chưa sở hữu
    [SerializeField] private Outline selectedOutline;     // Outline component trên button

    // Public getters cho Tab đọc config
    public string ItemId => itemId;
    public SlotCategory Category => category;
    public UnlockType SlotUnlockType => unlockType;
    public int CoinPrice => coinPrice;
    public int RequiredLevel => requiredLevel;
    public Sprite ItemSprite => itemSprite;

    public event System.Action<CosmeticSlotUI> OnSlotSelected;

    private void Awake()
    {
        if (selectButton) selectButton.onClick.AddListener(() => OnSlotSelected?.Invoke(this));
    }

    private void OnEnable()
    {
        RefreshIndicators();
    }

    /// <summary>
    /// Cập nhật indicators trên slot (equipped mark, lock icon).
    /// Không xử lý logic mua/equip.
    /// </summary>
    public void RefreshIndicators()
    {
        if (iconImage && itemSprite) iconImage.sprite = itemSprite;

        CosmeticStatus status = GetStatus();

        bool equipped = status == CosmeticStatus.Equipped;
        bool locked = status == CosmeticStatus.Locked || status == CosmeticStatus.NotEnoughCoins;

        bool owned = status == CosmeticStatus.Equipped || status == CosmeticStatus.Owned;

        if (equippedMark) equippedMark.SetActive(equipped);
        if (lockIcon) lockIcon.SetActive(locked);
        if (required) required.SetActive(!owned);
    }

    public void SetSelected(bool selected)
    {
        if (selectedOutline) selectedOutline.enabled = selected;
    }

    public CosmeticStatus GetStatus()
    {
        if (ShopService.Ins == null) return CosmeticStatus.Locked;
        int playerLevel = PlayerPrefs.GetInt(Constant.LEVEL_KEY);
        return ShopService.Ins.GetStatus(itemId, unlockType, requiredLevel, coinPrice, playerLevel);
    }
}
