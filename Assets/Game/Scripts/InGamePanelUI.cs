using System;
using System.Collections.Generic;
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

    [Header("Score Burst")]
    [SerializeField] private ScoreBurstStarUI scoreBurstStarPrefab;
    [SerializeField] private RectTransform scoreBurstPoolRoot;
    [SerializeField] private int scoreBurstPrewarmCount = 8;
    [SerializeField] private int minScoreBurstStars = 3;
    [SerializeField] private int maxScoreBurstStars = 6;
    [SerializeField] private Vector2 scoreBurstSpread = new Vector2(120f, 90f);
    [SerializeField] private Vector2 scoreBurstSizeRange = new Vector2(28f, 40f);
    [SerializeField] private float scoreBurstScatterDuration = 0.18f;
    [SerializeField] private float scoreBurstFlyDuration = 0.6f;
    [SerializeField] private float scoreBurstDelayStep = 0.04f;
    [SerializeField] private float scoreBurstArcHeight = 110f;
    [SerializeField] private Color scoreBurstColor = Color.white;
    [SerializeField] private float scoreBurstArriveSfxVolume = 0.35f;

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
    private RectTransform runtimeScoreBurstRoot;
    private readonly Queue<ScoreBurstStarUI> scoreBurstPool = new();
    private readonly List<ScoreBurstStarUI> activeScoreBurstStars = new();
    private readonly Dictionary<ScoreBurstStarUI, int> pendingScoreBurstPayloads = new();

    private void Awake()
    {
        HookButtons();
        EnsureScoreBurstRoot();
        PrewarmScoreBurstPool();
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

        if (ScoreManager.Ins != null)
        {
            UpdateScore(ScoreManager.Ins.TotalStars);
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

        ClearScoreBurstEffects();
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

    public bool PlayTubeCompleteScoreBurst(Vector3 worldPosition, int reward)
    {
        if (reward <= 0 || scoreText == null || scoreBurstStarPrefab == null) return false;
        if (!TryEnsureScoreBurstRoot(out RectTransform rootRect, out Canvas canvas)) return false;

        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            Debug.LogWarning("[InGamePanelUI] Missing main camera for score burst.");
            return false;
        }

        Vector2 startScreenPoint = RectTransformUtility.WorldToScreenPoint(sceneCamera, worldPosition);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, startScreenPoint, uiCamera, out Vector2 startLocalPoint))
            return false;

        Vector3 targetLocalPosition3 = rootRect.InverseTransformPoint(scoreText.rectTransform.position);
        Vector2 targetLocalPoint = new Vector2(targetLocalPosition3.x, targetLocalPosition3.y);
        int starCount = GetScoreBurstStarCount(reward);
        int[] starPayloads = BuildScoreBurstPayloads(reward, starCount);

        return PlayScoreBurst(startLocalPoint, targetLocalPoint, starPayloads, PunchScoreText);
    }

    public bool PlayScoreBurstTest(RectTransform startPoint, RectTransform endPoint, int starCount)
    {
        if (starCount <= 0 || startPoint == null || endPoint == null) return false;
        if (!TryEnsureScoreBurstRoot(out RectTransform rootRect, out _)) return false;
        if (!TryGetLocalPoint(rootRect, startPoint, out Vector2 startLocalPoint)) return false;
        if (!TryGetLocalPoint(rootRect, endPoint, out Vector2 targetLocalPoint)) return false;

        int[] payloads = new int[Mathf.Max(1, starCount)];
        return PlayScoreBurst(startLocalPoint, targetLocalPoint, payloads, null);
    }



    private bool PlayScoreBurst(Vector2 startLocalPoint, Vector2 targetLocalPoint, int[] payloads, Action onArrive)
    {
        if (payloads == null || payloads.Length == 0) return false;

        bool hasPlayedArriveSfx = false;
        for (int i = 0; i < payloads.Length; i++)
        {
            SpawnScoreBurstStar(startLocalPoint, targetLocalPoint, payloads[i], i, () =>
            {
                if (!hasPlayedArriveSfx)
                {
                    hasPlayedArriveSfx = true;
                    PlayScoreBurstArriveSfx();
                }

                onArrive?.Invoke();
            });
        }

        return true;
    }

    private static bool TryGetLocalPoint(RectTransform rootRect, RectTransform pointRect, out Vector2 localPoint)
    {
        if (rootRect == null || pointRect == null)
        {
            localPoint = default;
            return false;
        }

        Vector3 localPosition3 = rootRect.InverseTransformPoint(pointRect.position);
        localPoint = new Vector2(localPosition3.x, localPosition3.y);
        return true;
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

    private bool TryEnsureScoreBurstRoot(out RectTransform rootRect, out Canvas canvas)
    {
        canvas = scoreText != null ? scoreText.canvas : GetComponentInParent<Canvas>();
        rootRect = EnsureScoreBurstRoot();

        if (canvas == null || rootRect == null || scoreBurstStarPrefab == null) return false;

        rootRect.SetAsLastSibling();
        PrewarmScoreBurstPool();
        return true;
    }

    private RectTransform EnsureScoreBurstRoot()
    {
        if (scoreBurstPoolRoot != null) return scoreBurstPoolRoot;
        if (runtimeScoreBurstRoot != null) return runtimeScoreBurstRoot;

        RectTransform hostRect = transform as RectTransform;
        if (hostRect == null) return null;

        GameObject rootObject = new GameObject("ScoreBurstPoolRoot", typeof(RectTransform));
        runtimeScoreBurstRoot = rootObject.GetComponent<RectTransform>();
        runtimeScoreBurstRoot.SetParent(hostRect, false);
        runtimeScoreBurstRoot.anchorMin = Vector2.zero;
        runtimeScoreBurstRoot.anchorMax = Vector2.one;
        runtimeScoreBurstRoot.offsetMin = Vector2.zero;
        runtimeScoreBurstRoot.offsetMax = Vector2.zero;
        runtimeScoreBurstRoot.pivot = new Vector2(0.5f, 0.5f);

        return runtimeScoreBurstRoot;
    }

    private void PrewarmScoreBurstPool()
    {
        if (scoreBurstStarPrefab == null) return;

        RectTransform rootRect = EnsureScoreBurstRoot();
        if (rootRect == null) return;

        int desiredCount = Mathf.Max(1, Mathf.Max(scoreBurstPrewarmCount, maxScoreBurstStars));
        int totalCount = scoreBurstPool.Count + activeScoreBurstStars.Count;

        while (totalCount < desiredCount)
        {
            scoreBurstPool.Enqueue(CreatePooledScoreBurstStar(rootRect));
            totalCount++;
        }
    }

    private ScoreBurstStarUI CreatePooledScoreBurstStar(RectTransform rootRect)
    {
        ScoreBurstStarUI star = Instantiate(scoreBurstStarPrefab, rootRect);
        star.gameObject.name = $"{scoreBurstStarPrefab.name}_Pooled";
        star.ResetForPool();
        return star;
    }

    private ScoreBurstStarUI RentScoreBurstStar()
    {
        RectTransform rootRect = EnsureScoreBurstRoot();
        if (rootRect == null || scoreBurstStarPrefab == null) return null;

        ScoreBurstStarUI star = null;
        while (scoreBurstPool.Count > 0 && star == null)
        {
            star = scoreBurstPool.Dequeue();
        }

        if (star == null)
        {
            star = CreatePooledScoreBurstStar(rootRect);
        }

        activeScoreBurstStars.Add(star);
        return star;
    }

    private void ReturnScoreBurstStar(ScoreBurstStarUI star)
    {
        if (star == null) return;
        if (!activeScoreBurstStars.Remove(star)) return;

        star.ResetForPool();
        star.RectTransform.SetParent(EnsureScoreBurstRoot(), false);
        scoreBurstPool.Enqueue(star);
    }

    private int GetScoreBurstStarCount(int reward)
    {
        int desiredCount = Mathf.Clamp(Mathf.CeilToInt(reward / 3f), minScoreBurstStars, maxScoreBurstStars);
        return Mathf.Clamp(desiredCount, 1, Mathf.Max(1, reward));
    }

    private int[] BuildScoreBurstPayloads(int reward, int starCount)
    {
        int safeCount = Mathf.Clamp(starCount, 1, Mathf.Max(1, reward));
        int[] payloads = new int[safeCount];

        for (int i = 0; i < safeCount; i++)
        {
            payloads[i] = 1;
        }

        int remaining = reward - safeCount;
        while (remaining > 0)
        {
            int index = UnityEngine.Random.Range(0, safeCount);
            payloads[index]++;
            remaining--;
        }

        return payloads;
    }

    private void SpawnScoreBurstStar(Vector2 startLocalPoint, Vector2 targetLocalPoint, int payload, int index, Action onArrive)
    {
        ScoreBurstStarUI star = RentScoreBurstStar();
        if (star == null)
        {
            if (payload > 0)
            {
                ScoreManager.Ins?.AddStars(payload);
            }

            onArrive?.Invoke();
            return;
        }

        if (payload > 0)
        {
            pendingScoreBurstPayloads[star] = payload;
        }

        float starSize = UnityEngine.Random.Range(scoreBurstSizeRange.x, scoreBurstSizeRange.y);
        star.Prepare(EnsureScoreBurstRoot(), startLocalPoint, starSize, scoreBurstColor);

        Vector2 burstOffset = new Vector2(
            UnityEngine.Random.Range(-scoreBurstSpread.x, scoreBurstSpread.x),
            UnityEngine.Random.Range(scoreBurstSpread.y * 0.35f, scoreBurstSpread.y));
        Vector2 scatterPoint = startLocalPoint + burstOffset;
        Vector2 controlPoint = Vector2.Lerp(scatterPoint, targetLocalPoint, 0.5f)
            + Vector2.up * UnityEngine.Random.Range(scoreBurstArcHeight * 0.75f, scoreBurstArcHeight * 1.25f);

        float scatterDuration = scoreBurstScatterDuration * UnityEngine.Random.Range(0.9f, 1.15f);
        float flyDuration = scoreBurstFlyDuration * UnityEngine.Random.Range(0.9f, 1.1f);
        float startDelay = index * scoreBurstDelayStep;

        star.PlayBurst(scatterPoint, controlPoint, targetLocalPoint, startDelay, scatterDuration, flyDuration, () =>
        {
            if (pendingScoreBurstPayloads.TryGetValue(star, out int claimedPayload))
            {
                pendingScoreBurstPayloads.Remove(star);
                ScoreManager.Ins?.AddStars(claimedPayload);
            }

            onArrive?.Invoke();
            ReturnScoreBurstStar(star);
        });
    }

    private void PlayScoreBurstArriveSfx()
    {
        AudioManager.Ins?.PlaySFX(SfxCue.Star, scoreBurstArriveSfxVolume);
    }

    private void PunchScoreText()
    {
        if (scoreText == null) return;

        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.DOKill();
        scoreRect.localScale = Vector3.one;
        scoreRect.DOPunchScale(Vector3.one * 0.16f, 0.22f, 8, 0.8f);
    }

    private void ClearScoreBurstEffects()
    {
        int pendingReward = 0;
        foreach (int payload in pendingScoreBurstPayloads.Values)
        {
            pendingReward += payload;
        }

        pendingScoreBurstPayloads.Clear();

        for (int i = activeScoreBurstStars.Count - 1; i >= 0; i--)
        {
            ScoreBurstStarUI star = activeScoreBurstStars[i];
            if (star == null)
            {
                activeScoreBurstStars.RemoveAt(i);
                continue;
            }

            ReturnScoreBurstStar(star);
        }

        if (pendingReward > 0)
        {
            ScoreManager.Ins?.AddStars(pendingReward);
        }
    }
}
