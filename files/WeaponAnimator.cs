using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponAnimator : MonoBehaviour
{
    [System.Serializable]
    public class WeaponAnimationSet
    {
        [Header("Название")]
        public string weaponName;

        [Header("Объекты")]
        public GameObject weaponObject;
        public Transform weaponHoldPoint;

        [Header("Анимации")]
        public AnimationClip equipAnimation;
        public AnimationClip inspectAnimation;
        public AnimationClip fireAnimation;

        [Header("Настройки осмотра")]
        public Vector3 inspectRotation = new Vector3(0f, -70f, 0f);

        [Header("Настройки")]
        public bool canInspect = true;
        public bool canFire = true;

        [HideInInspector]
        public Animation animation;

        [HideInInspector]
        public Quaternion defaultRotation;
    }

    [Header("🔫 ОРУЖИЕ")]

    [Tooltip("Добавь сюда M9, Deagle, AK 47, M16 и т.д.")]
    public List<WeaponAnimationSet> weapons = new List<WeaponAnimationSet>();

    [Header("🎮 Управление")]
    public KeyCode inspectKey = KeyCode.F;

    [Header("⚙️ Общие настройки")]
    public bool blockInputDuringAnimation = true;

    [Tooltip("Возвращать holder в исходную позицию после анимации")]
    public bool restoreHolderAfterAnimation = true;

    private WeaponAnimationSet currentWeapon;

    private Coroutine currentRoutine;

    private bool isBusy;

    void Start()
    {
        InitializeWeapons();

        // Ищем первое активное оружие
        FindCurrentWeapon();

        if (currentWeapon != null)
        {
            PlayEquipAnimation(currentWeapon);
        }
    }

    void Update()
    {
        FindCurrentWeapon();

        if (currentWeapon == null)
            return;

        // F = осмотр
        if (Input.GetKeyDown(inspectKey))
        {
            if (!currentWeapon.canInspect)
                return;

            if (isBusy && blockInputDuringAnimation)
                return;

            PlayInspect();
        }

        // ЛКМ = fireAnimation (сбивает текущую анимацию, например осмотр)
        if (Input.GetMouseButtonDown(0))
        {
            if (!currentWeapon.canFire)
                return;

            // Принудительно прерываем текущую анимацию (осмотр)
            StopCurrentAnimation();

            PlayFire();
        }
    }

    // =========================================================
    // ИНИЦИАЛИЗАЦИЯ
    // =========================================================

    void InitializeWeapons()
    {
        foreach (WeaponAnimationSet weapon in weapons)
        {
            if (weapon == null)
                continue;

            if (weapon.weaponObject == null)
            {
                Debug.LogWarning(
                    "[WeaponAnimator] У оружия '" +
                    weapon.weaponName +
                    "' не назначен Weapon Object."
                );

                continue;
            }

            // Если holder не указан — берём родителя оружия
            if (weapon.weaponHoldPoint == null)
            {
                weapon.weaponHoldPoint = weapon.weaponObject.transform.parent;
            }

            if (weapon.weaponHoldPoint != null)
            {
                weapon.defaultRotation =
                    weapon.weaponHoldPoint.localRotation;
            }

            weapon.animation =
                weapon.weaponObject.GetComponent<Animation>();

            if (weapon.animation == null)
            {
                weapon.animation =
                    weapon.weaponObject.AddComponent<Animation>();
            }

            RegisterClip(weapon, weapon.equipAnimation);
            RegisterClip(weapon, weapon.inspectAnimation);
            RegisterClip(weapon, weapon.fireAnimation);
        }
    }

    void RegisterClip(
        WeaponAnimationSet weapon,
        AnimationClip clip)
    {
        if (weapon == null ||
            clip == null ||
            weapon.animation == null)
            return;

        clip.legacy = true;

        if (weapon.animation.GetClip(clip.name) == null)
        {
            weapon.animation.AddClip(
                clip,
                clip.name
            );
        }
    }

    // =========================================================
    // ОПРЕДЕЛЕНИЕ ТЕКУЩЕГО ОРУЖИЯ
    // =========================================================

    void FindCurrentWeapon()
    {
        foreach (WeaponAnimationSet weapon in weapons)
        {
            if (weapon == null ||
                weapon.weaponObject == null)
                continue;

            if (weapon.weaponObject.activeInHierarchy)
            {
                if (currentWeapon != weapon)
                {
                    currentWeapon = weapon;

                    Debug.Log(
                        "[WeaponAnimator] Текущее оружие: " +
                        weapon.weaponName
                    );
                }

                return;
            }
        }

        currentWeapon = null;
    }

    // =========================================================
    // EQUIP
    // =========================================================

    public void PlayEquipAnimation()
    {
        if (currentWeapon == null)
            FindCurrentWeapon();

        if (currentWeapon == null)
            return;

        PlayEquipAnimation(currentWeapon);
    }

    void PlayEquipAnimation(
        WeaponAnimationSet weapon)
    {
        if (weapon == null ||
            weapon.equipAnimation == null)
            return;

        RegisterClip(
            weapon,
            weapon.equipAnimation
        );

        weapon.animation.Stop();

        weapon.animation.Play(
            weapon.equipAnimation.name
        );
    }

    // =========================================================
    // FIRE
    // =========================================================

    public void PlayFire()
    {
        if (currentWeapon == null)
            FindCurrentWeapon();

        if (currentWeapon == null)
            return;

        if (!currentWeapon.canFire)
            return;

        if (currentWeapon.fireAnimation == null)
            return;

        StartWeaponAnimation(
            currentWeapon,
            currentWeapon.fireAnimation
        );
    }

    // =========================================================
    // INSPECT
    // =========================================================

    public void PlayInspect()
    {
        if (currentWeapon == null)
            FindCurrentWeapon();

        if (currentWeapon == null)
            return;

        if (!currentWeapon.canInspect)
            return;

        if (currentWeapon.inspectAnimation == null)
            return;

        StartWeaponAnimation(
            currentWeapon,
            currentWeapon.inspectAnimation
        );
    }

    // =========================================================
    // ОСНОВНОЙ ЗАПУСК АНИМАЦИИ
    // =========================================================

    void StartWeaponAnimation(
        WeaponAnimationSet weapon,
        AnimationClip clip)
    {
        if (weapon == null ||
            clip == null)
            return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentRoutine =
            StartCoroutine(
                WeaponAnimationRoutine(
                    weapon,
                    clip
                )
            );
    }

    IEnumerator WeaponAnimationRoutine(
        WeaponAnimationSet weapon,
        AnimationClip clip)
    {
        isBusy = true;

        // Запоминаем текущую позицию и вращение
        Vector3 originalPosition = Vector3.zero;
        Quaternion originalRotation = Quaternion.identity;

        bool hasHolder = weapon.weaponHoldPoint != null;

        if (hasHolder)
        {
            originalPosition =
                weapon.weaponHoldPoint.localPosition;

            originalRotation =
                weapon.weaponHoldPoint.localRotation;
        }

        RegisterClip(
            weapon,
            clip
        );

        // Для inspect можно задать дополнительный
        // поворот holder'а
        if (clip == weapon.inspectAnimation &&
            weapon.weaponHoldPoint != null)
        {
            weapon.weaponHoldPoint.localEulerAngles =
                weapon.inspectRotation;
        }

        weapon.animation.Stop();

        weapon.animation.Play(
            clip.name
        );

        // Ждём реальную длину animation clip
        float duration = clip.length;

        if (duration <= 0f)
            duration = 0.1f;

        yield return new WaitForSeconds(duration);

        // Возвращаем holder
        if (restoreHolderAfterAnimation &&
            hasHolder)
        {
            weapon.weaponHoldPoint.localPosition =
                originalPosition;

            weapon.weaponHoldPoint.localRotation =
                originalRotation;
        }

        isBusy = false;
        currentRoutine = null;
    }

    // =========================================================
    // ПРИНУДИТЕЛЬНЫЙ ЗАПУСК
    // =========================================================

    public void PlayAnimationByWeapon(
        string weaponName,
        AnimationClip clip)
    {
        WeaponAnimationSet weapon =
            GetWeapon(weaponName);

        if (weapon == null)
        {
            Debug.LogWarning(
                "[WeaponAnimator] Оружие не найдено: " +
                weaponName
            );

            return;
        }

        if (clip == null)
            return;

        StartWeaponAnimation(
            weapon,
            clip
        );
    }

    // =========================================================
    // ПОЛУЧЕНИЕ ОРУЖИЯ
    // =========================================================

    public WeaponAnimationSet GetWeapon(
        string weaponName)
    {
        foreach (WeaponAnimationSet weapon in weapons)
        {
            if (weapon == null)
                continue;

            if (weapon.weaponName == weaponName)
                return weapon;
        }

        return null;
    }

    // =========================================================
    // ВЫЗОВ ПО НАЗВАНИЮ
    // =========================================================

    public void PlayFireForWeapon(
        string weaponName)
    {
        WeaponAnimationSet weapon =
            GetWeapon(weaponName);

        if (weapon == null)
            return;

        if (weapon.fireAnimation == null)
            return;

        StartWeaponAnimation(
            weapon,
            weapon.fireAnimation
        );
    }

    public void PlayInspectForWeapon(
        string weaponName)
    {
        WeaponAnimationSet weapon =
            GetWeapon(weaponName);

        if (weapon == null)
            return;

        if (weapon.inspectAnimation == null)
            return;

        StartWeaponAnimation(
            weapon,
            weapon.inspectAnimation
        );
    }

    // =========================================================
    // ОСТАНОВКА
    // =========================================================

    public void StopCurrentAnimation()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (currentWeapon != null &&
            currentWeapon.animation != null)
        {
            currentWeapon.animation.Stop();
        }

        if (currentWeapon != null &&
            currentWeapon.weaponHoldPoint != null)
        {
            currentWeapon.weaponHoldPoint.localRotation =
                currentWeapon.defaultRotation;
        }

        isBusy = false;
    }

    // =========================================================
    // ПРОВЕРКА
    // =========================================================

    public bool IsBusy()
    {
        return isBusy;
    }

    public string GetCurrentWeaponName()
    {
        if (currentWeapon == null)
            return "";

        return currentWeapon.weaponName;
    }
}