using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("🎯 Оружие в Main Camera")]
    public GameObject primaryObject;              // WeaponHoldPoint3/AK 47
    public GameObject gunObject;                  // WeaponHoldPoint2/Deagle
    public GameObject knifeObject;                // WeaponHoldPoint/m9_bayonet
    public GameObject secondaryPrimaryObject;     // WeaponHoldPoint4/M 16

    [Header("🎮 Клавиши")]
    public KeyCode primaryKey = KeyCode.Alpha1;
    public KeyCode gunKey = KeyCode.Alpha2;
    public KeyCode knifeKey = KeyCode.Alpha3;
    public KeyCode buyMenuKey = KeyCode.B;

    [Header("🛒 Выбор основного оружия")]
    public bool createBuyMenu = true;
    public string primaryWeaponName = "AK 47";
    public string secondaryPrimaryWeaponName = "M 16";
    public string gunWeaponName = "Deagle";
    public string knifeWeaponName = "m9_bayonet";

    [HideInInspector] public bool isKnifeEquipped;
    [HideInInspector] public int currentSlot = 2;

    // true = AK 47, false = M 16
    [SerializeField] private bool ak47Selected = true;

    private GameObject buyMenu;
    private Canvas menuCanvas;
    private TextMeshProUGUI selectedWeaponText;

    void Start()
    {
        FindWeaponsIfNeeded();
        CreateBuyMenu();
        EquipGun();
    }

    void Update()
    {
        if (Input.GetKeyDown(primaryKey)) EquipPrimary();
        if (Input.GetKeyDown(gunKey)) EquipGun();
        if (Input.GetKeyDown(knifeKey)) EquipKnife();
        if (Input.GetKeyDown(buyMenuKey)) ToggleBuyMenu();

        if (buyMenu != null && buyMenu.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FindWeaponsIfNeeded()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>(true);
        if (cam == null) return;

        Transform hold1 = cam.transform.Find("WeaponHoldPoint");
        Transform hold2 = cam.transform.Find("WeaponHoldPoint2");
        Transform hold3 = cam.transform.Find("WeaponHoldPoint3");
        Transform hold4 = cam.transform.Find("WeaponHoldPoint4");

        if (knifeObject == null && hold1 != null)
        {
            Transform t = hold1.Find("m9_bayonet");
            if (t != null) knifeObject = t.gameObject;
        }

        if (gunObject == null && hold2 != null)
        {
            Transform t = hold2.Find("Deagle");
            if (t != null) gunObject = t.gameObject;
        }

        if (primaryObject == null && hold3 != null)
        {
            Transform t = hold3.Find("AK 47");
            if (t != null) primaryObject = t.gameObject;
        }

        if (secondaryPrimaryObject == null && hold4 != null)
        {
            Transform t = hold4.Find("M 16");
            if (t != null) secondaryPrimaryObject = t.gameObject;
        }

        // Оставляем каждое оружие строго в своей WeaponHoldPoint.
        ResetWeaponRoot(primaryObject);
        ResetWeaponRoot(secondaryPrimaryObject);
        ResetWeaponRoot(gunObject);
        ResetWeaponRoot(knifeObject);

        HideAllWeapons();
    }

    void ResetWeaponRoot(GameObject weapon)
    {
        if (weapon == null) return;
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.localScale = Vector3.one;
    }

    // =========================================================
    // СЛОТЫ: 1 = AK/M16, 2 = Deagle, 3 = нож
    // =========================================================

    public void EquipPrimary()
    {
        currentSlot = 1;
        isKnifeEquipped = false;
        HideAllWeapons();

        GameObject selected = GetSelectedPrimaryObject();
        if (selected != null) selected.SetActive(true);

        CloseBuyMenu();
    }

    public void EquipGun()
    {
        currentSlot = 2;
        isKnifeEquipped = false;
        HideAllWeapons();
        if (gunObject != null) gunObject.SetActive(true);
        CloseBuyMenu();
    }

    public void EquipKnife()
    {
        currentSlot = 3;
        isKnifeEquipped = true;
        HideAllWeapons();
        if (knifeObject != null) knifeObject.SetActive(true);
        CloseBuyMenu();
    }

    public void HideAllWeapons()
    {
        if (primaryObject != null) primaryObject.SetActive(false);
        if (secondaryPrimaryObject != null) secondaryPrimaryObject.SetActive(false);
        if (gunObject != null) gunObject.SetActive(false);
        if (knifeObject != null) knifeObject.SetActive(false);
    }

    public void UnequipAll()
    {
        HideAllWeapons();
        isKnifeEquipped = false;
    }

    // =========================================================
    // ВЫБОР ОСНОВНОГО: AK 47 ИЛИ M 16
    // =========================================================

    public void SelectAK47()
    {
        ak47Selected = true;
        UpdateBuyMenuText();
        if (currentSlot == 1) EquipPrimary();
    }

    public void SelectM16()
    {
        ak47Selected = false;
        UpdateBuyMenuText();
        if (currentSlot == 1) EquipPrimary();
    }

    public bool IsAK47Selected() => ak47Selected;
    public bool IsM16Selected() => !ak47Selected;

    public GameObject GetSelectedPrimaryObject()
    {
        return ak47Selected ? primaryObject : secondaryPrimaryObject;
    }

    public string GetSelectedPrimaryName()
    {
        return ak47Selected ? primaryWeaponName : secondaryPrimaryWeaponName;
    }

    public GameObject GetActiveWeapon()
    {
        if (currentSlot == 1) return GetSelectedPrimaryObject();
        if (currentSlot == 2) return gunObject;
        if (currentSlot == 3) return knifeObject;
        return null;
    }

    public bool IsPrimaryEquipped()
    {
        GameObject selected = GetSelectedPrimaryObject();
        return currentSlot == 1 && selected != null && selected.activeSelf;
    }

    public bool IsGunEquipped()
    {
        return currentSlot == 2 && gunObject != null && gunObject.activeSelf;
    }

    public bool IsKnifeEquipped()
    {
        return currentSlot == 3 && knifeObject != null && knifeObject.activeSelf;
    }

    public bool IsM16Equipped()
    {
        return currentSlot == 1 && secondaryPrimaryObject != null && secondaryPrimaryObject.activeSelf;
    }

    // =========================================================
    // МЕНЮ НА B
    // =========================================================

    void CreateBuyMenu()
    {
        if (!createBuyMenu || buyMenu != null) return;

        GameObject canvasObject = new GameObject("WeaponBuyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        menuCanvas = canvasObject.GetComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024, 576);

        buyMenu = new GameObject("WeaponBuyMenu", typeof(RectTransform), typeof(Image));
        buyMenu.transform.SetParent(menuCanvas.transform, false);

        RectTransform menuRect = buyMenu.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(500, 360);
        menuRect.anchoredPosition = Vector2.zero;
        buyMenu.GetComponent<Image>().color = new Color(0.035f, 0.04f, 0.055f, 0.98f);

        CreateMenuTitle("ВЫБОР ОСНОВНОГО ОРУЖИЯ", new Vector2(0, 135));
        selectedWeaponText = CreateMenuInfo("Выбрано: " + GetSelectedPrimaryName(), new Vector2(0, 95));

        CreateWeaponButton("AK 47", new Vector2(0, 35), SelectAK47);
        CreateWeaponButton("M 16", new Vector2(0, -30), SelectM16);

        CreateMenuInfo("1 — основное    2 — Deagle    3 — нож", new Vector2(0, -100));
        CreateWeaponButton("ЗАКРЫТЬ", new Vector2(0, -145), CloseBuyMenu);

        buyMenu.SetActive(false);
    }

    void CreateMenuTitle(string text, Vector2 position)
    {
        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        title.transform.SetParent(buyMenu.transform, false);
        RectTransform rect = title.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450, 60);
        rect.anchoredPosition = position;

        TextMeshProUGUI tmp = title.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    TextMeshProUGUI CreateMenuInfo(string text, Vector2 position)
    {
        GameObject info = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
        info.transform.SetParent(buyMenu.transform, false);
        RectTransform rect = info.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450, 35);
        rect.anchoredPosition = position;

        TextMeshProUGUI tmp = info.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.75f, 0.75f, 0.78f);
        tmp.raycastTarget = false;
        return tmp;
    }

    void CreateWeaponButton(string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject button = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(buyMenu.transform, false);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 50);
        rect.anchoredPosition = position;
        button.GetComponent<Image>().color = new Color(0.13f, 0.15f, 0.2f, 1f);
        button.GetComponent<Button>().onClick.AddListener(action);

        GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(button.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    void UpdateBuyMenuText()
    {
        if (selectedWeaponText != null)
            selectedWeaponText.text = "Выбрано: " + GetSelectedPrimaryName();
    }

    public void ToggleBuyMenu()
    {
        if (buyMenu == null)
        {
            CreateBuyMenu();
            if (buyMenu == null) return;
        }

        bool open = !buyMenu.activeSelf;
        buyMenu.SetActive(open);

        if (open)
        {
            UpdateBuyMenuText();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void CloseBuyMenu()
    {
        if (buyMenu != null) buyMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
