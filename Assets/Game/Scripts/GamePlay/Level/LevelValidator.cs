#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public static class LevelValidator
{
    public static void Validate(LevelDataSO level)
    {
        if (level.tubes == null || level.tubes.Count == 0)
        {
            Debug.LogWarning($"Level {level.level}: No tubes defined!");
            return;
        }

        var colorCounts = new Dictionary<ColorId, int>();
        int totalEmpty = 0;
        int totalFilled = 0;

        foreach (var tube in level.tubes)
        {
            if (tube.layers == null) continue;

            bool tubeEmpty = true;
            foreach (var layer in tube.layers)
            {
                if (layer == ColorId.None)
                {
                    totalEmpty++;
                }
                else
                {
                    tubeEmpty = false;
                    totalFilled++;

                    if (!colorCounts.ContainsKey(layer))
                        colorCounts[layer] = 0;
                    colorCounts[layer]++;
                }
            }
        }

        // Validation checks
        bool isValid = true;

        // Check 1: Each color should have exactly 'capacity' units
        foreach (var kvp in colorCounts)
        {
            if (kvp.Value != level.capacity)
            {
                Debug.LogWarning($"Level {level.level}: Color {kvp.Key} has {kvp.Value} units, expected {level.capacity}");
                isValid = false;
            }
        }

        // Check 2: Should have at least 2 empty tubes
        int emptyTubeCount = 0;
        foreach (var tube in level.tubes)
        {
            bool isEmpty = true;
            foreach (var layer in tube.layers)
            {
                if (layer != ColorId.None)
                {
                    isEmpty = false;
                    break;
                }
            }
            if (isEmpty) emptyTubeCount++;
        }

        // Update totalColor
        level.totalColor = colorCounts.Count;

        if (isValid)
        {
            Debug.Log($"✓ Level {level.level} validated: {level.totalColor} colors, {level.tubes.Count} tubes, {emptyTubeCount} empty");
        }
    }
}
#endif