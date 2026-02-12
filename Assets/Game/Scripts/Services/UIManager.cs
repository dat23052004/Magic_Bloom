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
        if (comboTrack != null) comboTrack.OnComboChanged += HandleComboChanged;
    }

    protected override void OnInit()
    {
        HideAll();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (comboTrack != null) comboTrack.OnComboChanged -= HandleComboChanged;
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

            case GameState.Settings:
                settingPanel.Show();
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
        //inGamePanel.Hide();
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
}
