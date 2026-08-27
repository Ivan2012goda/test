using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkinInterface : MonoBehaviour
{
    [Header("🎨 Интерфейс скинов")]
    public KeyCode toggleKey = KeyCode.F8;
    public bool createOnStart = true;

    private Canvas canvas;
    private GameObject panel;
    private Text selectedText;
    private readonly List<Button> buttons = new List<Button>();
    private bool opened;

    void Start()
    {
        if (createOnStart)
            BuildInterface();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        if (canvas == null)
            BuildInterface();

        opened = !opened;
        panel.SetActive(opened);

        Cursor.visible = opened;
        Cursor.lockState = opened ? CursorLockMode.None : CursorLockMode.Locked;

        Time.timeScale = opened ? 0f : 1f;
        UpdateSelectedText();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("SkinInterfaceCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024, 576);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("SkinEventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        panel = new GameObject("SkinPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 430f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.025f, 0.025f, 0.035f, 0.97f);

        CreateText("SkinTitle", "СКИНЫ ОРУЖИЯ", new Vector2(0f, -30f), new Vector2(560f, 45f), 28, TextAnchor.MiddleCenter);
        selectedText = CreateText("SelectedSkin", "Выбран: Gray", new Vector2(0f, -75f), new Vector2(560f, 30f), 18, TextAnchor.MiddleCenter);

        float startY = -125f;
        float x1 = -145f;
        float x2 = 145f;
        float step = 48f;

        string[] skins = SkinDefinitions.AllSkins;
        for (int i = 0; i < skins.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;
            float x = column == 0 ? x1 : x2;
            float y = startY - row * step;
            CreateSkinButton(skins[i], new Vector2(x, y));
        }

        Text hint = CreateText("Hint", "F8 — открыть/закрыть", new Vector2(0f, 25f), new Vector2(560f, 25f), 14, TextAnchor.MiddleCenter);
        hint.color = new Color(0.65f, 0.65f, 0.65f, 1f);

        panel.SetActive(false);
        opened = false;
    }

    private Text CreateText(string objectName, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(panel.transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text label = obj.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private void CreateSkinButton(string skin, Vector2 position)
    {
        GameObject buttonObject = new GameObject("SkinButton_" + skin, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 40f);
        rect.anchoredPosition = position;

        Image image = buttonObject.GetComponent<Image>();
        image.color = GetButtonColor(skin);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = GetButtonColor(skin);
        colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text label = CreateButtonLabel(buttonObject.transform, skin);
        button.onClick.AddListener(() => SelectSkin(skin));
        buttons.Add(button);
    }

    private Text CreateButtonLabel(Transform parent, string text)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = obj.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 17;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontStyle = FontStyle.Bold;
        return label;
    }

    private Color GetButtonColor(string skin)
    {
        string baseName = SkinDefinitions.GetBaseName(skin);
        Color color = SkinDefinitions.GetColor(baseName);

        if (SkinDefinitions.IsSplit(skin))
            return Color.Lerp(color, Color.black, 0.45f);

        return Color.Lerp(color, Color.black, 0.15f);
    }

    public void SelectSkin(string skin)
    {
        if (WeaponSkinManager.Instance == null)
        {
            Debug.LogWarning("WeaponSkinManager не найден. Добавь WeaponSkinManager на любой GameObject сцены.");
            return;
        }

        WeaponSkinManager.Instance.ApplySkin(skin);

        InventoryManager inventory = Object.FindAnyObjectByType<InventoryManager>();
        if (inventory != null)
            inventory.EquipSkin(skin);

        UpdateSelectedText();
    }

    private void UpdateSelectedText()
    {
        if (selectedText == null) return;

        string skin = WeaponSkinManager.Instance != null
            ? WeaponSkinManager.Instance.CurrentSkin
            : PlayerPrefs.GetString("EquippedSkin", "Gray");

        selectedText.text = "Выбран: " + skin;
    }
}
