using UnityEngine;

public enum GameState { Shop, InGame, Win }

public class GameManager : Singleton<GameManager>
{
    [Header("Game States")]
    public GameState currentState;

    public int startLevel = 1;
    protected override void OnInit()
    {
        StartGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            SaveService.ResetAll();
            ShopService.Ins?.ReloadAll();
            InventoryService.Ins?.ReloadFromSave();
            Debug.Log("[GameManager] All save data reset to default.");
        }
    }

    public void StartGame()
    {
        SwitchState(GameState.InGame);
        LevelManager.Ins.LoadLevel(startLevel);
    }
    public void SwitchState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        // Notify UI
        UIManager.Ins.OnGameStateChanged(currentState);
    }
}
