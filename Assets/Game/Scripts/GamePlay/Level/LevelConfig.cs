using System;
using UnityEngine;

[Serializable]
public class LevelConfig
{
    public int level;
    public int colorCount;
    public int tubeCount;
    public int emptyTubes;
    public int capacity;
    public int mixDepth;
    public LevelViewType viewType;
    public bool isBreather;
    public bool isMilestone;

    public LevelConfig(int levelNumber)
    {
        level = levelNumber;
        DetermineSpecialFlags();
        CalculateParameters();
        DetermineViewType();
    }

    private void DetermineSpecialFlags()
    {
        int[] breatherLevels = { 7, 14, 21, 28, 35, 42 };
        isBreather = Array.Exists(breatherLevels, x => x == level);
        isMilestone = level % 10 == 0;
    }

    private void CalculateParameters()
    {
        UnityEngine.Random.InitState(GetSeed());

        if (level <= 3)
        {
            colorCount = UnityEngine.Random.Range(2, 4);
            capacity = 6;
            mixDepth = 1;
        }
        else if (level <= 7)
        {
            colorCount = UnityEngine.Random.Range(3, 6);
            capacity = 7;
            mixDepth = UnityEngine.Random.Range(1, 3);
        }
        else if (level <= 10)
        {
            colorCount = UnityEngine.Random.Range(5, 8);
            capacity = 7;
            mixDepth = 2;
        }
        else if (level <= 15)
        {
            colorCount = UnityEngine.Random.Range(6, 9);
            capacity = 8;
            mixDepth = UnityEngine.Random.Range(2, 4);
        }
        else if (level <= 20)
        {
            colorCount = UnityEngine.Random.Range(7, 10);
            capacity = 8;
            mixDepth = 3;
        }
        else if (level <= 25)
        {
            colorCount = UnityEngine.Random.Range(8, 10);
            capacity = 8;
            mixDepth = UnityEngine.Random.Range(3, 5);
        }
        else if (level <= 30)
        {
            colorCount = UnityEngine.Random.Range(9, 11);
            capacity = 9;
            mixDepth = 4;
        }
        else if (level <= 35)
        {
            colorCount = UnityEngine.Random.Range(10, 11);
            capacity = 9;
            mixDepth = UnityEngine.Random.Range(4, 6);
        }
        else if (level <= 40)
        {
            colorCount = UnityEngine.Random.Range(10, 12);
            capacity = 9;
            mixDepth = 5;
        }
        else if (level <= 45)
        {
            colorCount = UnityEngine.Random.Range(10, 12);
            capacity = 10;
            mixDepth = UnityEngine.Random.Range(5, 7);
        }
        else
        {
            colorCount = UnityEngine.Random.Range(10, 12);
            capacity = 10;
            mixDepth = 6;
        }

        if (isBreather)
            mixDepth = Mathf.Max(mixDepth - 1, 1);

        if (isMilestone)
            mixDepth += UnityEngine.Random.Range(1, 3);

        colorCount = Mathf.Clamp(colorCount, 2, 11);
        capacity = Mathf.Clamp(capacity, 6, 10);
        mixDepth = Mathf.Clamp(mixDepth, 1, 8);

        if (mixDepth <= 2) emptyTubes = 2;
        else emptyTubes = 3;

        // Breather: luôn +1 nếu chưa max
        if (isBreather && emptyTubes < 4)
            emptyTubes++;

        emptyTubes = Mathf.Clamp(emptyTubes, 2, 4);

        // ── Enforce total <= 12 ──
        const int maxTubes = 12;
        if (colorCount + emptyTubes > maxTubes)
            colorCount = maxTubes - emptyTubes;

        colorCount = Mathf.Max(colorCount, 2);
        tubeCount = colorCount + emptyTubes;
    }

    private void DetermineViewType()
    {
        if (isBreather || isMilestone)
        {
            viewType = LevelViewType.ShowAll;
            return;
        }

        UnityEngine.Random.InitState(GetSeed() + 1000);
        float hideChance = level <= 10 ? 0.4f
                         : level <= 20 ? 0.3f
                         : level <= 30 ? 0.2f
                         : 0.1f;

        viewType = UnityEngine.Random.value < hideChance
            ? LevelViewType.HideLowerLayers
            : LevelViewType.ShowAll;
    }

    private const int SEED_SALT = unchecked((int)0xA7B3C1D5);
    private int GetSeed() => level * 12345 + SEED_SALT;
}