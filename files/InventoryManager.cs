using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class InventoryData
{
    public List<string> availableSkins = new List<string>();
    public string equippedSkin = "Gray";

    public List<string> countKeys = new List<string>();
    public List<int> countValues = new List<int>();
}

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Data")]
    public List<string> availableSkins = new List<string> { "Gray" };
    private string equippedSkin = "Gray";

    private Dictionary<string, int> skinCounts = new Dictionary<string, int>();

    private string saveFilePath;
    private static InventoryManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            saveFilePath = Path.Combine(Application.persistentDataPath, "inventory_save.json");
            LoadInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        EquipSkin(equippedSkin);
    }

    public void SaveInventory()
    {
        InventoryData data = new InventoryData
        {
            availableSkins = this.availableSkins,
            equippedSkin = this.equippedSkin
        };

        data.countKeys.Clear();
        data.countValues.Clear();
        foreach (var pair in skinCounts)
        {
            data.countKeys.Add(pair.Key);
            data.countValues.Add(pair.Value);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("<color=cyan>Инвентарь сохранен в файл:</color> " + saveFilePath);
    }

    public void LoadInventory()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);

            if (data != null)
            {
                if (data.availableSkins != null && data.availableSkins.Count > 0)
                {
                    availableSkins = data.availableSkins;
                }
                equippedSkin = !string.IsNullOrEmpty(data.equippedSkin) ? data.equippedSkin : "Gray";

                skinCounts.Clear();
                for (int i = 0; i < data.countKeys.Count; i++)
                {
                    if (i < data.countValues.Count)
                    {
                        skinCounts[data.countKeys[i]] = data.countValues[i];
                    }
                }

                foreach (var skin in availableSkins)
                {
                    if (!skinCounts.ContainsKey(skin))
                    {
                        skinCounts[skin] = 1;
                    }
                }

                Debug.Log("<color=cyan>Инвентарь успешно загружен из файла.</color>");
            }
        }
        else
        {
            foreach (var skin in availableSkins)
            {
                skinCounts[skin] = 1;
            }
            SaveInventory();
        }
    }

    public void AddItem(string skinName)
    {
        if (!availableSkins.Contains(skinName))
        {
            availableSkins.Add(skinName);
            skinCounts[skinName] = 1;
            Debug.Log($"<color=green>Новый предмет добавлен в инвентарь:</color> {skinName} (x1)");
        }
        else
        {
            if (skinCounts.ContainsKey(skinName))
            {
                skinCounts[skinName]++;
            }
            else
            {
                skinCounts[skinName] = 1;
            }
            Debug.Log($"<color=green>Повторное выпадение предмета:</color> {skinName} (Всего: {skinCounts[skinName]})");
        }

        SaveInventory();
    }

    public int GetSkinCount(string skinName)
    {
        if (skinCounts.ContainsKey(skinName))
        {
            return skinCounts[skinName];
        }
        return availableSkins.Contains(skinName) ? 1 : 0;
    }

    public void EquipSkin(string skinName)
    {
        if (availableSkins.Contains(skinName))
        {
            equippedSkin = skinName;
            SaveInventory();
            Debug.Log("<color=green>Скин надет:</color> " + skinName);

            WeaponGenerator weaponGen = Object.FindAnyObjectByType<WeaponGenerator>();
            if (weaponGen != null)
            {
                weaponGen.ApplySkinToActiveWeapon(skinName);
            }
        }
    }

    public string GetEquippedSkin()
    {
        return equippedSkin;
    }
}