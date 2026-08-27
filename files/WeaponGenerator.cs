using UnityEngine;
using System.Collections;

public class WeaponGenerator : MonoBehaviour
{
    [Header("Точки удержания на сцене")]
    public Transform weaponHoldPoint;   // WeaponHoldPoint — m9_bayonet
    public Transform weaponHoldPoint2;  // WeaponHoldPoint2 — Deagle
    public Transform weaponHoldPoint3;  // WeaponHoldPoint3 — AK 47
    public Transform weaponHoldPoint4;  // WeaponHoldPoint4 — M 16

    [Header("Основное оружие")]
    public GameObject primaryWeaponObject; // AK 47
    public GameObject m16Object;           // M 16
    public Animator primaryAnimator;
    public float primaryFireCooldown = 0.12f;
    private float nextPrimaryTime;

    [Header("Настройки стрельбы пистолета")]
    public float fireCooldown = 0.5f;
    public Animator gunAnimator;
    private float nextFireTime;
    private bool isGunAnimating;

    [Header("Настройки ближнего боя (Нож)")]
    public float knifeRange = 2.0f;
    public float knifeDamage = 50f;
    public float knifeCooldown = 0.6f;
    public Animator knifeAnimator;
    private float nextKnifeTime;

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
            if (IsKnifeEquipped()) TryAttackKnife();
            else if (IsGunEquipped()) TryShootGun();
            else if (IsPrimaryEquipped()) TryShootPrimary();
        }
    }

    void FindWeaponsInScene()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>(true);
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        weaponHoldPoint = weaponHoldPoint != null ? weaponHoldPoint : cam.transform.Find("WeaponHoldPoint");
        weaponHoldPoint2 = weaponHoldPoint2 != null ? weaponHoldPoint2 : cam.transform.Find("WeaponHoldPoint2");
        weaponHoldPoint3 = weaponHoldPoint3 != null ? weaponHoldPoint3 : cam.transform.Find("WeaponHoldPoint3");
        weaponHoldPoint4 = weaponHoldPoint4 != null ? weaponHoldPoint4 : cam.transform.Find("WeaponHoldPoint4");

        if (knifeInstance == null && weaponHoldPoint != null)
        {
            Transform t = weaponHoldPoint.Find("m9_bayonet");
            if (t != null) knifeInstance = t.gameObject;
        }

        if (gunInstance == null && weaponHoldPoint2 != null)
        {
            Transform t = weaponHoldPoint2.Find("Deagle");
            if (t != null) gunInstance = t.gameObject;
        }

        if (primaryWeaponObject == null && weaponHoldPoint3 != null)
        {
            Transform t = weaponHoldPoint3.Find("AK 47");
            if (t != null) primaryWeaponObject = t.gameObject;
        }

        if (m16Object == null && weaponHoldPoint4 != null)
        {
            Transform t = weaponHoldPoint4.Find("M 16");
            if (t != null) m16Object = t.gameObject;
        }

        // Каждый root оружия должен находиться в своей точке удержания.
        // Это не мешает Animator менять localPosition во время анимации.
        ResetWeaponRoot(knifeInstance, weaponHoldPoint);
        ResetWeaponRoot(gunInstance, weaponHoldPoint2);
        ResetWeaponRoot(primaryWeaponObject, weaponHoldPoint3);
        ResetWeaponRoot(m16Object, weaponHoldPoint4);

        DisableColliders(knifeInstance);
        DisableColliders(gunInstance);
        DisableColliders(primaryWeaponObject);
        DisableColliders(m16Object);

        if (knifeAnimator == null && knifeInstance != null)
            knifeAnimator = knifeInstance.GetComponentInChildren<Animator>(true);
        if (gunAnimator == null && gunInstance != null)
            gunAnimator = gunInstance.GetComponentInChildren<Animator>(true);
        if (primaryAnimator == null && primaryWeaponObject != null)
            primaryAnimator = primaryWeaponObject.GetComponentInChildren<Animator>(true);
    }

    void ResetWeaponRoot(GameObject weapon, Transform holdPoint)
    {
        if (weapon == null || holdPoint == null) return;

        // Только корень. Внутренние детали модели не трогаем.
        Transform t = weapon.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
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
        if (m16Object != null) m16Object.SetActive(false);
        if (knifeInstance != null) knifeInstance.SetActive(false);
        if (gunInstance != null) gunInstance.SetActive(false);
    }

    public void EquipPrimary()
    {
        UnequipAll();
        if (primaryWeaponObject != null) primaryWeaponObject.SetActive(true);
    }

    public void EquipM16()
    {
        UnequipAll();
        if (m16Object != null) m16Object.SetActive(true);
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

    void TryShootPrimary()
    {
        if (!IsPrimaryEquipped() || Time.time < nextPrimaryTime) return;
        nextPrimaryTime = Time.time + primaryFireCooldown;

        if (primaryAnimator != null)
            primaryAnimator.SetTrigger("Shoot");

        Debug.Log("[Weapon] AK 47 произвел выстрел!");
    }

    void TryAttackKnife()
    {
        if (!IsKnifeEquipped() || Time.time < nextKnifeTime) return;
        nextKnifeTime = Time.time + knifeCooldown;

        if (knifeAnimator != null)
            knifeAnimator.SetTrigger("Attack");

        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>(true);
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, knifeRange))
        {
            Debug.Log($"[Knife] Попадание: {hit.collider.name}, {hit.distance:F2}м");
            hit.collider.SendMessage("TakeDamage", knifeDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    void TryShootGun()
    {
        if (!IsGunEquipped() || Time.time < nextFireTime || isGunAnimating) return;

        nextFireTime = Time.time + fireCooldown;
        if (gunAnimator != null)
            gunAnimator.SetTrigger("Shoot");

        Debug.Log("[Weapon] Deagle произвел выстрел!");
        StartCoroutine(LockGunShootingRoutine(0.4f));
    }

    IEnumerator LockGunShootingRoutine(float duration)
    {
        isGunAnimating = true;
        yield return new WaitForSeconds(duration);
        isGunAnimating = false;
    }

    public bool IsPrimaryEquipped() => primaryWeaponObject != null && primaryWeaponObject.activeSelf;
    public bool IsM16Equipped() => m16Object != null && m16Object.activeSelf;
    public bool IsKnifeEquipped() => knifeInstance != null && knifeInstance.activeSelf;
    public bool IsGunEquipped() => gunInstance != null && gunInstance.activeSelf;

    // =========================================================
    // СКИНЫ
    // =========================================================

    public void ApplySkinToActiveWeapon(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName)) skinName = "White";

        string normalized = skinName.Trim().ToLowerInvariant();
        string[] parts = normalized.Split(new[] { "-and-" }, System.StringSplitOptions.None);

        Color color1 = ParseColor(parts[0]);
        Color color2 = parts.Length > 1 ? ParseColor(parts[1]) : color1;
        bool twoTone = parts.Length > 1;

        // Скин применяется к каждому оружию отдельно.
        // Поэтому при переключении он не "склеивает" AK 47 и M 16.
        ApplySkinToWeapon(knifeInstance, color1, color2, twoTone, true);
        ApplySkinToWeapon(gunInstance, color1, color2, twoTone, false);
        ApplySkinToWeapon(primaryWeaponObject, color1, color2, twoTone, false);
        ApplySkinToWeapon(m16Object, color1, color2, twoTone, false);
    }

    private void ApplySkinToWeapon(GameObject weapon, Color c1, Color c2, bool twoTone, bool isKnife)
    {
        if (weapon == null) return;

        Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            // Не меняем sharedMaterial, иначе Unity изменит материал у всех копий модели.
            Material[] materials = rend.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                string objectName = rend.gameObject.name.ToLowerInvariant();
                Color targetColor = GetSkinPartColor(objectName, c1, c2, twoTone, isKnife);
                SetMaterialColor(mat, targetColor);
            }
            rend.materials = materials;
        }
    }

    private Color GetSkinPartColor(string objectName, Color c1, Color c2, bool twoTone, bool isKnife)
    {
        if (isKnife)
        {
            if (objectName.Contains("blade")) return c1;
            if (objectName.Contains("handle") || objectName.Contains("ручка")) return twoTone ? c2 : c1;
            return twoTone ? c2 : c1;
        }

        // Для Deagle: всё с названием "верх" = первый цвет,
        // всё с названием "ручка" = второй цвет.
        if (objectName.Contains("верх") || objectName.Contains("slide")) return c1;
        if (objectName.Contains("ручка") || objectName.Contains("grip") || objectName.Contains("handle"))
            return twoTone ? c2 : c1;

        return twoTone ? c2 : c1;
    }

    private void SetMaterialColor(Material mat, Color color)
    {
        // Built-in Standard
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        // URP Lit / большинство современных Unity-шейдеров
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
    }

    private Color ParseColor(string colorStr)
    {
        switch (colorStr.Trim().ToLowerInvariant())
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
            case "magenta": return Color.magenta;
            case "pink": return new Color(1f, 0.41f, 0.71f);
            case "orange": return new Color(1f, 0.5f, 0f);
            case "purple": return new Color(0.5f, 0f, 0.5f);
            default:
                if (ColorUtility.TryParseHtmlString(colorStr, out Color parsed)) return parsed;
                return Color.gray;
        }
    }

    public void GenerateAndEquipProceduralWeapon() => EquipPrimary();
    public void ForceEquipGun() => EquipGun();
    public void GenerateNormalWeapon() => EquipPrimary();
    public void UnequipWeapon() => UnequipAll();

    public GameObject GetActiveWeapon()
    {
        if (IsPrimaryEquipped()) return primaryWeaponObject;
        if (IsM16Equipped()) return m16Object;
        if (IsKnifeEquipped()) return knifeInstance;
        return gunInstance;
    }
}
