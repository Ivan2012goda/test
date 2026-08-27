using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerCheats))]
public class FirstPersonController : MonoBehaviour
{
    // Флаг паузы (чтобы в меню нельзя было играть, но софт не ломался)
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

    public void DeactivatePlayer()
    {
        isAlive = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    public void ActivatePlayer()
    {
        isAlive = true;
        isActivated = true;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
    }

    public void ActivatePlayerAt(Vector3 spawnPosition)
    {
        CharacterController cc = GetComponent<CharacterController>();

        // Отключаем CharacterController перед сменой позиции, чтобы Unity не блокировала телепорт
        if (cc != null) cc.enabled = false;

        transform.position = spawnPosition;

        if (cc != null) cc.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        isActivated = true;
        isAlive = true;
    }

    [Header("Точка спавна")]
    public Transform spawnPoint;

    [Header("Движение")]
    public float walkSpeed = 6.0f;
    public float gravity = -20.0f;
    public float jumpForce = 5.0f;

    [Header("Камера")]
    public Transform playerCamera;
    public float mouseSensitivity = 2.0f;
    private float cameraPitch = 0.0f;

    [Header("Оружие и Стрельба")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float shootForce = 60f;
    public float fireRate = 0.15f;
    private float nextFireTime = 0f;
    public int currentAmmo = 30;
    public int maxAmmo = 30;
    [HideInInspector] public float bulletDamage = 25f;

    [Header("Перезарядка")]
    public float reloadTime = 5.0f;
    private bool isReloading = false;
    private float nextReloadAllowedTime = 0f;

    [Header("Здоровье")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    public bool cheatGodMode => cheats != null && cheats.godMode;
    public bool cheatInfiniteAmmo => cheats != null && cheats.infiniteAmmo;
    public bool cheatRapidFire => cheats != null && cheats.rapidFire;
    public bool cheatBhop => cheats != null && cheats.bhopEnabled;

    private CharacterController controller;
    private PlayerCheats cheats;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isActivated = false;
    private bool isAlive = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cheats = GetComponent<PlayerCheats>();
        currentHealth = maxHealth;

        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 1. Определение точки спавна
        Vector3 targetSpawnPos = Vector3.zero;
        if (spawnPoint != null)
        {
            targetSpawnPos = spawnPoint.position;
        }
        else
        {
            GameObject foundSpawn = GameObject.FindWithTag("Respawn");
            if (foundSpawn != null)
            {
                targetSpawnPos = foundSpawn.transform.position;
            }
            else
            {
                targetSpawnPos = transform.position;
            }
        }

        // 2. Телепортируем игрока на спавн
        ActivatePlayerAt(targetSpawnPos);

        // 3. Уничтожаем всех ботов при старте карты
        KillAllBotsOnMap();
    }

    public void KillAllBotsOnMap()
    {
        GameObject[] bots = GameObject.FindGameObjectsWithTag("Bot");
        foreach (GameObject bot in bots)
        {
            Destroy(bot);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("Все боты зачищены!");
        }
    }

    void Update()
    {
        if (isGamePaused || !isActivated || !isAlive) return;

        HandleMouseLook();
        HandleMovement();
        HandleShooting();
        HandleReloading();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 moveInput = new Vector3(moveX, 0, moveZ);
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (cheatBhop && Input.GetKey(KeyCode.Space))
        {
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                if (isMoving && cheats != null)
                {
                    cheats.currentBhopSpeed = Mathf.Min(cheats.currentBhopSpeed * cheats.bhopAcceleration, cheats.maxBhopSpeed);
                }
            }
        }
        else
        {
            if (isGrounded && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            if (cheats != null) cheats.currentBhopSpeed = walkSpeed;
        }

        float currentBhopSpeed = cheats != null ? cheats.currentBhopSpeed : walkSpeed;
        float targetSpeed = isGrounded ? (cheatBhop && Input.GetKey(KeyCode.Space) ? currentBhopSpeed : walkSpeed) : currentBhopSpeed;

        Vector3 move = (transform.right * moveX + transform.forward * moveZ);
        if (!isMoving) targetSpeed = 0f;

        Vector3 targetVelocity = move * targetSpeed;

        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleShooting()
    {
        if (isReloading) return;

        // --- ПРОВЕРКА НА НОЖ (исправлено) ---
        KnifeSystem knife = GetComponent<KnifeSystem>();
        if (knife != null && knife.isKnifeEquipped)
        {
            return; // Если нож в руках, выходим и не даем стрелять
        }
        // ------------------------------------

        float currentFireRate = cheatRapidFire ? 0.005f : fireRate;

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0 || cheatInfiniteAmmo)
            {
                Shoot();
                nextFireTime = Time.time + currentFireRate;

                if (!cheatInfiniteAmmo) currentAmmo--;
            }
            else
            {
                if (Time.time >= nextReloadAllowedTime && !isReloading)
                {
                    StartCoroutine(ReloadRoutine());
                }
            }
        }
    }

    void HandleReloading()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            if (Time.time < nextReloadAllowedTime) return;
            if (currentAmmo < maxAmmo) StartCoroutine(ReloadRoutine());
        }
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        if (UIManager.Instance != null) UIManager.Instance.ShowNotification("Перезарядка...");
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        nextReloadAllowedTime = Time.time + 0.2f;
        if (UIManager.Instance != null) UIManager.Instance.ShowNotification("Готово!");
    }

    void Shoot()
    {
        Vector3 spawnPosition = firePoint != null ? firePoint.position : (playerCamera.position + playerCamera.forward * 0.6f);
        Vector3 targetPoint;
        Camera cam = playerCamera != null ? playerCamera.GetComponent<Camera>() : null;

        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 100f)) targetPoint = hit.point;
            else targetPoint = ray.GetPoint(100f);
        }
        else
        {
            targetPoint = spawnPosition + playerCamera.forward * 100f;
        }

        Vector3 shootDirection = (targetPoint - spawnPosition).normalized;

        GameObject bullet;

        if (bulletPrefab != null)
        {
            bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.LookRotation(shootDirection));
        }
        else
        {
            bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = spawnPosition;
            bullet.transform.rotation = Quaternion.LookRotation(shootDirection);
            bullet.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            Renderer rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                rend.material.color = Color.yellow;
            }

            Destroy(bullet, 3.0f);
        }

        bullet.tag = "Bullet";

        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol != null)
        {
            bulletCol.isTrigger = true;
            if (controller != null) Physics.IgnoreCollision(bulletCol, controller);
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null) rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = shootDirection * shootForce;
#else
        rb.velocity = shootDirection * shootForce;
#endif

        BulletHitHandler hitHandler = bullet.AddComponent<BulletHitHandler>();
        hitHandler.damage = bulletDamage;
    }

    public class BulletHitHandler : MonoBehaviour
    {
        [HideInInspector] public float damage = 25f;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("Bullet")) return;

            SimpleBot bot = other.GetComponent<SimpleBot>();
            if (bot != null)
            {
                bot.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }

    public void TakeDamage(float amount)
    {
        if (cheatGodMode) return;

        currentHealth -= amount;
        if (currentHealth <= 0 && isAlive)
        {
            StartCoroutine(DieAndRespawnRoutine());
        }
    }

    IEnumerator DieAndRespawnRoutine()
    {
        isAlive = false;
        DeactivatePlayer();

        if (UIManager.Instance != null) UIManager.Instance.ShowNotification("УБИТ! Возрождение через 3 сек...");

        yield return new WaitForSeconds(3.0f);

        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        isReloading = false;

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : new Vector3(Random.Range(-12f, 12f), 1f, Random.Range(-12f, 12f));

        ActivatePlayerAt(spawnPos);
        if (UIManager.Instance != null) UIManager.Instance.ShowNotification("Возродился в бою!");
    }
}
