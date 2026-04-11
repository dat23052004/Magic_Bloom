using System;
using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{
    private int totalStars;
    public int TotalStars => totalStars;
    public event Action<int> OnScoreChanged;

    public void ResetScore()
    {
        totalStars = 0;
        OnScoreChanged?.Invoke(totalStars);
    }

    /// <summary>
    /// Gọi khi tube hoàn thành.
    /// Không combo: +5⭐. Combo x2: +15⭐. Combo x3: +20⭐...
    /// Công thức: BASE_STARS * combo + BASE_STARS
    /// </summary>
    public int GetTubeCompletionReward()
    {
        int combo = ComboTracker.Ins != null ? ComboTracker.Ins.CurrentCombo : 0;

        return combo > 1
            ? Constant.BASE_STARS * combo + Constant.BASE_STARS
            : Constant.BASE_STARS;
    }

    public void AddStars(int amount)
    {
        if (amount <= 0) return;

        totalStars += amount;
        OnScoreChanged?.Invoke(totalStars);
    }

    public void OnTubeCompleted()
    {
        AddStars(GetTubeCompletionReward());
    }

    /// <summary>
    /// Rating 1-3⭐ cuối level.
    /// Max = nếu tất cả tubes hoàn thành liên tiếp (combo tăng dần).
    /// </summary>
    public int GetStarRating(int totalColorTubes)
    {
        if (totalColorTubes <= 0) return 1;

        // Max: tube đầu +5, rồi combo 1→2→3...
        int maxStars = Constant.BASE_STARS; // tube đầu không combo
        for (int i = 1; i < totalColorTubes; i++)
            maxStars += Constant.BASE_STARS * i + Constant.BASE_STARS;

        float ratio = (float)totalStars / Mathf.Max(1, maxStars);

        if (ratio >= 0.7f) return 3;
        if (ratio >= 0.4f) return 2;
        return 1;
    }
}
