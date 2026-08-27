using UnityEngine;
using System;
using System.Collections.Generic;

public class WeaponSkinManager : MonoBehaviour
{
    public static WeaponSkinManager Instance { get; private set; }

    [Header("Автопоиск оружия")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private string m9Path = "player/Main camera/WeaponHoldPoint/m9_bayonet";
    [SerializeField] private string deaglePath = "player/Main camera/WeaponHoldPoint2/Deagle";

    private string currentSkin = "Gray";
    private readonly List<Material> createdMaterials = new List<Material>();

    public string CurrentSkin => currentSkin;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplySavedSkin();
    }

    public void ApplySkin(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName))
            skinName = "Gray";

        currentSkin = skinName;

        Transform m9 = FindWeapon(m9Path, "m9_bayonet");
        Transform deagle = FindWeapon(deaglePath, "Deagle");

        if (m9 != null)
            ApplyM9(m9, skinName);

        if (deagle != null)
            ApplyDeagle(deagle, skinName);

        PlayerPrefs.SetString("EquippedSkin", skinName);
        PlayerPrefs.Save();

        Debug.Log("[WeaponSkinManager] Applied skin: " + skinName);
    }

    public void ApplySavedSkin()
    {
        string saved = PlayerPrefs.GetString("EquippedSkin", "Gray");
        ApplySkin(saved);
    }

    private Transform FindWeapon(string configuredPath, string objectName)
    {
        if (playerRoot != null)
        {
            Transform found = playerRoot.Find(configuredPath.Replace("player/", ""));
            if (found != null) return found;
        }

        GameObject exact = GameObject.Find(objectName);
        if (exact != null) return exact.transform;

        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (!go.scene.IsValid()) continue;
            if (string.Equals(go.name, objectName, StringComparison.OrdinalIgnoreCase))
                return go.transform;
        }

        return null;
    }

    private void ApplyM9(Transform root, string skin)
    {
        bool split = SkinDefinitions.IsSplit(skin);
        Color main = SkinDefinitions.GetColor(SkinDefinitions.GetBaseName(skin));
        Color secondary = Color.black;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            string n = renderer.gameObject.name.ToLowerInvariant();

            if (!split)
            {
                SetRendererColor(renderer, main);
                continue;
            }

            // M9: blade = main color, handle + handle.001 = black.
            if (IsM9Handle(n))
                SetRendererColor(renderer, secondary);
            else if (IsM9Blade(n))
                SetRendererColor(renderer, main);
            else
                SetRendererColor(renderer, main);
        }
    }

    private void ApplyDeagle(Transform root, string skin)
    {
        bool split = SkinDefinitions.IsSplit(skin);
        Color main = SkinDefinitions.GetColor(SkinDefinitions.GetBaseName(skin));
        Color secondary = Color.black;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            string n = renderer.gameObject.name.ToLowerInvariant();

            if (!split)
            {
                SetRendererColor(renderer, main);
                continue;
            }

            // Deagle: objects named "верх" are main color, objects named "ручка" are black.
            if (ContainsAny(n, "ручка", "handle", "grip"))
                SetRendererColor(renderer, secondary);
            else if (ContainsAny(n, "верх", "upper", "slide", "body"))
                SetRendererColor(renderer, main);
            else
                SetRendererColor(renderer, main);
        }
    }

    private bool IsM9Handle(string name)
    {
        return ContainsAny(name, "handle", "handle.001", "рукоят", "grip");
    }

    private bool IsM9Blade(string name)
    {
        return ContainsAny(name, "blade", "лезв", "m9", "knife");
    }

    private bool ContainsAny(string value, params string[] terms)
    {
        foreach (string term in terms)
        {
            if (value.Contains(term)) return true;
        }
        return false;
    }

    private void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        // renderer.material creates an instance for this renderer, so changing the skin
        // does not alter the shared material asset.
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null) continue;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }
    }

    public void SetPlayerRoot(Transform root)
    {
        playerRoot = root;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
