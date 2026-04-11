using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public ShopPanelUI shopPanel;
    public SettingPanelUI settingPanel;
    public WinPanelUI winPanel;
    public LosePanelUI losePanel;
    public InGamePanelUI inGamePanel;
    private ComboTracker comboTrack;
    private void Start()
    {
        comboTrack = ComboTracker.Ins;           // lấy 1 lần
        if (comboTrack != null) comboTrack.OnComboChanged += HandleComboChanged;// top
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
        if (state == GameState.Shop || state == GameState.Win || state == GameState.Lose)
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

            case GameState.Lose:
                losePanel.Show();
                break;
        }
    }

    private void HandleWin()
    {
        int level = LevelManager.Ins != null ? LevelManager.Ins.CurrentLevel : 1;

        // Tính star rating từ ScoreManager
        int totalColorTubes = 0;
        if (LevelManager.Ins != null)
            totalColorTubes = LevelManager.Ins.CurrentModels.FindAll(t => !t.isEmpty).Count;

        int starRating = ScoreManager.Ins != null
            ? ScoreManager.Ins.GetStarRating(totalColorTubes)
            : 1;

        // Tính coin reward (chưa cộng — chờ player bấm Claim hoặc Watch Ad)
        int coinReward = starRating switch
        {
            3 => Constant.COIN_REWARD_3_STAR ,
            2 => Constant.COIN_REWARD_2_STAR ,
            _ => Constant.COIN_REWARD_1_STAR 
        };

        // Lưu lại để cộng khi player bấm Claim / Watch Ad
        pendingCoinReward = coinReward;

        // Hiển thị WinPanel với kết quả
        winPanel.ShowResult(level, starRating, coinReward);
    }

    private void HideAll()
    {
        shopPanel.Hide();
        inGamePanel.Hide();
        settingPanel.Hide();
        winPanel.Hide();
        losePanel.Hide();
    }

    public void UpdateLevel(int level)
    {
        inGamePanel.SetLevel(level);
    }
    private void HandleComboChanged(int combo)
    {
        if (inGamePanel == null || comboTrack == null) return;
        float resetTime = comboTrack.comboResetTime;
        inGamePanel.SetCombo(combo, resetTime);

        if (combo >= 5)
        {
            AudioManager.Ins?.PlaySFX("ComboHigh");
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
        if (GameManager.Ins.currentState != GameState.InGame) return; // Chỉ xử lý khi đang trong game
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
        // TODO: Mở shop / rewarded ads
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
        // Nhận coin gốc
        ShopService.Ins?.AddCoins(pendingCoinReward);
        GoNextLevel();
    }

    private void HandleWinWatchAd(int multiplier)
    {
        // Nhận coin × multiplier (sau khi xem ads xong)
        int total = pendingCoinReward * multiplier;
        ShopService.Ins?.AddCoins(total);
        GoNextLevel();
    }

    private void GoNextLevel()
    {
        OnGameStateChanged(GameState.InGame);
        LevelManager.Ins?.LoadNextLevel();
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
