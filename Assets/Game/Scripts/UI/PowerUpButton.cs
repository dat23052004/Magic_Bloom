using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpButton : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    [SerializeField] private Sprite iconNormal;
    [SerializeField] private Sprite iconEmpty;

    public event Action OnClicked;

    private void Awake()
    {
        if (button) button.onClick.AddListener(() => OnClicked?.Invoke());
    }

    private void OnEnable()
    {
        if (InventoryService.Ins != null) InventoryService.Ins.OnItemChanged += HandleItemChanged;
        RefreshUI();
    }


    private void OnDisable()
    {
        if (InventoryService.Ins != null) InventoryService.Ins.OnItemChanged -= HandleItemChanged;
    }
    private void HandleItemChanged(ItemType type, int newCount)
    {
        if(type == itemType) RefreshUI();
    }

    private void RefreshUI()
    {
        int count = InventoryService.Ins != null ? InventoryService.Ins.GetCount(itemType) : 0;
        bool hasItem = count > 0;
        if(iconImage) iconImage.sprite = hasItem ? iconNormal : iconEmpty;
        if (countText)
        {
            countText.gameObject.SetActive(hasItem);
            if(hasItem) countText.text = count.ToString();
        }
    }

    public ItemType GetItemType() => itemType;
}
