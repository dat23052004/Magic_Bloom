using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WinPanelUI : UIPanel
{
    [Header("Display")]
    [SerializeField] private TMP_Text coinText;   // tổng coin hiện tại
    [SerializeField] private TMP_Text coinRewardText;
    [SerializeField] private TMP_Text coinMultiplierRewardText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image[] starImages; // 3 star icons

    [Header("Multiplier Bar")]
    [SerializeField] private RectTransform multiplierPointer;  // con trỏ trượt qua lại
    [SerializeField] private RectTransform multiplierBar;       // thanh ngang chứa các zone
    [SerializeField] private TMP_Text multiplierText;           // hiển thị "x2", "x3", "x5"

    [Header("Buttons")]
    [SerializeField] private Button claimButton;      // nhận coin gốc
    [SerializeField] private Button watchAdButton;    // nhận coin × multiplier

    [Header("Settings")]
    [SerializeField] private float slideSpeed = 1.2f; // thời gian 1 lượt trượt (giây)

    // Các mức multiplier trên thanh, từ trái → phải
    private readonly int[] multiplierZones = { 2, 3, 5, 3, 2 };

    public event Action OnClaim;
    public event Action<int> OnWatchAd; // truyền multiplier hiện tại

    private int baseCoinReward;
    private Tween slideTween;
    private float normalizedPos; // 0..1 vị trí trên thanh

    private void Awake()
    {
        if (claimButton) claimButton.onClick.AddListener(HandleClaim);
        if (watchAdButton) watchAdButton.onClick.AddListener(HandleWatchAd);
    }

    private Sequence starSeq;

    public void ShowResult(int level, int starRating, int coinReward)
    {
        baseCoinReward = coinReward;

        Show();

        if (levelText) levelText.text = $"Level {level}";
        if (coinText) coinText.text = (ShopService.Ins != null ? ShopService.Ins.Coins : 0).ToString();
        if (coinRewardText) coinRewardText.text = $"+{coinReward}";

        // Kill sequence cũ nếu có
        if (starSeq != null && starSeq.IsActive()) starSeq.Kill();
        starSeq = DOTween.Sequence();

        // Star animation: scale từ 0 → 1 (bounce) + fade 0.2 → 1 + SFX
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                var rt = starImages[i].rectTransform;
                starImages[i].DOKill();
                rt.DOKill();

                starImages[i].color = new Color(1f, 1f, 1f, 0.2f);
                rt.localScale = Vector3.one * 0.3f;

                if (i < starRating)
                {
                    int idx = i;
                    starSeq.AppendCallback(() => AudioManager.Ins?.PlaySFX("Star"));
                    starSeq.Append(starImages[idx].DOFade(1f, 0.35f));
                    starSeq.Join(rt.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
                }
            }
        }

        // Chờ sao hiện xong → bắt đầu trượt multiplier
        starSeq.AppendInterval(0.2f);
        starSeq.AppendCallback(StartSliding);
    }

    public override void Hide()
    {
        StopSliding();
        base.Hide();
    }

    #region Multiplier Slider

    private void StartSliding()
    {
        StopSliding();
        normalizedPos = 0f;
        UpdatePointerPosition();

        // Ping-pong: trượt 0 → 1 → 0 → 1 ... liên tục
        slideTween = DOTween.To(() => normalizedPos, x => normalizedPos = x, 1f, slideSpeed)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(UpdatePointerPosition);
    }

    private void StopSliding()
    {
        if (slideTween != null && slideTween.IsActive())
        {
            slideTween.Kill();
            slideTween = null;
        }
    }

    private void UpdatePointerPosition()
    {
        if (multiplierPointer == null || multiplierBar == null) return;

        float barWidth = multiplierBar.rect.width * 0.922f;
        float leftEdge = -barWidth * 0.53f;

        // Đặt pointer theo normalizedPos (0..1)
        Vector2 pos = multiplierPointer.anchoredPosition;
        pos.x = leftEdge + normalizedPos * barWidth;
        multiplierPointer.anchoredPosition = pos;

        // Cập nhật text multiplier
        int multiplier = GetCurrentMultiplier();
        if (multiplierText)
        {
            multiplierText.text = $"Claim x{multiplier}";
            coinMultiplierRewardText.text = $"{multiplier*baseCoinReward}";
        }
    }

    /// <summary>
    /// Lấy multiplier dựa trên vị trí hiện tại (normalizedPos 0..1).
    /// Chia đều thanh thành 5 zone: x2 | x3 | x5 | x3 | x2
    /// </summary>
    private int GetCurrentMultiplier()
    {
        int zoneCount = multiplierZones.Length;
        int index = Mathf.FloorToInt(normalizedPos * zoneCount);
        index = Mathf.Clamp(index, 0, zoneCount - 1);
        return multiplierZones[index];
    }

    #endregion

    #region Button Handlers

    private void HandleClaim()
    {
        StopSliding();
        OnClaim?.Invoke();
    }

    private void HandleWatchAd()
    {
        StopSliding();
        int multiplier = GetCurrentMultiplier();

        // Cập nhật UI hiển thị coin sau nhân
        int bonusCoin = baseCoinReward * multiplier - baseCoinReward;
        if (coinRewardText) coinRewardText.text = $"+{baseCoinReward} x{multiplier} = {baseCoinReward * multiplier}";

        OnWatchAd?.Invoke(multiplier);
    }

    #endregion
}
