using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataSO", menuName = "Game/ Level Data", order = 1)]
public class LevelDataSO : ScriptableObject
{
    public int level;
    public int capacity = 4;
    public int totalColor = 0;
    public LevelViewType type;
     
    public List<TubeData> tubes;

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
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Orange
}
[System.Serializable]
public struct ColorSegment
{
    public ColorId colorId;
    public int Amount;
}

