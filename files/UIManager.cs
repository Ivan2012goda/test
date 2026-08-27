using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GameObject mainMenuPanel;
    private GameObject mapSelectPanel;
    private GameObject pausePanel;
    private GameObject settingsPanel;
    private GameObject inventoryPanel;
    private GameObject casesPanel;
    private GameObject winResultPanel;
    private GameObject cheatPanel;
    private GameObject interfacePanel;
    private GameObject crosshairSettingsPanel;

    private TextMeshProUGUI mapSelectBotsBtnText;
    private TextMeshProUGUI crosshairEspBtnText;
    private TextMeshProUGUI crosshairColorBtnText;
    private RectTransform inventoryContentPanel;
    private RectTransform rouletteContentPanel;
    private ScrollRect rouletteScrollRect;
    private TextMeshProUGUI notificationText;
    private TextMeshProUGUI winResultText;
    private Coroutine notificationCoroutine;

    private RectTransform crosshairPreviewArea;

    [System.Serializable]
    public class HUDElementData
    {
        public string elementName;
        public Vector2 anchoredPosition;
    }

    [System.Serializable]
    public class SaveData
    {
        public HUDElementData[] elements;
    }

    [Header("Настройки HUD для перетаскивания")]
    [SerializeField] private Transform hudCanvasTransform;
    private string hudSavePath;
    private RectTransform draggingElement;
    private Vector2 hudDragOffset;
    private List<RectTransform> hudElementsToDrag = new List<RectTransform>();

    private bool isPaused = false;
    private bool isSpinning = false;

    [Header("Настройки ботов")]
    public GameObject botPrefab;
    public float botHealthSetting = 100f;
    public int botMaxCountSetting = 5;
    public bool botsEnabledSetting = true;

    private List<GameObject> activeBots = new List<GameObject>();
    private Vector3 mapCenter = Vector3.zero;
    private Vector3 mapSize = new Vector3(50f, 1f, 50f);
    public int ActiveBotsCount => activeBots.Count;

    public struct CaseItem
    {
        public string name;
        public float dropChance;
        public Color color;

        public CaseItem(string name, float chance, Color col)
        {
            this.name = name;
            this.dropChance = chance;
            this.color = col;
        }
    }

    // Пул скинов и шансы выпадения
    private List<CaseItem> casePool = new List<CaseItem>()
    {
        new CaseItem("White-and-black", 1.0f, Color.white),
        new CaseItem("White", 3.0f, Color.white),
        new CaseItem("Black", 5.0f, Color.black),
        new CaseItem("Gray-and-black", 7.0f, Color.gray),
        new CaseItem("Gray", 10.0f, Color.gray),
        new CaseItem("Red-and-black", 15.0f, Color.red),
        new CaseItem("Red", 20.0f, Color.red),
        new CaseItem("Pink-and-black", 25.0f, new Color(1f, 0.41f, 0.71f)),
        new CaseItem("Pink", 30.0f, new Color(1f, 0.41f, 0.71f)),
        new CaseItem("Green-and-black", 40.0f, Color.green),
        new CaseItem("Green", 50.0f, Color.green)
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        hudSavePath = Application.persistentDataPath + "/hud_layout.json";
    }

    void Start()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Main_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1024, 576);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (hudCanvasTransform == null) hudCanvasTransform = canvas.transform;

        BuildAllUI(canvas.transform);
        BuildNotificationUI(canvas.transform);
        ShowPanel(mainMenuPanel);

        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player != null) player.DeactivatePlayer();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LoadHUDLayout();
    }

    void Update()
    {
        HandleHUDDragging();

        if (!isPaused && botsEnabledSetting && botPrefab != null)
        {
            activeBots.RemoveAll(bot => bot == null);
            int safetyCounter = 0;
            while (activeBots.Count < botMaxCountSetting && safetyCounter < 50)
            {
                SpawnSingleBot();
                safetyCounter++;
            }
        }

        bool isAnyUIActive = isPaused ||
                             (mainMenuPanel != null && mainMenuPanel.activeSelf) ||
                             (mapSelectPanel != null && mapSelectPanel.activeSelf) ||
                             (settingsPanel != null && settingsPanel.activeSelf) ||
                             (inventoryPanel != null && inventoryPanel.activeSelf) ||
                             (casesPanel != null && casesPanel.activeSelf) ||
                             (cheatPanel != null && cheatPanel.activeSelf) ||
                             (interfacePanel != null && interfacePanel.activeSelf) ||
                             (crosshairSettingsPanel != null && crosshairSettingsPanel.activeSelf);

        if (isAnyUIActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if ((pausePanel != null && pausePanel.activeSelf) || cheatPanel.activeSelf || interfacePanel.activeSelf || crosshairSettingsPanel.activeSelf || !isAnyUIActive)
            {
                TogglePause();
            }
        }
    }

    void HandleHUDDragging()
    {
        if (interfacePanel != null && interfacePanel.activeSelf && hudElementsToDrag.Count > 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                foreach (RectTransform rt in hudElementsToDrag)
                {
                    if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                    {
                        draggingElement = rt;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            (RectTransform)rt.parent, mousePos, null, out hudDragOffset);
                        hudDragOffset = (Vector2)rt.anchoredPosition - hudDragOffset;
                        break;
                    }
                }
            }

            if (Input.GetMouseButton(0) && draggingElement != null)
            {
                Vector2 mousePos = Input.mousePosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)draggingElement.parent, mousePos, null, out Vector2 localPoint))
                {
                    draggingElement.anchoredPosition = localPoint + hudDragOffset;
                }
            }

            if (Input.GetMouseButtonUp(0) && draggingElement != null)
            {
                draggingElement = null;
                SaveHUDLayout();
            }
        }
    }

    void InitHUDCustomizer()
    {
        hudElementsToDrag.Clear();
        Interface interfaceScript = Object.FindAnyObjectByType<Interface>();
        if (interfaceScript != null) interfaceScript.SetEditingMode(true);

        GameHUDManager hudManager = Object.FindAnyObjectByType<GameHUDManager>();
        if (hudManager != null)
        {
            if (hudManager.hpText != null) hudElementsToDrag.Add(hudManager.hpText.GetComponent<RectTransform>());
            if (hudManager.ammoText != null) hudElementsToDrag.Add(hudManager.ammoText.GetComponent<RectTransform>());
        }
    }

    void CloseHUDCustomizer()
    {
        Interface interfaceScript = Object.FindAnyObjectByType<Interface>();
        if (interfaceScript != null) interfaceScript.SetEditingMode(false);
    }

    public void SaveHUDLayout()
    {
        GameHUDManager hudManager = Object.FindAnyObjectByType<GameHUDManager>();
        if (hudManager == null) return;

        List<HUDElementData> elementList = new List<HUDElementData>();

        if (hudManager.hpText != null)
        {
            RectTransform rt = hudManager.hpText.GetComponent<RectTransform>();
            elementList.Add(new HUDElementData { elementName = hudManager.hpText.name, anchoredPosition = rt.anchoredPosition });
        }
        if (hudManager.ammoText != null)
        {
            RectTransform rt = hudManager.ammoText.GetComponent<RectTransform>();
            elementList.Add(new HUDElementData { elementName = hudManager.ammoText.name, anchoredPosition = rt.anchoredPosition });
        }

        SaveData data = new SaveData { elements = elementList.ToArray() };
        File.WriteAllText(hudSavePath, JsonUtility.ToJson(data, true));
    }

    public void LoadHUDLayout()
    {
        if (File.Exists(hudSavePath))
        {
            try
            {
                string json = File.ReadAllText(hudSavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                GameHUDManager hudManager = Object.FindAnyObjectByType<GameHUDManager>();
                if (data != null && data.elements != null && hudManager != null)
                {
                    foreach (var savedEl in data.elements)
                    {
                        if (hudManager.hpText != null && hudManager.hpText.name == savedEl.elementName)
                        {
                            hudManager.hpText.GetComponent<RectTransform>().anchoredPosition = savedEl.anchoredPosition;
                        }
                        if (hudManager.ammoText != null && hudManager.ammoText.name == savedEl.elementName)
                        {
                            hudManager.ammoText.GetComponent<RectTransform>().anchoredPosition = savedEl.anchoredPosition;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Ошибка загрузки HUD файла: " + e.Message);
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        HideAll();
        if (isPaused)
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DisablePlayerLook(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            DisablePlayerLook(false);
        }
    }

    public void ShowPanel(GameObject target)
    {
        if (interfacePanel != null && interfacePanel.activeSelf && target != interfacePanel)
        {
            CloseHUDCustomizer();
        }

        HideAll();
        if (target != null)
        {
            target.SetActive(true);
            if (target == inventoryPanel) UpdateInventoryGrid();
            if (target == mapSelectPanel) UpdateMapSelectBotsButtonText();
            if (target == interfacePanel) InitHUDCustomizer();
            if (target == crosshairSettingsPanel) UpdateCrosshairPreview();
        }

        bool isMenu = (target == mainMenuPanel || target == inventoryPanel || target == casesPanel || target == settingsPanel || target == mapSelectPanel || target == cheatPanel || target == interfacePanel || target == crosshairSettingsPanel);

        if (isMenu)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DisablePlayerLook(true);
        }

        if (target == null)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            DisablePlayerLook(false);
        }
    }

    void DisablePlayerLook(bool disable)
    {
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player != null)
        {
            if (disable) player.DeactivatePlayer();
            else player.ActivatePlayer();
        }
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 0f;
        isPaused = false;
        ClearAllBots();
        ShowPanel(mainMenuPanel);
    }

    void HideAll()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (mapSelectPanel) mapSelectPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (inventoryPanel) inventoryPanel.SetActive(false);
        if (casesPanel) casesPanel.SetActive(false);
        if (cheatPanel) cheatPanel.SetActive(false);
        if (interfacePanel) interfacePanel.SetActive(false);
        if (crosshairSettingsPanel) crosshairSettingsPanel.SetActive(false);
    }

    void BuildAllUI(Transform parent)
    {
        mainMenuPanel = CreatePanel("MainMenu", parent, false);
        CreateTitleCS16(mainMenuPanel, "ГЛАВНОЕ МЕНЮ", new Vector2(40, -40));

        float startY = -100f;
        float stepY = -35f;

        CreateCs16Btn(mainMenuPanel, "Играть", new Vector2(40, startY), () => ShowPanel(mapSelectPanel));
        CreateCs16Btn(mainMenuPanel, "Инвентарь", new Vector2(40, startY + stepY * 1), () => ShowPanel(inventoryPanel));
        CreateCs16Btn(mainMenuPanel, "Кейсы", new Vector2(40, startY + stepY * 2), () => ShowPanel(casesPanel));
        CreateCs16Btn(mainMenuPanel, "Настройки", new Vector2(40, startY + stepY * 3), () => ShowPanel(settingsPanel));
        CreateCs16Btn(mainMenuPanel, "Выход", new Vector2(40, startY + stepY * 4), Application.Quit);

        inventoryPanel = CreatePanel("Inventory", parent, false);
        CreateTitle(inventoryPanel, "ИНВЕНТАРЬ", new Vector2(0, 200));
        CreateBtn(inventoryPanel, "ОТКРЫТЬ КЕЙС", new Vector2(-120, -180), () => ShowPanel(casesPanel));
        CreateBtn(inventoryPanel, "Назад", new Vector2(120, -180), () => ShowMainMenu());
        BuildFullscreenInventoryScrollView(inventoryPanel.transform);

        casesPanel = CreatePanel("Cases", parent, false);
        CreateTitle(casesPanel, "РУЛЕТКА КЕЙСОВ", new Vector2(0, 150));
        CreateBtn(casesPanel, "КРУТИТЬ КЕЙС", new Vector2(0, -110), () => {
            if (!isSpinning) StartCoroutine(SpinRouletteRoutine());
        });
        CreateBtn(casesPanel, "Перейти в инвентарь", new Vector2(0, -150), () => ShowPanel(inventoryPanel));
        CreateBtn(casesPanel, "Назад", new Vector2(0, -190), () => ShowPanel(mainMenuPanel));
        BuildRouletteScrollView(casesPanel.transform);

        mapSelectPanel = CreatePanel("MapSelect", parent, false);
        CreateTitle(mapSelectPanel, "ВЫБОР КАРТЫ", new Vector2(0, 170));

        CreateBtn(mapSelectPanel, "Классик", new Vector2(0, 95), () => StartProceduralGame(MapGenerator.MapStyle.ClassicDust, "Классик"));
        CreateBtn(mapSelectPanel, "Арена", new Vector2(0, 45), () => StartProceduralGame(MapGenerator.MapStyle.OpenArena, "Арена"));
        CreateBtn(mapSelectPanel, "Лабиринт", new Vector2(0, -5), () => StartProceduralGame(MapGenerator.MapStyle.Maze, "Лабиринт"));
        CreateBtn(mapSelectPanel, "Пустота", new Vector2(0, -55), () => StartProceduralGame(MapGenerator.MapStyle.Void, "Пустота"));

        GameObject mapBotsBtn = CreateBtn(mapSelectPanel, "Боты: ВКЛ", new Vector2(-200, 45), () => {
            botsEnabledSetting = !botsEnabledSetting;
            UpdateMapSelectBotsButtonText();
        });
        mapSelectBotsBtnText = mapBotsBtn.GetComponentInChildren<TextMeshProUGUI>();
        UpdateMapSelectBotsButtonText();

        CreateSlider(mapSelectPanel, "ХП Бота", new Vector2(-200, -5), 10f, 500f, botHealthSetting, (val) => { botHealthSetting = val; });
        CreateSlider(mapSelectPanel, "Кол-во ботов (до 20)", new Vector2(-200, -65), 1f, 20f, botMaxCountSetting, (val) => { botMaxCountSetting = Mathf.RoundToInt(val); });

        CreateBtn(mapSelectPanel, "Назад", new Vector2(0, -155), () => ShowPanel(mainMenuPanel));

        pausePanel = CreatePanel("PauseMenu", parent, true);
        CreateTitle(pausePanel, "ПАУЗА", new Vector2(0, 130));
        CreateBtn(pausePanel, "Продолжить", new Vector2(0, 70), TogglePause);

        CreateBtn(pausePanel, "Меню Читов (Bhop, Aimbot...)", new Vector2(0, 25), () => ShowPanel(cheatPanel));
        CreateBtn(pausePanel, "Настройки", new Vector2(0, -20), () => ShowPanel(settingsPanel));
        CreateBtn(pausePanel, "Выйти в меню", new Vector2(0, -65), ShowMainMenu);

        cheatPanel = CreatePanel("CheatMenu", parent, true);
        CreateTitle(cheatPanel, "ВНУТРЕННИЙ ЧИТ", new Vector2(0, 200), Color.red);

        float leftX = -180f;
        float rightX = 180f;
        float startCheatY = 120f;
        float stepCheatY = -42f;

        CreateBtn(cheatPanel, "Bhop: ВЫКЛ", new Vector2(leftX, startCheatY), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.bhopEnabled = !cheats.bhopEnabled;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cheats.bhopEnabled ? "Bhop: ВКЛ" : "Bhop: ВЫКЛ";
            }
        });

        CreateBtn(cheatPanel, "Aimbot: ВЫКЛ", new Vector2(leftX, startCheatY + stepCheatY * 1), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.aimbotEnabled = !cheats.aimbotEnabled;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cheats.aimbotEnabled ? "Aimbot: ВКЛ" : "Aimbot: ВЫКЛ";
            }
        });

        CreateBtn(cheatPanel, "RapidFire: ВЫКЛ", new Vector2(leftX, startCheatY + stepCheatY * 2), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.rapidFireEnabled = !cheats.rapidFireEnabled;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cheats.rapidFireEnabled ? "RapidFire: ВКЛ" : "RapidFire: ВЫКЛ";
            }
        });

        CreateSlider(cheatPanel, "Aimbot FOV", new Vector2(leftX, startCheatY + stepCheatY * 3.2f), 10f, 180f, 50f, (val) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null) cheats.aimbotFOV = val;
        });

        CreateSlider(cheatPanel, "Camera FOV", new Vector2(leftX, startCheatY + stepCheatY * 4.6f), 40f, 120f, 60f, (val) => {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player != null && player.playerCamera != null)
            {
                Camera camComp = player.playerCamera.GetComponent<Camera>();
                if (camComp != null) camComp.fieldOfView = val;
            }
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null) cheats.cameraFOV = val;
        });

        CreateBtn(cheatPanel, "ESP: ВКЛ", new Vector2(rightX, startCheatY), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.espBoxesEnabled = !cheats.espBoxesEnabled;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cheats.espBoxesEnabled ? "ESP: ВКЛ" : "ESP: ВЫКЛ";
            }
        });

        CreateBtn(cheatPanel, "Inf Ammo: ВЫКЛ", new Vector2(rightX, startCheatY + stepCheatY * 1), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.infiniteAmmoEnabled = !cheats.infiniteAmmoEnabled;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cheats.infiniteAmmoEnabled ? "Inf Ammo: ВКЛ" : "Inf Ammo: ВЫКЛ";
            }
        });

        CreateBtn(cheatPanel, "Godmode: ВЫКЛ", new Vector2(rightX, startCheatY + stepCheatY * 2), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.godModeEnabled = !cheats.godModeEnabled;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cheats.godModeEnabled ? "Godmode: ВКЛ" : "Godmode: ВЫКЛ";
            }
        });

        CreateSlider(cheatPanel, "Скорость (Speed)", new Vector2(rightX, startCheatY + stepCheatY * 3.2f), 1f, 1000f, 6f, (val) => {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player != null) player.walkSpeed = val;
        });

        CreateSlider(cheatPanel, "Сила прыжка (Jump)", new Vector2(rightX, startCheatY + stepCheatY * 4.6f), 1f, 1000f, 5f, (val) => {
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player != null) player.jumpForce = val;
        });

        CreateBtn(cheatPanel, "Rage-Kill (Убить всех ботов)", new Vector2(0, -165), () => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null) cheats.antiAimbotKillAll = true;
        });

        CreateBtn(cheatPanel, "Назад в паузу", new Vector2(0, -205), () => ShowPanel(pausePanel));

        settingsPanel = CreatePanel("Settings", parent, true);
        CreateTitle(settingsPanel, "НАСТРОЙКИ", new Vector2(0, 170));

        CreateSlider(settingsPanel, "Чувствительность", new Vector2(0, 45), 0.5f, 10f, 2f, (val) => {
            FirstPersonController p = Object.FindAnyObjectByType<FirstPersonController>();
            if (p) p.mouseSensitivity = val;
        });

        CreateBtn(settingsPanel, "⚙ Настройка интерфейса (HUD)", new Vector2(0, -15), () => ShowPanel(interfacePanel));
        CreateBtn(settingsPanel, "🎯 Настройка прицела (aim.cs)", new Vector2(0, -60), () => ShowPanel(crosshairSettingsPanel));

        CreateBtn(settingsPanel, "Назад", new Vector2(0, -135), () => {
            if (isPaused) ShowPanel(pausePanel); else ShowPanel(mainMenuPanel);
        });

        interfacePanel = CreatePanel("InterfaceSettings", parent, true);
        CreateTitle(interfacePanel, "НАСТРОЙКА ИНТЕРФЕЙСА", new Vector2(0, 150));

        GameObject hudInfoObj = new GameObject("HUDInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        hudInfoObj.transform.SetParent(interfacePanel.transform, false);
        hudInfoObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 60);
        hudInfoObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
        TextMeshProUGUI hudInfoTxt = hudInfoObj.GetComponent<TextMeshProUGUI>();
        hudInfoTxt.fontSize = 13;
        hudInfoTxt.alignment = TextAlignmentOptions.Center;
        hudInfoTxt.text = "Перетаскивайте элементы HUD (здоровье, патроны)\nпрямо мышкой на экране! Изменения сохраняются.";
        hudInfoTxt.raycastTarget = false;

        CreateBtn(interfacePanel, "Назад в настройки", new Vector2(0, -165), () => {
            CloseHUDCustomizer();
            ShowPanel(settingsPanel);
        });

        crosshairSettingsPanel = CreatePanel("CrosshairSettings", parent, false);
        CreateTitle(crosshairSettingsPanel, "НАСТРОЙКА ПРИЦЕЛА", new Vector2(0, 170));

        GameObject previewBox = new GameObject("PreviewBox", typeof(RectTransform), typeof(Image));
        previewBox.transform.SetParent(crosshairSettingsPanel.transform, false);
        RectTransform pbRt = previewBox.GetComponent<RectTransform>();
        pbRt.anchoredPosition = new Vector2(0, 120);
        pbRt.sizeDelta = new Vector2(100, 50);
        Image pbImg = previewBox.GetComponent<Image>();
        pbImg.color = new Color(0.1f, 0.1f, 0.12f, 0.8f);

        GameObject previewCh = new GameObject("PreviewCrosshair", typeof(RectTransform));
        previewCh.transform.SetParent(previewBox.transform, false);
        crosshairPreviewArea = previewCh.GetComponent<RectTransform>();
        crosshairPreviewArea.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairPreviewArea.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairPreviewArea.sizeDelta = Vector2.zero;
        crosshairPreviewArea.anchoredPosition = Vector2.zero;

        CreatePreviewCrosshairLines(crosshairPreviewArea);

        GameObject espBtn = CreateBtn(crosshairSettingsPanel, "ESP: ВКЛ", new Vector2(0, 75), (btnObj) => {
            PlayerCheats cheats = Object.FindAnyObjectByType<PlayerCheats>();
            if (cheats != null)
            {
                cheats.espBoxesEnabled = !cheats.espBoxesEnabled;
                if (crosshairEspBtnText != null)
                {
                    crosshairEspBtnText.text = cheats.espBoxesEnabled ? "ESP: ВКЛ" : "ESP: ВЫКЛ";
                }
            }
        });
        crosshairEspBtnText = espBtn.GetComponentInChildren<TextMeshProUGUI>();
        PlayerCheats existingCheats = Object.FindAnyObjectByType<PlayerCheats>();
        if (existingCheats != null && crosshairEspBtnText != null)
        {
            crosshairEspBtnText.text = existingCheats.espBoxesEnabled ? "ESP: ВКЛ" : "ESP: ВЫКЛ";
        }

        CreateSlider(crosshairSettingsPanel, "Длина линий", new Vector2(0, 30), 2f, 50f, 10f, (val) => {
            aim cAim = Object.FindAnyObjectByType<aim>();
            if (cAim != null) cAim.size = val;
            UpdateCrosshairPreview();
        });

        CreateSlider(crosshairSettingsPanel, "Толщина линий", new Vector2(0, -15), 1f, 10f, 2f, (val) => {
            aim cAim = Object.FindAnyObjectByType<aim>();
            if (cAim != null) cAim.thickness = val;
            UpdateCrosshairPreview();
        });

        CreateSlider(crosshairSettingsPanel, "Раздвиг линий", new Vector2(0, -60), 0f, 20f, 4f, (val) => {
            aim cAim = Object.FindAnyObjectByType<aim>();
            if (cAim != null) cAim.gap = val;
            UpdateCrosshairPreview();
        });

        CreateBtn(crosshairSettingsPanel, "Точка в центре: ВЫКЛ", new Vector2(0, -105), (btnObj) => {
            aim cAim = Object.FindAnyObjectByType<aim>();
            if (cAim != null)
            {
                cAim.centerDot = !cAim.centerDot;
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = cAim.centerDot ? "Точка в центре: ВКЛ" : "Точка в центре: ВЫКЛ";
                UpdateCrosshairPreview();
            }
        });

        GameObject colorBtn = CreateBtn(crosshairSettingsPanel, "Цвет: Зеленый", new Vector2(0, -150), (btnObj) => {
            aim cAim = Object.FindAnyObjectByType<aim>();
            if (cAim != null)
            {
                if (cAim.crosshairColor == Color.green) cAim.crosshairColor = Color.red;
                else if (cAim.crosshairColor == Color.red) cAim.crosshairColor = Color.yellow;
                else if (cAim.crosshairColor == Color.yellow) cAim.crosshairColor = Color.cyan;
                else if (cAim.crosshairColor == Color.cyan) cAim.crosshairColor = Color.white;
                else cAim.crosshairColor = Color.green;

                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    string colorName = "Зеленый";
                    if (cAim.crosshairColor == Color.red) colorName = "Красный";
                    else if (cAim.crosshairColor == Color.yellow) colorName = "Желтый";
                    else if (cAim.crosshairColor == Color.cyan) colorName = "Голубой";
                    else if (cAim.crosshairColor == Color.white) colorName = "Белый";
                    txt.text = "Цвет: " + colorName;
                }
                UpdateCrosshairPreview();
            }
        });
        crosshairColorBtnText = colorBtn.GetComponentInChildren<TextMeshProUGUI>();
        aim existingAimForColor = Object.FindAnyObjectByType<aim>();
        if (existingAimForColor != null && crosshairColorBtnText != null)
        {
            string colName = "Зеленый";
            if (existingAimForColor.crosshairColor == Color.red) colName = "Красный";
            else if (existingAimForColor.crosshairColor == Color.yellow) colName = "Желтый";
            else if (existingAimForColor.crosshairColor == Color.cyan) colName = "Голубой";
            else if (existingAimForColor.crosshairColor == Color.white) colName = "Белый";
            crosshairColorBtnText.text = "Цвет: " + colName;
        }

        CreateBtn(crosshairSettingsPanel, "Сохранить прицел", new Vector2(-110, -195), () => {
            aim cAim = Object.FindAnyObjectByType<aim>();
            if (cAim != null) cAim.SaveConfig();
        });

        CreateBtn(crosshairSettingsPanel, "Назад", new Vector2(110, -195), () => ShowPanel(settingsPanel));
    }

    void CreatePreviewCrosshairLines(RectTransform parent)
    {
        string[] names = { "Top", "Bottom", "Left", "Right", "Dot" };
        foreach (var n in names)
        {
            GameObject line = new GameObject(n, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            line.GetComponent<Image>().color = Color.green;
        }
        UpdateCrosshairPreview();
    }

    void UpdateCrosshairPreview()
    {
        if (crosshairPreviewArea == null) return;

        aim cAim = Object.FindAnyObjectByType<aim>();
        float currentSize = cAim != null ? cAim.size : 10f;
        float currentThick = cAim != null ? cAim.thickness : 2f;
        float currentGap = cAim != null ? cAim.gap : 4f;
        bool currentDot = cAim != null && cAim.centerDot;
        Color currentColor = cAim != null ? cAim.crosshairColor : Color.green;

        float scale = 0.6f;
        float displaySize = currentSize * scale;
        float displayThick = Mathf.Max(1f, currentThick * scale);
        float displayGap = currentGap * scale;

        foreach (Transform child in crosshairPreviewArea)
        {
            RectTransform rt = child.GetComponent<RectTransform>();
            Image img = child.GetComponent<Image>();
            if (rt == null || img == null) continue;

            img.color = currentColor;

            if (child.name == "Top")
            {
                rt.anchoredPosition = new Vector2(0, displayGap + displaySize / 2f);
                rt.sizeDelta = new Vector2(displayThick, displaySize);
            }
            else if (child.name == "Bottom")
            {
                rt.anchoredPosition = new Vector2(0, -(displayGap + displaySize / 2f));
                rt.sizeDelta = new Vector2(displayThick, displaySize);
            }
            else if (child.name == "Left")
            {
                rt.anchoredPosition = new Vector2(-(displayGap + displaySize / 2f), 0);
                rt.sizeDelta = new Vector2(displaySize, displayThick);
            }
            else if (child.name == "Right")
            {
                rt.anchoredPosition = new Vector2(displayGap + displaySize / 2f, 0);
                rt.sizeDelta = new Vector2(displaySize, displayThick);
            }
            else if (child.name == "Dot")
            {
                child.gameObject.SetActive(currentDot);
                float dotSz = Mathf.Max(2f, displayThick);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(dotSz, dotSz);
            }
        }
    }

    void UpdateMapSelectBotsButtonText()
    {
        if (mapSelectBotsBtnText != null) mapSelectBotsBtnText.text = botsEnabledSetting ? "Боты: ВКЛ" : "Боты: ВЫКЛ";
    }

    void StartProceduralGame(MapGenerator.MapStyle style, string styleName)
    {
        HideAll();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        MapGenerator mapGen = Object.FindAnyObjectByType<MapGenerator>();
        if (mapGen != null) mapGen.SetMapStyle(style);

        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player != null)
        {
            Transform sp = GameObject.Find("spawnpoint")?.transform;
            Vector3 spawnPos = (sp != null) ? sp.position : new Vector3(0, 1.5f, 0);
            player.ActivatePlayerAt(spawnPos);
        }

        ClearAllBots();
        ShowNotification("Старт игры! Стиль: " + styleName);
    }

    public void SetMapBounds(Vector3 center, Vector3 size)
    {
        mapCenter = center;
        mapSize = size;
        ClearAllBots();
    }

    void SpawnSingleBot()
    {
        if (botPrefab == null) return;

        float rx = Random.Range(-mapSize.x / 2.5f, mapSize.x / 2.5f);
        float rz = Random.Range(-mapSize.z / 2.5f, mapSize.z / 2.5f);
        Vector3 spawnPos = mapCenter + new Vector3(rx, 2f, rz);

        GameObject botObj = Instantiate(botPrefab, spawnPos, Quaternion.identity);

        SimpleBot botScript = botObj.GetComponent<SimpleBot>();
        if (botScript != null) botScript.maxHealth = botHealthSetting;

        activeBots.Add(botObj);
    }

    public void ClearAllBots()
    {
        foreach (var bot in activeBots)
        {
            if (bot != null) Destroy(bot);
        }
        activeBots.Clear();
    }

    void BuildNotificationUI(Transform parent)
    {
        GameObject nObj = new GameObject("NotificationText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nObj.transform.SetParent(parent, false);

        RectTransform rt = nObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(20, 0);
        rt.sizeDelta = new Vector2(400, 150);

        notificationText = nObj.GetComponent<TextMeshProUGUI>();
        notificationText.fontSize = 18;
        notificationText.color = Color.yellow;
        notificationText.alignment = TextAlignmentOptions.Left;
        notificationText.raycastTarget = false;
        notificationText.text = "";
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;
        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
        notificationCoroutine = StartCoroutine(DisplayNotificationRoutine(message));
    }

    IEnumerator DisplayNotificationRoutine(string msg)
    {
        notificationText.text = msg;
        yield return new WaitForSecondsRealtime(4.0f);
        notificationText.text = "";
    }

    void BuildFullscreenInventoryScrollView(Transform parent)
    {
        GameObject sv = new GameObject("InventoryScroll", typeof(RectTransform), typeof(ScrollRect));
        sv.transform.SetParent(parent, false);
        RectTransform svRt = sv.GetComponent<RectTransform>();

        svRt.anchorMin = new Vector2(0.1f, 0.25f);
        svRt.anchorMax = new Vector2(0.9f, 0.85f);
        svRt.sizeDelta = Vector2.zero;
        svRt.anchoredPosition = Vector2.zero;

        ScrollRect scroll = sv.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(sv.transform, false);
        RectTransform vpRt = vp.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.sizeDelta = Vector2.zero;
        scroll.viewport = vpRt;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(vp.transform, false);
        inventoryContentPanel = content.GetComponent<RectTransform>();

        inventoryContentPanel.anchorMin = new Vector2(0, 1);
        inventoryContentPanel.anchorMax = new Vector2(1, 1);
        inventoryContentPanel.pivot = new Vector2(0.5f, 1f);
        inventoryContentPanel.anchoredPosition = Vector2.zero;
        inventoryContentPanel.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup glg = content.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(120, 110);
        glg.spacing = new Vector2(15, 15);
        glg.padding = new RectOffset(10, 10, 10, 10);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        glg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = inventoryContentPanel;
    }

    void UpdateInventoryGrid()
    {
        if (inventoryContentPanel == null) return;

        foreach (Transform child in inventoryContentPanel) Destroy(child.gameObject);

        InventoryManager inv = Object.FindAnyObjectByType<InventoryManager>();
        if (inv == null) return;

        foreach (string skin in inv.availableSkins)
        {
            bool isEquipped = (inv.GetEquippedSkin() == skin);

            GameObject cell = new GameObject("Skin_" + skin, typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement));
            cell.transform.SetParent(inventoryContentPanel, false);

            LayoutElement le = cell.GetComponent<LayoutElement>();
            le.minWidth = 120;
            le.minHeight = 110;

            cell.GetComponent<Image>().color = isEquipped ? new Color(0.15f, 0.45f, 0.2f) : new Color(0.25f, 0.25f, 0.3f);

            string skinCopy = skin;
            cell.GetComponent<Button>().onClick.AddListener(() => {
                inv.EquipSkin(skinCopy);
                UpdateInventoryGrid();
            });

            // Иконка предмета в инвентаре с поддержкой комбинированных цветов
            GameObject preview = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            preview.transform.SetParent(cell.transform, false);
            RectTransform pRt = preview.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0, 15);
            pRt.sizeDelta = new Vector2(80, 50);
            preview.GetComponent<Image>().color = GetSkinColor(skinCopy);

            if (skinCopy.EndsWith("-and-black"))
            {
                GameObject inner = new GameObject("InnerSquare", typeof(RectTransform), typeof(Image));
                inner.transform.SetParent(preview.transform, false);
                RectTransform innerRt = inner.GetComponent<RectTransform>();
                innerRt.anchorMin = new Vector2(0.5f, 0.5f);
                innerRt.anchorMax = new Vector2(0.5f, 0.5f);
                innerRt.pivot = new Vector2(0.5f, 0.5f);
                innerRt.anchoredPosition = Vector2.zero;
                innerRt.sizeDelta = new Vector2(25, 25);
                inner.GetComponent<Image>().color = Color.black;
                inner.GetComponent<Image>().raycastTarget = false;
            }

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(cell.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = isEquipped ? $"{skinCopy}\n<b>[НАДЕТО]</b>" : skinCopy;
            tmp.alignment = TextAlignmentOptions.Bottom;
            tmp.fontSize = 12;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }
    }

    void BuildRouletteScrollView(Transform parent)
    {
        GameObject sv = new GameObject("CasesScroll", typeof(RectTransform), typeof(ScrollRect));
        sv.transform.SetParent(parent, false);
        RectTransform svRt = sv.GetComponent<RectTransform>();
        svRt.sizeDelta = new Vector2(400, 100);
        svRt.anchoredPosition = new Vector2(0, 20);

        rouletteScrollRect = sv.GetComponent<ScrollRect>();
        rouletteScrollRect.horizontal = true;
        rouletteScrollRect.vertical = false;

        GameObject vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(sv.transform, false);
        RectTransform vpRt = vp.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.sizeDelta = Vector2.zero;
        rouletteScrollRect.viewport = vpRt;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(vp.transform, false);
        rouletteContentPanel = content.GetComponent<RectTransform>();
        rouletteContentPanel.anchorMin = new Vector2(0, 0.5f);
        rouletteContentPanel.anchorMax = new Vector2(0, 0.5f);
        rouletteContentPanel.pivot = new Vector2(0, 0.5f);

        HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        rouletteScrollRect.content = rouletteContentPanel;

        GameObject pointer = new GameObject("Pointer", typeof(RectTransform), typeof(Image));
        pointer.transform.SetParent(parent, false);
        RectTransform pRt = pointer.GetComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(4f, 110f);
        pRt.anchoredPosition = new Vector2(0, 20);
        pointer.GetComponent<Image>().color = Color.red;

        BuildWinPopupUI(parent);
    }

    void BuildWinPopupUI(Transform parent)
    {
        winResultPanel = new GameObject("WinResultPanel", typeof(RectTransform), typeof(Image));
        winResultPanel.transform.SetParent(parent, false);
        RectTransform rt = winResultPanel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 160);
        rt.anchoredPosition = new Vector2(0, -20);
        winResultPanel.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

        CreateTitle(winResultPanel, "РЕЗУЛЬТАТ РОЗЫГРЫШЕЙ", new Vector2(0, 50), Color.yellow);

        GameObject txtObj = new GameObject("WinText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(winResultPanel.transform, false);
        txtObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 50);
        winResultText = txtObj.GetComponent<TextMeshProUGUI>();
        winResultText.fontSize = 15;
        winResultText.alignment = TextAlignmentOptions.Center;
        winResultText.raycastTarget = false;

        CreateBtn(winResultPanel, "Забрать в инвентарь", new Vector2(0, -50), () => {
            if (winResultPanel != null) winResultPanel.SetActive(false);
        });

        winResultPanel.SetActive(false);
    }

    CaseItem GetRandomItemByChance()
    {
        float totalWeight = 0f;
        foreach (var item in casePool) totalWeight += item.dropChance;

        float rndVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var item in casePool)
        {
            currentSum += item.dropChance;
            if (rndVal <= currentSum) return item;
        }

        return casePool[0];
    }

    void CreateRouletteItemVisual(Transform parent, CaseItem caseItem)
    {
        GameObject item = new GameObject("Item_" + caseItem.name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        item.transform.SetParent(parent, false);

        LayoutElement le = item.GetComponent<LayoutElement>();
        le.minWidth = 80; le.minHeight = 80;
        le.preferredWidth = 80; le.preferredHeight = 80;

        Image mainImg = item.GetComponent<Image>();
        mainImg.color = caseItem.color;

        if (caseItem.name.EndsWith("-and-black"))
        {
            GameObject innerSquare = new GameObject("InnerSquare", typeof(RectTransform), typeof(Image));
            innerSquare.transform.SetParent(item.transform, false);

            RectTransform innerRt = innerSquare.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.5f, 0.5f);
            innerRt.anchorMax = new Vector2(0.5f, 0.5f);
            innerRt.pivot = new Vector2(0.5f, 0.5f);
            innerRt.anchoredPosition = Vector2.zero;
            innerRt.sizeDelta = new Vector2(35, 35);

            Image innerImg = innerSquare.GetComponent<Image>();
            innerImg.color = Color.black;
            innerImg.raycastTarget = false;
        }
    }

    IEnumerator SpinRouletteRoutine()
    {
        isSpinning = true;
        if (winResultPanel != null) winResultPanel.SetActive(false);

        foreach (Transform child in rouletteContentPanel) Destroy(child.gameObject);

        InventoryManager inv = Object.FindAnyObjectByType<InventoryManager>();
        if (inv == null) { isSpinning = false; yield break; }

        int totalItems = 40;
        int winningIndex = 28;

        CaseItem winningCaseItem = GetRandomItemByChance();
        List<CaseItem> generatedItems = new List<CaseItem>();

        for (int i = 0; i < totalItems; i++)
        {
            if (i == winningIndex) generatedItems.Add(winningCaseItem);
            else generatedItems.Add(GetRandomItemByChance());
        }

        for (int i = 0; i < totalItems; i++)
        {
            CreateRouletteItemVisual(rouletteContentPanel, generatedItems[i]);
        }

        Canvas.ForceUpdateCanvases();

        rouletteScrollRect.horizontalNormalizedPosition = 0f;
        float timer = 0f;
        float duration = 4.0f;

        float contentWidth = totalItems * 90f;
        float viewportWidth = rouletteScrollRect.viewport.rect.width;
        float maxScrollableWidth = Mathf.Max(0, contentWidth - viewportWidth);

        float targetContentX = (winningIndex * 90f) - (viewportWidth / 2f) + 40f;
        float targetNormalizedPos = Mathf.Clamp01(targetContentX / maxScrollableWidth);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            rouletteScrollRect.horizontalNormalizedPosition = Mathf.Lerp(0f, targetNormalizedPos, smoothT);
            yield return null;
        }

        rouletteScrollRect.horizontalNormalizedPosition = targetNormalizedPos;
        yield return new WaitForSecondsRealtime(0.5f);

        inv.AddItem(winningCaseItem.name);
        inv.EquipSkin(winningCaseItem.name);

        if (winResultPanel != null && winResultText != null)
        {
            winResultText.text = $"Выпало: <color=yellow><b>{winningCaseItem.name}</b></color>\nШанс: {winningCaseItem.dropChance}%";
            winResultPanel.SetActive(true);
        }

        isSpinning = false;
    }

    Color GetSkinColor(string skinName)
    {
        switch (skinName)
        {
            case "White-and-black":
            case "White": return Color.white;
            case "Black": return Color.black;
            case "Gray-and-black":
            case "Gray": return Color.gray;
            case "Red-and-black":
            case "Red": return Color.red;
            case "Pink-and-black":
            case "Pink": return new Color(1f, 0.41f, 0.71f);
            case "Green-and-black":
            case "Green": return Color.green;
            default: return Color.gray;
        }
    }

    GameObject CreatePanel(string name, Transform parent, bool transparent)
    {
        GameObject p = new GameObject(name, typeof(RectTransform), typeof(Image));
        p.transform.SetParent(parent, false);
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

        p.GetComponent<Image>().color = transparent ? new Color(0.08f, 0.08f, 0.12f, 0.65f) : new Color(0.14f, 0.14f, 0.18f, 1.0f);
        return p;
    }

    void CreateTitle(GameObject p, string txt, Vector2 pos, Color? col = null)
    {
        GameObject t = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(p.transform, false);
        t.GetComponent<RectTransform>().anchoredPosition = pos;
        t.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 50);
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = txt; tmp.fontSize = 26; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = col ?? Color.white;
        tmp.raycastTarget = false;
    }

    GameObject CreateBtn(GameObject p, string txt, Vector2 pos, System.Action<GameObject> act)
    {
        GameObject b = new GameObject("Btn_" + txt, typeof(RectTransform), typeof(Button), typeof(Image));
        b.transform.SetParent(p.transform, false);
        RectTransform rt = b.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(165, 30);
        rt.anchoredPosition = pos;

        b.GetComponent<Button>().onClick.AddListener(() => act(b));
        b.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.32f);

        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(b.transform, false);
        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = txt; tmp.fontSize = 13; tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return b;
    }

    GameObject CreateBtn(GameObject p, string txt, Vector2 pos, UnityEngine.Events.UnityAction act)
    {
        return CreateBtn(p, txt, pos, (GameObject btn) => act());
    }

    GameObject CreateCs16Btn(GameObject p, string txt, Vector2 pos, UnityEngine.Events.UnityAction act)
    {
        GameObject b = new GameObject("Cs16Btn_" + txt, typeof(RectTransform), typeof(Button), typeof(Image));
        b.transform.SetParent(p.transform, false);
        RectTransform rt = b.GetComponent<RectTransform>();

        rt.sizeDelta = new Vector2(180, 28);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;

        b.GetComponent<Button>().onClick.AddListener(act);
        b.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(b.transform, false);
        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = txt;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.margin = new Vector4(8, 0, 0, 0);
        tmp.color = new Color(0.85f, 0.85f, 0.85f);
        tmp.raycastTarget = false;

        return b;
    }

    void CreateTitleCS16(GameObject p, string txt, Vector2 pos)
    {
        GameObject t = new GameObject("TitleCS16", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(p.transform, false);

        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 40);

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = txt;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.9f, 0.75f, 0.2f);
        tmp.raycastTarget = false;
    }

    void CreateSlider(GameObject p, string label, Vector2 pos, float min, float max, float def, UnityEngine.Events.UnityAction<float> act)
    {
        GameObject c = new GameObject("Slider_" + label, typeof(RectTransform));
        c.transform.SetParent(p.transform, false);

        RectTransform crt = c.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = pos;
        crt.sizeDelta = new Vector2(165, 40);

        GameObject t = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(c.transform, false);
        t.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = label + ": " + def.ToString("F0");
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        GameObject sObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sObj.transform.SetParent(c.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(0, -10);
        sRt.sizeDelta = new Vector2(150, 14);
        Slider s = sObj.GetComponent<Slider>();

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sObj.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.25f);
        bgRt.anchorMax = new Vector2(1, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sObj.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f);
        faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.zero; fRt.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sObj.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.sizeDelta = new Vector2(-15, 0);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(12, 0);
        handle.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);

        s.fillRect = fRt;
        s.handleRect = hRt;
        s.targetGraphic = handle.GetComponent<Image>();
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min;
        s.maxValue = max;
        s.value = def;

        s.onValueChanged.AddListener((v) => {
            tmp.text = label + ": " + v.ToString("F0");
            act(v);
        });
    }
}