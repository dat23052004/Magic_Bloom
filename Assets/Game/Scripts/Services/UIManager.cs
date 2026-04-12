using DG.Tweening;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public ShopPanelUI shopPanel;
    public SettingPanelUI settingPanel;
    public WinPanelUI winPanel;
    public InGamePanelUI inGamePanel;

    [Header("Audio")]
    [SerializeField] private float winSfxDelay = 0.15f;
    [SerializeField] private float winSfxVolume = 0.2f;

    private ComboTracker comboTrack;
    private Tween pendingWinSfxTween;

    private void Start()
    {
        comboTrack = ComboTracker.Ins;           // lấy 1 lần
        if (comboTrack != null) comboTrack.OnComboChanged += HandleComboChanged; // top
        SubscribePowerUpEvents(); // bottom

        // Subscribe setting events
        if (inGamePanel != null)
        {
            inGamePanel.OnClickSetting += OpenSettingOverlay;
            inGamePanel.OnClickShop += OpenShop;
        }

        if (settingPanel != null)
        {
            settingPanel.OnClose += CloseSettingOverlay;
            settingPanel.OnReplay += HandleReplay;
        }

        if (shopPanel != null) shopPanel.OnCloseShop += CloseShop;

        if (winPanel != null)
        {
            winPanel.OnClaim += HandleWinClaim;
            winPanel.OnWatchAd += HandleWinWatchAd;
        }
    }

    protected override void OnInit()
    {
        HideAll();
    }

    protected override void OnDestroy()
    {
        KillPendingWinSfx();

        if (comboTrack != null) comboTrack.OnComboChanged -= HandleComboChanged;
        UnsubscribePowerUpEvents();

        if (inGamePanel != null)
        {
            inGamePanel.OnClickSetting -= OpenSettingOverlay;
            inGamePanel.OnClickShop -= OpenShop;
        }

        if (settingPanel != null)
        {
            settingPanel.OnClose -= CloseSettingOverlay;
            settingPanel.OnReplay -= HandleReplay;
        }

        if (shopPanel != null) shopPanel.OnCloseShop -= CloseShop;

        if (winPanel != null)
        {
            winPanel.OnClaim -= HandleWinClaim;
            winPanel.OnWatchAd -= HandleWinWatchAd;
        }

        base.OnDestroy();
    }

    public void OnGameStateChanged(GameState state)
    {
        HideAll();
        if (state == GameState.Shop || state == GameState.Win)
        {
            ComboTracker.Ins?.ResetCombo();
        }

        switch (state)
        {
            case GameState.Shop:
                shopPanel.Show();
                break;

            case GameState.InGame:
                inGamePanel.Show();
                break;

            case GameState.Win:
                HandleWin();
                break;
        }
    }

    private void HandleWin()
    {
        int level = LevelManager.Ins != null ? LevelManager.Ins.CurrentLevel : 1;

        int totalColorTubes = 0;
        if (LevelManager.Ins != null)
            totalColorTubes = LevelManager.Ins.CurrentModels.FindAll(t => !t.isEmpty).Count;

        int starRating = ScoreManager.Ins != null
            ? ScoreManager.Ins.GetStarRating(totalColorTubes)
            : 1;

        int coinReward = starRating switch
        {
            3 => Constant.COIN_REWARD_3_STAR,
            2 => Constant.COIN_REWARD_2_STAR,
            _ => Constant.COIN_REWARD_1_STAR
        };

        pendingCoinReward = coinReward;

        PlayWinSfxDelayed();
        winPanel.ShowResult(level, starRating, coinReward);
    }

    private void HideAll()
    {
        KillPendingWinSfx();
        shopPanel.Hide();
        inGamePanel.Hide();
        settingPanel.Hide();
        winPanel.Hide();
    }

    public void UpdateLevel(int level)
    {
        inGamePanel.SetLevel(level);
    }

    public bool TryPlayTubeCompleteScoreBurst(Vector3 worldPosition, int reward)
    {
        return inGamePanel != null
            && inGamePanel.isActiveAndEnabled
            && inGamePanel.PlayTubeCompleteScoreBurst(worldPosition, reward);
    }

    private void HandleComboChanged(int combo)
    {
        if (inGamePanel == null || comboTrack == null) return;

        float resetTime = comboTrack.comboResetTime;
        inGamePanel.SetCombo(combo, resetTime);

        if (combo >= 5)
        {
            AudioManager.Ins?.PlaySFX(SfxCue.ComboHigh);
        }
    }

    #region Event Bottom UI - PowerUp Buttons
    private void SubscribePowerUpEvents()
    {
        var panel = inGamePanel;
        if (panel == null) return;

        panel.OnPowerUpUse += HandlePowerUpUse;
        panel.OnPowerUpEmpty += HandlePowerUpEmpty;
    }

    private void UnsubscribePowerUpEvents()
    {
        var panel = inGamePanel;
        if (panel == null) return;

        panel.OnPowerUpUse -= HandlePowerUpUse;
        panel.OnPowerUpEmpty -= HandlePowerUpEmpty;
    }

    private void HandlePowerUpUse(ItemType itemType)
    {
        if (GameManager.Ins.currentState != GameState.InGame) return;

        var inventory = InventoryService.Ins;
        var levelManager = LevelManager.Ins;

        switch (itemType)
        {
            case ItemType.Undo:
                if (UndoManager.Ins == null || !UndoManager.Ins.HasHistory) return;
                if (inventory == null || !inventory.UseItem(itemType)) return;
                if (!(levelManager?.PerformUndo() ?? false))
                {
                    inventory.AddItem(itemType, 1);
                }
                break;

            case ItemType.AddTube:
                if (levelManager == null || !levelManager.CanAddExtraTube()) return;
                if (inventory == null || !inventory.UseItem(itemType)) return;
                if (!levelManager.AddExtraTube())
                {
                    inventory.AddItem(itemType, 1);
                }
                break;

            case ItemType.ShuffleTube:
                if (levelManager == null) return;
                if (!levelManager.IsShuffleSelectMode && (inventory == null || !inventory.HasItem(itemType))) return;
                levelManager.ToggleShuffleSelectMode();
                break;
        }
    }

    private void HandlePowerUpEmpty(ItemType type)
    {
        Debug.Log($"[PowerUp] {type} hết! Mở shop/ads...");
    }
    #endregion

    #region Setting Overlay
    private void OpenSettingOverlay()
    {
        inGamePanel.SetSettingButtonVisible(false);
        settingPanel.Show();
    }

    private void CloseSettingOverlay()
    {
        settingPanel.Hide();
        inGamePanel.SetSettingButtonVisible(true);
    }

    private void HandleReplay()
    {
        CloseSettingOverlay();
        LevelManager.Ins?.LoadLevel(LevelManager.Ins.CurrentLevel);
    }
    #endregion

    #region Win
    private int pendingCoinReward;

    private void HandleWinClaim()
    {
        ShopService.Ins?.AddCoins(pendingCoinReward);
        GoNextLevel();
    }

    private void HandleWinWatchAd(int multiplier)
    {
        int total = pendingCoinReward * multiplier;
        ShopService.Ins?.AddCoins(total);
        GoNextLevel();
    }

    private void GoNextLevel()
    {
        OnGameStateChanged(GameState.InGame);
        LevelManager.Ins?.LoadNextLevel();
    }

    private void PlayWinSfxDelayed()
    {
        KillPendingWinSfx();

        if (winSfxDelay <= 0f)
        {
            AudioManager.Ins?.PlaySFX(SfxCue.Win,winSfxVolume);
            return;
        }

        pendingWinSfxTween = DOVirtual.DelayedCall(winSfxDelay, () =>
        {
            pendingWinSfxTween = null;
            AudioManager.Ins?.PlaySFX(SfxCue.Win,winSfxVolume);
        });
    }

    private void KillPendingWinSfx()
    {
        if (pendingWinSfxTween == null || !pendingWinSfxTween.IsActive()) return;

        pendingWinSfxTween.Kill();
        pendingWinSfxTween = null;
    }
    #endregion

    #region Shop
    private void OpenShop()
    {
        OnGameStateChanged(GameState.Shop);
    }

    private void CloseShop()
    {
        OnGameStateChanged(GameState.InGame);
    }
    #endregion
}
