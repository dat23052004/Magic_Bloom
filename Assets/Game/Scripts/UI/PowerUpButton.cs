using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PowerUpButton : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private RectTransform bounceTarget;

    [SerializeField] private Sprite iconNormal;
    [SerializeField] private Sprite iconEmpty;

    [Header("Press Animation")]
    [SerializeField] private float pressOffsetY = 24f;
    [SerializeField] private float pressUpDuration = 0.12f;
    [SerializeField] private float pressDownDuration = 0.18f;
    [SerializeField] private Ease pressUpEase = Ease.OutQuad;
    [SerializeField] private Ease pressDownEase = Ease.OutBounce;

    [Header("Selected State")]
    [SerializeField] private float selectedOffsetY = 24f;
    [SerializeField] private float selectedDuration = 0.12f;
    [SerializeField] private Ease selectedEase = Ease.OutQuad;

    public event Action OnClicked;

    private RectTransform animatedRect;
    private Vector2 cachedAnchoredPosition;
    private Sequence pressSequence;
    private Tween selectedTween;
    private bool isSelected;

    private void Awake()
    {
        animatedRect = bounceTarget != null ? bounceTarget : transform as RectTransform;
        CacheAnimatedPosition();

        if (button) button.onClick.AddListener(() => OnClicked?.Invoke());
    }

    private void OnEnable()
    {
        if (InventoryService.Ins != null) InventoryService.Ins.OnItemChanged += HandleItemChanged;
        CacheAnimatedPosition();
        SnapToRestPosition();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (InventoryService.Ins != null) InventoryService.Ins.OnItemChanged -= HandleItemChanged;
        SnapToRestPosition();
        KillTweens();
    }

    private void HandleItemChanged(ItemType type, int newCount)
    {
        if (type != itemType) return;

        RefreshUI();
    }

    private void RefreshUI()
    {
        int count = InventoryService.Ins != null ? InventoryService.Ins.GetCount(itemType) : 0;
        bool hasItem = count > 0;
        if(iconImage) iconImage.sprite = hasItem ? iconNormal : iconEmpty;
        if (countText)
        {
            countText.gameObject.SetActive(hasItem);
            if (hasItem) countText.text = count.ToString();
        }
    }

    private void CacheAnimatedPosition()
    {
        if (animatedRect == null) return;

        cachedAnchoredPosition = isSelected
            ? animatedRect.anchoredPosition - Vector2.up * selectedOffsetY
            : animatedRect.anchoredPosition;
    }

    private void KillTweens()
    {
        pressSequence?.Kill();
        pressSequence = null;

        selectedTween?.Kill();
        selectedTween = null;
    }

    private void SnapToRestPosition()
    {
        if (animatedRect == null) return;
        animatedRect.anchoredPosition = GetRestPosition();
    }

    private Vector2 GetRestPosition()
    {
        return cachedAnchoredPosition + (isSelected ? Vector2.up * selectedOffsetY : Vector2.zero);
    }

    public void SetInteractable(bool interactable)
    {
        if (button) button.interactable = interactable;
    }

    public void SetSelected(bool selected, bool animate = true)
    {
        if (isSelected == selected)
        {
            if (!animate) SnapToRestPosition();
            return;
        }

        isSelected = selected;

        if (animatedRect == null) return;

        pressSequence?.Kill();
        pressSequence = null;

        selectedTween?.Kill();

        if (!animate)
        {
            selectedTween = null;
            SnapToRestPosition();
            return;
        }

        selectedTween = animatedRect.DOAnchorPos(GetRestPosition(), selectedDuration)
            .SetEase(selectedEase)
            .OnComplete(() => selectedTween = null)
            .OnKill(() =>
            {
                if (animatedRect != null)
                {
                    animatedRect.anchoredPosition = GetRestPosition();
                }

                selectedTween = null;
            });
    }

    public void PlayUseAnimation()
    {
        if (animatedRect == null) return;

        selectedTween?.Kill();
        selectedTween = null;

        pressSequence?.Kill();
        animatedRect.anchoredPosition = GetRestPosition();

        Vector2 bounceUpPosition = GetRestPosition() + Vector2.up * pressOffsetY;

        pressSequence = DOTween.Sequence()
            .Append(animatedRect.DOAnchorPos(bounceUpPosition, pressUpDuration).SetEase(pressUpEase))
            .Append(animatedRect.DOAnchorPos(GetRestPosition(), pressDownDuration).SetEase(pressDownEase))
            .OnComplete(() => pressSequence = null)
            .OnKill(() =>
            {
                if (animatedRect != null)
                {
                    animatedRect.anchoredPosition = GetRestPosition();
                }

                pressSequence = null;
            });
    }

    public ItemType GetItemType() => itemType;
}
