using System.Collections.Generic;
using UnityEngine;

public static class ColorPalette
{
    static Dictionary<ColorId, Color> map = new()
    {
        { ColorId.Red, Color.red },
        { ColorId.Blue, Color.blue },
        { ColorId.Green, Color.green },
        { ColorId.Yellow, Color.yellow },
        { ColorId.Purple, new Color(0.6f, 0.2f, 0.8f) },
        { ColorId.Orange, new Color(1f, 0.5f, 0f) },
    };

    public static Color GetColor(ColorId colorId)
    {
        return map[colorId];
    }
}
