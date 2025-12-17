using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public GameObject menuPanel;
    public GameObject settingPanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject inGamePanel;

    protected override void Initialize()
    {
        HideAll();
    }

    public void OnGameStateChanged(GameState state)
    {
        HideAll();
        switch (state)
        {
            case GameState.Menu:
                menuPanel.SetActive(true);
                break;

            case GameState.InGame:
                inGamePanel.SetActive(true);
                break;

            case GameState.Settings:
                settingPanel.SetActive(true);
                break;

            case GameState.Win:
                winPanel.SetActive(true);
                break;

            case GameState.Lose:
                losePanel.SetActive(true);
                break;
        }
    }

    private void HideAll()
    {
        menuPanel.SetActive(false);
        inGamePanel.SetActive(false);
        settingPanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }
}
