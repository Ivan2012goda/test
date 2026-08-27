using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("🎯 Объекты оружия со сцены")]
    public GameObject gunObject;   // Перетащи сюда Deagle из WeaponHoldPoint
    public GameObject knifeObject; // Перетащи сюда m9_bayonet из WeaponHoldPoint

    [Header("🎮 Клавиши переключения")]
    public KeyCode equipGunKey = KeyCode.Alpha1;
    public KeyCode equipKnifeKey = KeyCode.Alpha3;

    [HideInInspector] public bool isKnifeEquipped = false;

    void Start()
    {
        // При старте включаем пушку и скрываем нож
        EquipGun();
    }

    void Update()
    {
        if (Input.GetKeyDown(equipGunKey))
        {
            EquipGun();
        }

        if (Input.GetKeyDown(equipKnifeKey))
        {
            EquipKnife();
        }
    }

    public void EquipGun()
    {
        isKnifeEquipped = false;

        if (gunObject != null) gunObject.SetActive(true);
        if (knifeObject != null) knifeObject.SetActive(false);
    }

    public void EquipKnife()
    {
        isKnifeEquipped = true;

        if (gunObject != null) gunObject.SetActive(false);
        if (knifeObject != null) knifeObject.SetActive(true);
    }
}