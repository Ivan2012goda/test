using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerCheats))]
public class FirstPersonController : MonoBehaviour
{
    // =========================================================
    // ПАУЗА
    // =========================================================

    public bool isGamePaused = false;

    public void SetGamePaused(bool paused)
    {
        isGamePaused = paused;

        if (paused)
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

    // =========================================================
    // АКТИВАЦИЯ ИГРОКА
    // =========================================================

    public void DeactivatePlayer()
    {
        isAlive = false;

        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        Rigidbody rb =
            GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = true;
    }

    public void ActivatePlayer()
    {
        isAlive = true;
        isActivated = true;

        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = true;

        Rigidbody rb =
            GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = false;
    }

    public void ActivatePlayerAt(Vector3 spawnPosition)
    {
        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        transform.position = spawnPosition;

        if (cc != null)
            cc.enabled = true;

        Rigidbody rb =
            GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = false;

        velocity = Vector3.zero;

        isActivated = true;
        isAlive = true;
    }

    // =========================================================
    // ТОЧКА СПАВНА
    // =========================================================

    [Header("Точка спавна")]
    public Transform spawnPoint;

    // =========================================================
    // ДВИЖЕНИЕ
    // =========================================================

    [Header("Движение")]
    public float walkSpeed = 6.0f;
    public float gravity = -20.0f;
    public float jumpForce = 5.0f;

    // =========================================================
    // КАМЕРА
    // =========================================================

    [Header("Камера")]
    public Transform playerCamera;
    public float mouseSensitivity = 2.0f;

    private float cameraPitch = 0.0f;

    // =========================================================
    // ЗДОРОВЬЕ
    // =========================================================

    [Header("Здоровье")]
    public float maxHealth = 100f;

    [HideInInspector]
    public float currentHealth;

    // =========================================================
    // CHEATS
    // =========================================================

    public bool cheatGodMode =>
        cheats != null && cheats.godMode;

    public bool cheatInfiniteAmmo =>
        cheats != null && cheats.infiniteAmmo;

    public bool cheatRapidFire =>
        cheats != null && cheats.rapidFire;

    public bool cheatBhop =>
        cheats != null && cheats.bhopEnabled;

    // =========================================================
    // PRIVATE
    // =========================================================

    private CharacterController controller;
    private PlayerCheats cheats;

    private Vector3 velocity;

    private bool isGrounded;
    private bool isActivated = false;
    private bool isAlive = true;

    // Поле для совместимости с Property bulletDamage
    private float fallbackBulletDamage = 25f;

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        controller =
            GetComponent<CharacterController>();

        cheats =
            GetComponent<PlayerCheats>();

        currentHealth =
            maxHealth;

        if (playerCamera == null)
        {
            Camera cam =
                GetComponentInChildren<Camera>();

            if (cam != null)
                playerCamera = cam.transform;
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        Vector3 targetSpawnPos =
            Vector3.zero;

        if (spawnPoint != null)
        {
            targetSpawnPos =
                spawnPoint.position;
        }
        else
        {
            GameObject foundSpawn =
                GameObject.FindWithTag("Respawn");

            if (foundSpawn != null)
            {
                targetSpawnPos =
                    foundSpawn.transform.position;
            }
            else
            {
                targetSpawnPos =
                    transform.position;
            }
        }

        ActivatePlayerAt(
            targetSpawnPos
        );

        KillAllBotsOnMap();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (
            isGamePaused ||
            !isActivated ||
            !isAlive
        )
            return;

        HandleMouseLook();

        HandleMovement();
    }

    // =========================================================
    // КАМЕРА
    // =========================================================

    void HandleMouseLook()
    {
        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity;

        cameraPitch -= mouseY;

        cameraPitch =
            Mathf.Clamp(
                cameraPitch,
                -90f,
                90f
            );

        if (playerCamera != null)
        {
            playerCamera.localRotation =
                Quaternion.Euler(
                    cameraPitch,
                    0f,
                    0f
                );
        }

        transform.Rotate(
            Vector3.up * mouseX
        );
    }

    // =========================================================
    // ДВИЖЕНИЕ
    // =========================================================

    void HandleMovement()
    {
        if (controller == null)
            return;

        isGrounded =
            controller.isGrounded;

        if (
            isGrounded &&
            velocity.y < 0
        )
        {
            velocity.y = -2f;
        }

        float moveX =
            Input.GetAxis("Horizontal");

        float moveZ =
            Input.GetAxis("Vertical");

        Vector3 moveInput =
            new Vector3(
                moveX,
                0,
                moveZ
            );

        bool isMoving =
            moveInput.sqrMagnitude > 0.01f;

        if (
            cheatBhop &&
            Input.GetKey(KeyCode.Space)
        )
        {
            if (isGrounded)
            {
                velocity.y =
                    Mathf.Sqrt(
                        jumpForce *
                        -2f *
                        gravity
                    );

                if (
                    isMoving &&
                    cheats != null
                )
                {
                    cheats.currentBhopSpeed =
                        Mathf.Min(
                            cheats.currentBhopSpeed *
                            cheats.bhopAcceleration,

                            cheats.maxBhopSpeed
                        );
                }
            }
        }
        else
        {
            if (
                isGrounded &&
                (
                    Input.GetButtonDown("Jump") ||
                    Input.GetKeyDown(KeyCode.Space)
                )
            )
            {
                velocity.y =
                    Mathf.Sqrt(
                        jumpForce *
                        -2f *
                        gravity
                    );
            }

            if (cheats != null)
            {
                cheats.currentBhopSpeed =
                    walkSpeed;
            }
        }

        float currentBhopSpeed =
            cheats != null
                ? cheats.currentBhopSpeed
                : walkSpeed;

        float targetSpeed;

        if (isGrounded)
        {
            targetSpeed =
                cheatBhop &&
                Input.GetKey(KeyCode.Space)
                    ? currentBhopSpeed
                    : walkSpeed;
        }
        else
        {
            targetSpeed =
                currentBhopSpeed;
        }

        if (!isMoving)
            targetSpeed = 0f;

        Vector3 move =
            transform.right * moveX +
            transform.forward * moveZ;

        Vector3 targetVelocity =
            move * targetSpeed;

        velocity.x =
            targetVelocity.x;

        velocity.z =
            targetVelocity.z;

        velocity.y +=
            gravity *
            Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );
    }

