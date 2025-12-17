using UnityEngine;

public enum GameState { Menu, InGame, Settings, Win, Lose }

public class GameManager : Singleton<GameManager>
{
    [Header("Game States")]
    public GameState currentState;

    private int startLevel = 1;
    protected override void Initialize()
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

        //// (Optional) lock/unlock gameplay input
        //switch (currentState)
        //{
        //    case GameState.InGame:
        //        GameplayInput.I?.SetEnable(true);
        //        break;

        //    case GameState.Menu:
        //    case GameState.Settings:
        //    case GameState.Win:
        //    case GameState.Lose:
        //        GameplayInput.I?.SetEnable(false);
        //        break;
        //}
    }
}
