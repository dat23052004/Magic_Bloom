using UnityEngine;

public enum GameState { Menu, InGame, Settings, Win, Lose }

public class GameManager : Singleton<GameManager>
{
    [Header("Game States")]
    public GameState currentState;

    public int startLevel = 1;
    protected override void OnInit()
    {
        StartGame();
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
