using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SkinInterface : MonoBehaviour
{
    [Header("Интерфейс скинов")]
    public KeyCode toggleKey = KeyCode.F8;

    private Canvas canvas;
    private GameObject panel;
    private Text selectedText;
    private bool opened;

    void Start()
    {
        BuildInterface();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024, 576);

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        panel = new GameObject("Skins", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(620, 430);
        panel.GetComponent<Image>().color = new Color(.025f, .025f, .035f, .97f);

        CreateText("СКИНЫ", new Vector2(0, -25), new Vector2(560, 45), 28);
        selectedText = CreateText("Выбран: Gray", new Vector2(0, -70), new Vector2(560, 30), 18);

        string[] skins = SkinDefinitions.AllSkins;
        float startY = -115f;
        float step = 48f;

        for (int i = 0; i < skins.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;
            CreateSkinButton(skins[i], new Vector2(column == 0 ? -150 : 150, startY - row * step));
        }

        panel.SetActive(false);
    }

    private Text CreateText(string value, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(panel.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private void CreateSkinButton(string skin, Vector2 position)
    {
        GameObject obj = new GameObject(skin, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(panel.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(270, 40);
        rect.anchoredPosition = position;

        Image image = obj.GetComponent<Image>();
        image.color = SkinDefinitions.GetColor(SkinDefinitions.GetBaseName(skin));
        if (SkinDefinitions.IsSplit(skin)) image.color = Color.Lerp(image.color, Color.black, .4f);

        Text label = CreateText(skin, Vector2.zero, new Vector2(260, 38), 16);
        label.transform.SetParent(obj.transform, false);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        obj.GetComponent<Button>().onClick.AddListener(() => SelectSkin(skin));
    }

    private void SelectSkin(string skin)
    {
        if (WeaponSkinManager.Instance == null)
        {
            Debug.LogWarning("WeaponSkinManager отсутствует на сцене.");
            return;
        }

        WeaponSkinManager.Instance.ApplySkin(skin);

        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory != null && inventory.availableSkins.Contains(skin))
            inventory.EquipSkin(skin);

        UpdateSelectedText();
    }

    private void UpdateSelectedText()
    {
        if (selectedText != null)
            selectedText.text = "Выбран: " + (WeaponSkinManager.Instance != null ? WeaponSkinManager.Instance.CurrentSkin : PlayerPrefs.GetString("EquippedSkin", "Gray"));
    }

    public void Toggle()
    {
        opened = !opened;
        panel.SetActive(opened);
        UpdateSelectedText();

        Cursor.visible = opened;
        Cursor.lockState = opened ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
