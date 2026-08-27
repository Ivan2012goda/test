using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("🎯 Объекты оружия со сцены")]
    public GameObject primaryObject; // Основное оружие: автомат/винтовка
    public GameObject gunObject;     // Пистолет: Deagle
    public GameObject knifeObject;   // Нож: m9_bayonet

    [Header("🎮 Клавиши переключения")]
    public KeyCode primaryKey = KeyCode.Alpha1;
    public KeyCode gunKey = KeyCode.Alpha2;
    public KeyCode knifeKey = KeyCode.Alpha3;
    public KeyCode buyMenuKey = KeyCode.B;

    [Header("🛒 Меню выбора оружия")]
    public bool createBuyMenu = true;
    public string primaryWeaponName = "Основное оружие";
    public string gunWeaponName = "Deagle";
    public string knifeWeaponName = "m9_bayonet";

    [HideInInspector] public bool isKnifeEquipped = false;
    [HideInInspector] public int currentSlot = 2;

    private GameObject buyMenu;
    private Canvas menuCanvas;

    void Start()
    {
        FindWeaponsIfNeeded();
        CreateBuyMenu();
        EquipGun();
    }

    void Update()
    {
        if (Input.GetKeyDown(primaryKey))
            EquipPrimary();

        if (Input.GetKeyDown(gunKey))
            EquipGun();

        if (Input.GetKeyDown(knifeKey))
            EquipKnife();

        if (Input.GetKeyDown(buyMenuKey))
            ToggleBuyMenu();

        if (buyMenu != null && buyMenu.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FindWeaponsIfNeeded()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) return;

        Transform holdPoint = cam.transform.Find("WeaponHoldPoint");
        Transform holdPoint2 = cam.transform.Find("WeaponHoldPoint2");

        if (knifeObject == null && holdPoint != null)
        {
            Transform knife = holdPoint.Find("m9_bayonet");
            if (knife != null) knifeObject = knife.gameObject;
            else if (holdPoint.childCount > 0) knifeObject = holdPoint.GetChild(0).gameObject;
        }

        if (gunObject == null && holdPoint2 != null)
        {
            Transform deagle = holdPoint2.Find("Deagle");
            if (deagle != null) gunObject = deagle.gameObject;
            else if (holdPoint2.childCount > 0) gunObject = holdPoint2.GetChild(0).gameObject;
        }
    }

    public void EquipPrimary()
    {
        currentSlot = 1;
        isKnifeEquipped = false;
        HideAllWeapons();
        if (primaryObject != null) primaryObject.SetActive(true);
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
        if (gunObject != null) gunObject.SetActive(false);
        if (knifeObject != null) knifeObject.SetActive(false);
    }

    public void UnequipAll()
    {
        HideAllWeapons();
        isKnifeEquipped = false;
    }

    public GameObject GetActiveWeapon()
    {
        if (currentSlot == 1) return primaryObject;
        if (currentSlot == 2) return gunObject;
        return knifeObject;
    }

    public bool IsPrimaryEquipped() => currentSlot == 1 && primaryObject != null && primaryObject.activeSelf;
    public bool IsGunEquipped() => currentSlot == 2 && gunObject != null && gunObject.activeSelf;
    public bool IsKnifeEquipped() => currentSlot == 3 && knifeObject != null && knifeObject.activeSelf;

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
        menuRect.sizeDelta = new Vector2(430, 300);
        menuRect.anchoredPosition = Vector2.zero;

        buyMenu.GetComponent<Image>().color = new Color(0.035f, 0.04f, 0.055f, 0.97f);

        CreateMenuTitle("ВЫБОР ОРУЖИЯ\nB — закрыть", new Vector2(0, 105));
        CreateWeaponButton("1   " + primaryWeaponName, new Vector2(0, 45), EquipPrimary);
        CreateWeaponButton("2   " + gunWeaponName, new Vector2(0, -20), EquipGun);
        CreateWeaponButton("3   " + knifeWeaponName, new Vector2(0, -85), EquipKnife);

        buyMenu.SetActive(false);
    }

    void CreateMenuTitle(string text, Vector2 position)
    {
        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        title.transform.SetParent(buyMenu.transform, false);

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 60);
        rect.anchoredPosition = position;

        TextMeshProUGUI tmp = title.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    void CreateWeaponButton(string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject button = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(buyMenu.transform, false);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 50);
        rect.anchoredPosition = position;

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.13f, 0.15f, 0.2f, 1f);

        Button btn = button.GetComponent<Button>();
        btn.onClick.AddListener(action);

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

    public void ToggleBuyMenu()
    {
        if (buyMenu == null)
        {
            CreateBuyMenu();
            return;
        }

        bool open = !buyMenu.activeSelf;
        buyMenu.SetActive(open);

        if (open)
        {
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
        if (buyMenu != null && buyMenu.activeSelf)
            buyMenu.SetActive(false);
    }
}
