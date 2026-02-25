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

    [SerializeField] private Button noAdsButton;

    [Header("Power-Up Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button addTubeButton;
    [SerializeField] private Button shuffleTubeButton;

    public event Action OnClickSetting;
    public event Action OnClickShop;
    public event Action OnClickNoAds;

    public event Action OnClickUndo;
    public event Action OnClickAddTube;
    public event Action OnClickShuffleTube;

    private Tween fillTween;

    private void Awake()
    {
        HookButtons();
    }

    private void OnEnable()
    {
        // Khi panel bật lên, bạn có thể refresh dữ liệu nếu cần
        // RefreshAll();
    }

    private void HookButtons()
    {
        if (settingButton) settingButton.onClick.AddListener(() => OnClickSetting?.Invoke());
        if (shopButton) shopButton.onClick.AddListener(() => OnClickShop?.Invoke());
        if (noAdsButton) noAdsButton.onClick.AddListener(() => OnClickNoAds?.Invoke());

        if (undoButton) undoButton.onClick.AddListener(() => OnClickUndo?.Invoke());
        if (addTubeButton) addTubeButton.onClick.AddListener(() => OnClickAddTube?.Invoke());
        if (shuffleTubeButton) shuffleTubeButton.onClick.AddListener(() => OnClickShuffleTube?.Invoke());
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

        if (!show) return;

        comboText.text = $"Combo x{combo.ToString()}";

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

    // Cập nhật trạng thái interactable của undo button
    public void SetUndoInteractable(bool interactable)
    {
        if (undoButton) undoButton.interactable = interactable;
    }

 

}
