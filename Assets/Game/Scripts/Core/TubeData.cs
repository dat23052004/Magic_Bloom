using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TubeData
{
    public ColorId[] layers;
    public List<ColorSegment> GetSegments()
    {
        List<ColorSegment> segments = new();
        if (layers == null || layers.Length == 0) return segments;

        ColorId currentColor = layers[0];
        int count = 0;

        foreach (var layer in layers)
        {
            if (layer == ColorId.None) continue;

            if (layer == currentColor) count++;
            else
            {
                if (count > 0)
                {
                    segments.Add(new ColorSegment() { colorId = currentColor, Amount = count });
                    currentColor = layer;
                    count = 1;
                }
            }
        }
        if (count > 0)
            segments.Add(new ColorSegment { colorId = currentColor, Amount = count });

        return segments;
    }
}
