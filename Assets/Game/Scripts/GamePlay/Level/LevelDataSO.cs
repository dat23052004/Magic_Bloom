using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataSO", menuName = "Game/ Level Data", order = 1)]
public class LevelDataSO : ScriptableObject
{
    public int level = 1;
    public int capacity = 4;
    public int totalColor = 0;
    public LevelViewType viewType;

    public bool isBreatherLevel = false;      // Dễ thở hơn expected
    public bool isMilestoneLevel = false;     // Level đặc biệt
    public int targetMoves = 0;               // Expected solution moves

    // ✅ NEW: Metrics from estimator
    public int totalTransitions = 0;
    public int topBlockers = 0;
    public int estMoves = 0;

    //[HideInInspector]
    public List<TubeData> tubes;

    public void GenerateFromLevel(int levelNumber)
    {
        level = levelNumber;
        var config = LevelGenerator.GetLevelConfig(levelNumber);
        capacity = config.capacity;
        totalColor = config.colorCount;
        viewType = config.viewType;
        isBreatherLevel = config.isBreather;
        isMilestoneLevel = config.isMilestone;
        tubes = LevelGenerator.GenerateTubes(config);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        LevelValidator.Validate(this);
    }
#endif
}


public enum LevelViewType
{
    ShowAll,
    HideLowerLayers,
}
public enum ColorId
{
    None = -1,
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Purple = 4,
    Orange = 5,
    Pink = 6,
    Cyan = 7,
    Lime = 8,
    Brown = 9,
}


[System.Serializable]
public struct ColorSegment
{
    public ColorId colorId;
    public int Amount;
}

