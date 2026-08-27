using UnityEngine;
using System;
using System.Collections.Generic;

public static class SkinDefinitions
{
    public static readonly string[] AllSkins =
    {
        "White-and-black",
        "White",
        "Black",
        "Gray-and-black",
        "Gray",
        "Red-and-black",
        "Red",
        "Pink-and-black",
        "Pink",
        "Green-and-black",
        "Green"
    };

    public static bool IsSplit(string skin)
    {
        return !string.IsNullOrEmpty(skin) && skin.IndexOf("-and-black", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string GetBaseName(string skin)
    {
        if (string.IsNullOrEmpty(skin)) return "Gray";
        int i = skin.IndexOf("-and-black", StringComparison.OrdinalIgnoreCase);
        return i >= 0 ? skin.Substring(0, i) : skin;
    }

    public static Color GetColor(string name)
    {
        if (string.IsNullOrEmpty(name)) return Color.gray;

        switch (name.Trim().ToLowerInvariant())
        {
            case "white": return Color.white;
            case "black": return Color.black;
            case "gray":
            case "grey": return new Color(0.45f, 0.45f, 0.45f, 1f);
            case "red": return new Color(0.9f, 0.03f, 0.03f, 1f);
            case "pink": return new Color(1f, 0.18f, 0.58f, 1f);
            case "green": return new Color(0.05f, 0.85f, 0.18f, 1f);
            default: return Color.gray;
        }
    }
}
