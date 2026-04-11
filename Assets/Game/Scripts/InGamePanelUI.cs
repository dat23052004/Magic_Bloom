using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InGamePanelUI : UIPanel
{
    [Header("Top")]
    [SerializeField] private Button settingButton;
    [SerializeField] private Button shopButton;

    [SerializeField] private TMP_Text levelText;

    [SerializeField] private GameObject comboRoot;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Image comboFillImage;

    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private Button noAdsButton;

    [Header("Power-Up Buttons")]
    [SerializeField] private PowerUpButton undoButton;
    [SerializeField] private PowerUpButton addTubeButton;
    [SerializeField] private PowerUpButton shuffleTubeButton;

    public event Action<ItemType> OnPowerUpUse; // khi con item
    public event Action<ItemType> OnPowerUpEmpty; // hki het item mo shop

    public event Action OnClickSetting;
    public event Action OnClickShop;
    public event Action OnClickNoAds;

    private Tween fillTween;

    private void Awake()
    {
        HookButtons();
    }

    private void OnEnable()
    {
        if (ScoreManager.Ins != null) ScoreManager.Ins.OnScoreChanged += UpdateScore;
        if (InventoryService.Ins != null) InventoryService.Ins.OnItemChanged += HandleInventoryChanged;
        if (UndoManager.Ins != null) UndoManager.Ins.OnHistoryChanged += HandleUndoHistoryChanged;
        if (LevelManager.Ins != null)
        {
            LevelManager.Ins.OnShuffleSelectModeChanged += HandleShuffleSelectModeChanged;
            LevelManager.Ins.OnExtraTubeStateChanged += HandleExtraTubeStateChanged;
        }

        RefreshPowerUpButtons();
    }

    private void OnDisable()
    {
        if (ScoreManager.Ins != null) ScoreManager.Ins.OnScoreChanged -= UpdateScore;
        if (InventoryService.Ins != null) InventoryService.Ins.OnItemChanged -= HandleInventoryChanged;
        if (UndoManager.Ins != null) UndoManager.Ins.OnHistoryChanged -= HandleUndoHistoryChanged;
        if (LevelManager.Ins != null)
        {
            LevelManager.Ins.OnShuffleSelectModeChanged -= HandleShuffleSelectModeChanged;
            LevelManager.Ins.OnExtraTubeStateChanged -= HandleExtraTubeStateChanged;
        }
    }

    private void HookButtons()
    {
        if (settingButton) settingButton.onClick.AddListener(() => OnClickSetting?.Invoke());
        if (shopButton) shopButton.onClick.AddListener(() => OnClickShop?.Invoke());
        if (noAdsButton) noAdsButton.onClick.AddListener(() => OnClickNoAds?.Invoke());

        if (undoButton) undoButton.OnClicked += () => HandlePowerUpClick(ItemType.Undo);
        if (addTubeButton) addTubeButton.OnClicked += () => HandlePowerUpClick(ItemType.AddTube);
        if (shuffleTubeButton) shuffleTubeButton.OnClicked += () => HandlePowerUpClick(ItemType.ShuffleTube);
    }
    private void HandlePowerUpClick(ItemType type)
    {
        bool hasItem = InventoryService.Ins != null && InventoryService.Ins.HasItem(type);

        switch (type)
        {
            case ItemType.Undo:
                if (!hasItem)
                {
                    OnPowerUpEmpty?.Invoke(type);
                    return;
                }

                if (!CanUseUndo()) return;
                undoButton?.PlayUseAnimation();
                OnPowerUpUse?.Invoke(type);
                break;

            case ItemType.AddTube:
                if (!hasItem)
                {
                    OnPowerUpEmpty?.Invoke(type);
                    return;
                }

                if (!CanUseAddTube()) return;
                addTubeButton?.PlayUseAnimation();
                OnPowerUpUse?.Invoke(type);
                break;

            case ItemType.ShuffleTube:
                if (!IsShuffleActive() && !hasItem)
                {
                    OnPowerUpEmpty?.Invoke(type);
                    return;
                }

                OnPowerUpUse?.Invoke(type);
                break;
        }
    }
    public void SetLevel(int level)
    {
        if (levelText) levelText.text = $"Level {level.ToString()}";
    }

    public void SetCombo(int combo, float resetTime)
    {
        if (!comboRoot || !comboText) return;

        bool show = combo > 0;
        comboRoot.SetActive(show);

        if (!show)
        {
            KillFillTween();
            return;
        }

        comboText.text = $"Combo x{combo}";

        comboRoot.transform.DOKill();
        comboRoot.transform.localScale = Vector3.one;

        comboRoot.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1);

        AnimateFillDecrease(resetTime);
    }
    private void AnimateFillDecrease(float duration)
    {
        if (comboFillImage == null) return;

        KillFillTween();
        comboFillImage.fillAmount = 1f;

        // Tween từ 1 → 0
        fillTween = comboFillImage.DOFillAmount(0f, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                fillTween = null;
            });
    }
    private void KillFillTween()
    {
        if (fillTween != null && fillTween.IsActive())
        {
            fillTween.Kill();
            fillTween = null;
        }
    }
    public void SetNoAds(bool visible)
    {
        // to do - change to payment 
    }

    public void SetSettingButtonVisible(bool visible)
    {
        if (settingButton) settingButton.gameObject.SetActive(visible);
    }

    private void UpdateScore(int stars)
    {
        if (scoreText) scoreText.text = stars.ToString();
    }

    private void HandleInventoryChanged(ItemType type, int newCount)
    {
        RefreshPowerUpButtons();
    }

    private void HandleUndoHistoryChanged(bool hasHistory)
    {
        RefreshPowerUpButtons();
    }

    private void HandleShuffleSelectModeChanged(bool isActive)
    {
        RefreshPowerUpButtons();
    }

    private void HandleExtraTubeStateChanged()
    {
        RefreshPowerUpButtons();
    }

    private void RefreshPowerUpButtons()
    {
        bool hasUndoItem = InventoryService.Ins != null && InventoryService.Ins.HasItem(ItemType.Undo);
        bool hasAddTubeItem = InventoryService.Ins != null && InventoryService.Ins.HasItem(ItemType.AddTube);
        bool hasShuffleItem = InventoryService.Ins != null && InventoryService.Ins.HasItem(ItemType.ShuffleTube);
        bool shuffleActive = IsShuffleActive();

        if (undoButton) undoButton.SetInteractable(hasUndoItem && CanUseUndo());
        if (addTubeButton) addTubeButton.SetInteractable(hasAddTubeItem && CanUseAddTube());
        if (shuffleTubeButton)
        {
            shuffleTubeButton.SetInteractable(shuffleActive || hasShuffleItem);
            shuffleTubeButton.SetSelected(shuffleActive);
        }
    }

    private bool CanUseUndo()
    {
        return UndoManager.Ins != null && UndoManager.Ins.HasHistory;
    }

    private bool CanUseAddTube()
    {
        return LevelManager.Ins != null && LevelManager.Ins.CanAddExtraTube();
    }

    private bool IsShuffleActive()
    {
        return LevelManager.Ins != null && LevelManager.Ins.IsShuffleSelectMode;
    }
}
