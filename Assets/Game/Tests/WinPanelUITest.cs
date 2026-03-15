using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giả lập hoàn thành level → chuyển sang WinPanel.
/// Gắn lên bất kỳ GameObject nào trong scene, kéo WinPanelUI vào Inspector.
/// Bấm Play → chờ level load → bấm phím T để trigger Win.
/// </summary>
public class WinPanelUITest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private KeyCode triggerKey = KeyCode.T;

    [Header("Reference")]
    [SerializeField] private WinPanelUI winPanel;

    private bool hasTriggered;

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey) && !hasTriggered)
        {
            hasTriggered = true;
            SimulateWin();
        }
    }

    [ContextMenu("Simulate Win")]
    private void SimulateWin()
    {
        Debug.Log("<color=cyan>[Test] Simulating level complete → Win state...</color>");

        // 1. Giả lập vài tube completed để có score
        if (ScoreManager.Ins != null)
        {
            ScoreManager.Ins.ResetScore();
            // Giả lập 3 tube hoàn thành liên tiếp (combo tăng)
            if (ComboTracker.Ins != null)
            {
                ComboTracker.Ins.RegisterSuccessfulPour();
                ScoreManager.Ins.OnTubeCompleted();

                ComboTracker.Ins.RegisterSuccessfulPour();
                ScoreManager.Ins.OnTubeCompleted();

                ComboTracker.Ins.RegisterSuccessfulPour();
                ScoreManager.Ins.OnTubeCompleted();
            }

            Debug.Log($"<color=cyan>[Test] Score: {ScoreManager.Ins.TotalStars} stars</color>");
        }

        // 2. Chuyển sang Win state (đi qua đúng flow UIManager.HandleWin)
        UIManager.Ins?.OnGameStateChanged(GameState.Win);

        // 3. Log kết quả
        int level = LevelManager.Ins != null ? LevelManager.Ins.CurrentLevel : 1;
        int coins = ShopService.Ins != null ? ShopService.Ins.Coins : 0;
        Debug.Log($"<color=cyan>[Test] Level: {level} | Coins hiện tại: {coins}</color>");
    }
}
