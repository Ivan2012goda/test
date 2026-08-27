using UnityEngine;
using System.Collections;

public class WeaponGenerator : MonoBehaviour
{
    [Header("Точки удержания на сцене")]
    public Transform weaponHoldPoint;   // WeaponHoldPoint — нож
    public Transform weaponHoldPoint2;  // WeaponHoldPoint2 — Deagle

    [Header("Основное оружие")]
    public GameObject primaryWeaponObject; // Автомат/винтовка для слота 1
    public Animator primaryAnimator;
    public float primaryFireCooldown = 0.12f;
    private float nextPrimaryTime = 0f;

    [Header("Настройки стрельбы пистолета")]
    public float fireCooldown = 0.5f;
    public Animator gunAnimator;
    private float nextFireTime = 0f;
    private bool isGunAnimating = false;

    [Header("Настройки ближнего боя (Нож)")]
    public float knifeRange = 2.0f;
    public float knifeDamage = 50f;
    public float knifeCooldown = 0.6f;
    public Animator knifeAnimator;
    private float nextKnifeTime = 0f;

    private GameObject knifeInstance;
    private GameObject gunInstance;

    void Start()
    {
        FindWeaponsInScene();
        EquipGun();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsKnifeEquipped())
                TryAttackKnife();
            else if (IsGunEquipped())
                TryShootGun();
            else if (IsPrimaryEquipped())
                TryShootPrimary();
        }
    }

    void FindWeaponsInScene()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        if (weaponHoldPoint == null)
        {
            Transform found = cam.transform.Find("WeaponHoldPoint");
            if (found != null) weaponHoldPoint = found;
        }

        if (weaponHoldPoint2 == null)
        {
            Transform found = cam.transform.Find("WeaponHoldPoint2");
            if (found != null) weaponHoldPoint2 = found;
        }

        if (weaponHoldPoint != null && weaponHoldPoint.childCount > 0)
        {
            Transform exactKnife = weaponHoldPoint.Find("m9_bayonet");
            knifeInstance = exactKnife != null ? exactKnife.gameObject : weaponHoldPoint.GetChild(0).gameObject;
            DisableColliders(knifeInstance);

            if (knifeAnimator == null)
                knifeAnimator = knifeInstance.GetComponentInChildren<Animator>();
        }

        if (weaponHoldPoint2 != null && weaponHoldPoint2.childCount > 0)
        {
            Transform exactGun = weaponHoldPoint2.Find("Deagle");
            gunInstance = exactGun != null ? exactGun.gameObject : weaponHoldPoint2.GetChild(0).gameObject;
            DisableColliders(gunInstance);

            if (gunAnimator == null)
                gunAnimator = gunInstance.GetComponentInChildren<Animator>();
        }

        if (primaryWeaponObject != null)
        {
            DisableColliders(primaryWeaponObject);
            if (primaryAnimator == null)
                primaryAnimator = primaryWeaponObject.GetComponentInChildren<Animator>();
        }
    }

    void DisableColliders(GameObject obj)
    {
        if (obj == null) return;

        foreach (Collider col in obj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }

    public void UnequipAll()
    {
        if (primaryWeaponObject != null) primaryWeaponObject.SetActive(false);
        if (knifeInstance != null) knifeInstance.SetActive(false);
        if (gunInstance != null) gunInstance.SetActive(false);
    }

    public void EquipPrimary()
    {
        UnequipAll();
        if (primaryWeaponObject != null) primaryWeaponObject.SetActive(true);
    }

    public void EquipKnife()
    {
        UnequipAll();
        if (knifeInstance != null) knifeInstance.SetActive(true);
    }

    public void EquipGun()
    {
        UnequipAll();
        if (gunInstance != null) gunInstance.SetActive(true);
    }

    // --- ОСНОВНОЕ ОРУЖИЕ ---
    void TryShootPrimary()
    {
        if (!IsPrimaryEquipped()) return;
        if (Time.time < nextPrimaryTime) return;

        nextPrimaryTime = Time.time + primaryFireCooldown;

        if (primaryAnimator != null)
            primaryAnimator.SetTrigger("Shoot");

        FirePrimaryBullet();
    }

    void FirePrimaryBullet()
    {
        Debug.Log("[Weapon] Основное оружие произвело выстрел!");
    }

    // --- ЛОГИКА НОЖА ---
    void TryAttackKnife()
    {
        if (!IsKnifeEquipped()) return;
        if (Time.time < nextKnifeTime) return;

        nextKnifeTime = Time.time + knifeCooldown;

        if (knifeAnimator != null)
            knifeAnimator.SetTrigger("Attack");

        PerformKnifeHit();
    }

    void PerformKnifeHit()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, knifeRange))
        {
            Debug.Log($"[Knife] Нож попал по объекту: {hit.collider.name} на расстоянии {hit.distance:F2}м");
            hit.collider.SendMessage("TakeDamage", knifeDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // --- ПИСТОЛЕТ ---
    void TryShootGun()
    {
        if (!IsGunEquipped()) return;
        if (Time.time < nextFireTime || isGunAnimating) return;

        Shoot();
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireCooldown;

        if (gunAnimator != null)
            gunAnimator.SetTrigger("Shoot");

        FireBullet();
        StartCoroutine(LockGunShootingRoutine(0.4f));
    }

    IEnumerator LockGunShootingRoutine(float duration)
    {
        isGunAnimating = true;
        yield return new WaitForSeconds(duration);
        isGunAnimating = false;
    }

    void FireBullet()
    {
        Debug.Log("[Weapon] Пистолет произвел одиночный выстрел!");
    }

    // --- ПРОВЕРКИ АКТИВНОГО ОРУЖИЯ ---
    public bool IsPrimaryEquipped()
    {
        return primaryWeaponObject != null && primaryWeaponObject.activeSelf;
    }

    public bool IsKnifeEquipped()
    {
        if (knifeInstance != null && knifeInstance.activeSelf)
            return true;

        if (weaponHoldPoint != null && weaponHoldPoint.childCount > 0)
            return weaponHoldPoint.GetChild(0).gameObject.activeSelf;

        return false;
    }

    public bool IsGunEquipped()
    {
        if (gunInstance != null && gunInstance.activeSelf)
            return true;

        if (weaponHoldPoint2 != null && weaponHoldPoint2.childCount > 0)
            return weaponHoldPoint2.GetChild(0).gameObject.activeSelf;

        return false;
    }

    // --- СКИНЫ ---
    public void ApplySkinToActiveWeapon(string skinName)
    {
        if (string.IsNullOrEmpty(skinName)) skinName = "Default";

        string[] parts = skinName.ToLower().Split(new string[] { "-and-" }, System.StringSplitOptions.None);
        Color primaryColor;
        Color secondaryColor;
        bool isTwoTone = parts.Length > 1;

        if (isTwoTone)
        {
            primaryColor = ParseColor(parts[0]);
            secondaryColor = ParseColor(parts[1]);
        }
        else
        {
            primaryColor = GetPresetColor(skinName);
            secondaryColor = primaryColor;
        }

        if (knifeInstance != null)
            ApplyColorsToHierarchy(knifeInstance.transform, primaryColor, secondaryColor, isTwoTone, true);

        if (gunInstance != null)
            ApplyColorsToHierarchy(gunInstance.transform, primaryColor, secondaryColor, isTwoTone, false);

        if (primaryWeaponObject != null)
            ApplyColorsToHierarchy(primaryWeaponObject.transform, primaryColor, secondaryColor, isTwoTone, false);
    }

    private void ApplyColorsToHierarchy(Transform weaponTransform, Color c1, Color c2, bool isTwoTone, bool isKnife)
    {
        Renderer[] renderers = weaponTransform.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in renderers)
        {
            if (rend == null || rend.sharedMaterial == null) continue;

            Material mat = new Material(rend.sharedMaterial);
            rend.material = mat;

            string objName = rend.gameObject.name.ToLower();

            if (isKnife)
            {
                if (objName.Contains("blade"))
                    mat.color = c1;
                else if (objName.Contains("handle") || objName.Contains("ручка"))
                    mat.color = isTwoTone ? c2 : c1;
                else
                    mat.color = c1;
            }
            else
            {
                if (objName.Contains("верх") || objName.Contains("slide") || objName.Contains("barrel"))
                    mat.color = c1;
                else if (objName.Contains("ручка") || objName.Contains("grip") || objName.Contains("handle"))
                    mat.color = isTwoTone ? c2 : c1;
                else
                    mat.color = isTwoTone ? c2 : c1;
            }
        }
    }

    private Color GetPresetColor(string skinName)
    {
        switch (skinName)
        {
            case "Red Neon": return Color.red;
            case "Cyber Gold": return new Color(1f, 0.8f, 0f);
            case "Matrix Green": return Color.green;
            case "Void Black": return new Color(0.1f, 0.1f, 0.1f);
            default: return new Color(0.3f, 0.3f, 0.35f);
        }
    }

    private Color ParseColor(string colorStr)
    {
        switch (colorStr.Trim())
        {
            case "white": return Color.white;
            case "black": return Color.black;
            case "gray":
            case "grey": return Color.gray;
            case "red": return Color.red;
            case "blue": return Color.blue;
            case "green": return Color.green;
            case "yellow": return Color.yellow;
            case "cyan": return Color.cyan;
            case "magenta":
            case "pink": return new Color(1f, 0.41f, 0.71f);
            case "orange": return new Color(1f, 0.5f, 0f);
            case "purple": return new Color(0.5f, 0f, 0.5f);
            default:
                ColorUtility.TryParseHtmlString(colorStr, out Color parsedColor);
                return parsedColor != default ? parsedColor : Color.gray;
        }
    }

    public void GenerateAndEquipProceduralWeapon() => EquipGun();
    public void ForceEquipGun() => EquipGun();
    public void GenerateNormalWeapon() => EquipGun();
    public void UnequipWeapon() => UnequipAll();
    public GameObject GetActiveWeapon()
    {
        if (IsPrimaryEquipped()) return primaryWeaponObject;
        if (IsKnifeEquipped()) return knifeInstance;
        return gunInstance;
    }
}
