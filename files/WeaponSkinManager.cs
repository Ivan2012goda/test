using UnityEngine;
using System;

public class WeaponSkinManager : MonoBehaviour
{
    public static WeaponSkinManager Instance { get; private set; }

    [Header("Твои объекты оружия")]
    [SerializeField] private Transform player;

    private string currentSkin = "Gray";
    public string CurrentSkin => currentSkin;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        ApplySavedSkin();
    }

    public void ApplySkin(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName)) skinName = "Gray";
        currentSkin = skinName;

        Transform m9 = FindExact("player/Main camera/WeaponHoldPoint/m9_bayonet");
        Transform deagle = FindExact("player/Main camera/WeaponHoldPoint2/Deagle");

        if (m9 != null) ApplyM9(m9, skinName);
        else Debug.LogWarning("[WeaponSkinManager] Не найден: player/Main camera/WeaponHoldPoint/m9_bayonet");

        if (deagle != null) ApplyDeagle(deagle, skinName);
        else Debug.LogWarning("[WeaponSkinManager] Не найден: player/Main camera/WeaponHoldPoint2/Deagle");

        PlayerPrefs.SetString("EquippedSkin", skinName);
        PlayerPrefs.Save();
    }

    public void ApplySavedSkin()
    {
        ApplySkin(PlayerPrefs.GetString("EquippedSkin", "Gray"));
    }

    private Transform FindExact(string path)
    {
        if (player != null)
        {
            string relative = path.StartsWith("player/", StringComparison.OrdinalIgnoreCase)
                ? path.Substring("player/".Length)
                : path;
            Transform found = player.Find(relative);
            if (found != null) return found;
        }

        GameObject root = GameObject.Find("player");
        if (root == null) return null;

        string relativePath = path.Substring("player/".Length);
        return root.transform.Find(relativePath);
    }

    private void ApplyM9(Transform root, string skin)
    {
        Color main = SkinDefinitions.GetColor(SkinDefinitions.GetBaseName(skin));
        bool split = SkinDefinitions.IsSplit(skin);

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name.Trim().ToLowerInvariant();

            if (split && (n == "handle" || n == "handle.001"))
                SetColor(r, Color.black);
            else
                SetColor(r, main);
        }
    }

    private void ApplyDeagle(Transform root, string skin)
    {
        Color main = SkinDefinitions.GetColor(SkinDefinitions.GetBaseName(skin));
        bool split = SkinDefinitions.IsSplit(skin);

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name.Trim().ToLowerInvariant();

            if (split && n.Contains("ручка"))
                SetColor(r, Color.black);
            else if (split && n.Contains("верх"))
                SetColor(r, main);
            else
                SetColor(r, main);
        }
    }

    private void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        Material[] materials = renderer.materials;
        foreach (Material material in materials)
        {
            if (material == null) continue;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }

    public void SetPlayer(Transform value)
    {
        player = value;
    }
}