    // =========================================================
    // УРОН
    // =========================================================

    public void TakeDamage(float amount)
    {
        if (cheatGodMode)
            return;

        currentHealth -= amount;

        if (
            currentHealth <= 0 &&
            isAlive
        )
        {
            StartCoroutine(
                DieAndRespawnRoutine()
            );
        }
    }

    // =========================================================
    // СМЕРТЬ / РЕСПАВН
    // =========================================================

    IEnumerator DieAndRespawnRoutine()
    {
        isAlive = false;

        DeactivatePlayer();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(
                "УБИТ! Возрождение через 3 сек..."
            );
        }

        yield return new WaitForSeconds(
            3.0f
        );

        currentHealth =
            maxHealth;

        WeaponGenerator weaponGenerator =
            GetComponent<WeaponGenerator>();

        if (weaponGenerator != null)
        {
            if (
                weaponGenerator.IsPrimaryEquipped() ||
                weaponGenerator.IsM16Equipped()
            )
            {
                weaponGenerator.primaryAmmo =
                    weaponGenerator.primaryMaxAmmo;
            }

            if (
                weaponGenerator.IsGunEquipped()
            )
            {
                weaponGenerator.pistolAmmo =
                    weaponGenerator.pistolMaxAmmo;
            }
        }

        Vector3 spawnPos;

        if (spawnPoint != null)
        {
            spawnPos =
                spawnPoint.position;
        }
        else
        {
            spawnPos =
                new Vector3(
                    Random.Range(-12f, 12f),
                    1f,
                    Random.Range(-12f, 12f)
                );
        }

        ActivatePlayerAt(
            spawnPos
        );

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(
                "Возродился в бою!"
            );
        }
    }

    // =========================================================
    // УДАЛЕНИЕ БОТОВ
    // =========================================================

    public void KillAllBotsOnMap()
    {
        GameObject[] bots =
            GameObject.FindGameObjectsWithTag(
                "Bot"
            );

        foreach (GameObject bot in bots)
        {
            if (bot != null)
                Destroy(bot);
        }

        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag(
                "Enemy"
            );

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(
                "Все боты зачищены!"
            );
        }
    }

    // =========================================================
    // СОВМЕСТИМОСТЬ СО СТАРЫМИ СКРИПТАМИ (АММУНИЦИЯ И УРОН)
    // =========================================================

    public int currentAmmo
    {
        get
        {
            WeaponGenerator wg = GetComponent<WeaponGenerator>();
            if (wg == null) return 0;
            if (wg.IsPrimaryEquipped() || wg.IsM16Equipped()) return wg.primaryAmmo;
            if (wg.IsGunEquipped()) return wg.pistolAmmo;
            return 0;
        }
        set
        {
            WeaponGenerator wg = GetComponent<WeaponGenerator>();
            if (wg != null)
            {
                if (wg.IsPrimaryEquipped() || wg.IsM16Equipped()) wg.primaryAmmo = value;
                else if (wg.IsGunEquipped()) wg.pistolAmmo = value;
            }
        }
    }

    public int maxAmmo
    {
        get
        {
            WeaponGenerator wg = GetComponent<WeaponGenerator>();
            if (wg == null) return 100;
            if (wg.IsPrimaryEquipped() || wg.IsM16Equipped()) return wg.primaryMaxAmmo;
            if (wg.IsGunEquipped()) return wg.pistolMaxAmmo;
            return 100;
        }
    }

    public float bulletDamage
    {
        get
        {
            return fallbackBulletDamage;
        }
        set
        {
            fallbackBulletDamage = value;
        }
    }

    public class BulletHitHandler : MonoBehaviour
    {
        [HideInInspector]
        public float damage = 25f;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) return;
            if (other.CompareTag("Bullet")) return;

            SimpleBot bot = other.GetComponent<SimpleBot>();
            if (bot != null)
            {
                bot.TakeDamage(damage);
            }
            else
            {
                other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }

            Destroy(gameObject);
        }
    }

    // =========================================================
    // ДОПОЛНИТЕЛЬНЫЕ PUBLIC МЕТОДЫ
    // =========================================================

    public bool IsAlive()
    {
        return isAlive;
    }

    public bool IsActivated()
    {
        return isActivated;
    }

    public CharacterController GetController()
    {
        return controller;
    }

    public Transform GetPlayerCamera()
    {
        return playerCamera;
    }
}