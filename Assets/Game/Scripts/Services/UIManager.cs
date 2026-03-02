using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public MenuPanelUI menuPanel;
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
        if (inGamePanel != null) inGamePanel.OnClickSetting += OpenSettingOverlay;
        if (settingPanel != null)
        {
            settingPanel.OnClose += CloseSettingOverlay;
            settingPanel.OnReplay += HandleReplay;
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

        if (inGamePanel != null) inGamePanel.OnClickSetting -= OpenSettingOverlay;
        if (settingPanel != null)
        {
            settingPanel.OnClose -= CloseSettingOverlay;
            settingPanel.OnReplay -= HandleReplay;
        }

        base.OnDestroy();
    }

    public void OnGameStateChanged(GameState state)
    {
        HideAll();
        if (state == GameState.Menu || state == GameState.Win || state == GameState.Lose)
        {
            ComboTracker.Ins?.ResetCombo();
        }
        switch (state)
        {
            case GameState.Menu:
                menuPanel.Show();
                break;

            case GameState.InGame:
                inGamePanel.Show();
                break;

            case GameState.Win:
                winPanel.Show();
                break;

            case GameState.Lose:
                losePanel.Show();
                break;
        }
    }

    private void HideAll()
    {
        menuPanel.Hide();
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
        bool consumed = InventoryService.Ins.UseItem(itemType);
        if (!consumed) return;
        switch (itemType)
        {
            case ItemType.Undo:
                LevelManager.Ins?.PerformUndo();
                break;

            case ItemType.AddTube:
                LevelManager.Ins?.AddExtraTube();
                break;

            case ItemType.ShuffleTube:
                LevelManager.Ins?.ToggleShuffleSelectMode();
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
}
