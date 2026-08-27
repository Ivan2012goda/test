using UnityEngine;
using System;

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
        int index = skin.IndexOf("-and-black", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? skin.Substring(0, index) : skin;
    }

    public static Color GetColor(string name)
    {
        switch ((name ?? "Gray").Trim().ToLowerInvariant())
        {
            case "white": return Color.white;
            case "black": return Color.black;
            case "gray":
            case "grey": return new Color(.45f, .45f, .45f, 1f);
            case "red": return new Color(.9f, .03f, .03f, 1f);
            case "pink": return new Color(1f, .18f, .58f, 1f);
            case "green": return new Color(.05f, .85f, .18f, 1f);
            default: return new Color(.45f, .45f, .45f, 1f);
        }
    }
}
