using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPower : MonoBehaviour
{
    public float currentPower = 0f;
    public float maxPower = 100f;
    public float powerPerHit = 10f;

    public float powerRadius = 10f;
    public float powerDamage = 35f;

    public float bossDamageMultiplier = 2.5f;

    public float auraPushDistance = 12f;
    public float auraPushDuration = 0.2f;

    public float bossPushMultiplier = 2.5f;

    public float powerMultiplier = 1.5f;

    public ParticleSystem powerAura;
    public ParticleSystem powerExplosion;

    bool isCharged = false;
    bool buffActive = false;

    PlayerHealth playerHealth;
    PlayerMovement playerMovement;
    Sword sword;

    float originalMaxHealth;
    float originalMoveSpeed;
    float originalSprintSpeed;
    float originalStaminaRecovery;
    float originalSwordDamage;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        sword = GetComponentInChildren<Sword>();

        if (playerHealth != null)
            originalMaxHealth = playerHealth.maxHealth;

        if (playerMovement != null)
        {
            originalMoveSpeed = playerMovement.moveSpeed;
            originalSprintSpeed = playerMovement.sprintSpeed;
            originalStaminaRecovery = playerMovement.staminaRecovery;
        }

        if (sword != null)
            originalSwordDamage = sword.damage;

        currentPower = 0f;

        if (GameSaveManager.HasSave())
        {
            string currentScene =
                SceneManager.GetActiveScene().name;

            string savedScene =
                GameSaveManager.GetSceneName(currentScene);

            if (savedScene == currentScene)
            {
                currentPower =
                    GameSaveManager.GetPower(0f);
            }
        }
        else if (GameManager.instance != null &&
                 GameManager.instance.HasSavedPlayerPower())
        {
            currentPower =
                GameManager.instance.savedPower;
        }

        currentPower =
            Mathf.Clamp(currentPower, 0f, maxPower);

        UpdateAuraVisualAndBuff();
    }

    void Update()
    {
        SavePowerState();

        if (isCharged &&
            Input.GetKeyDown(KeyCode.Space))
        {
            ActivatePower();
        }
    }

    void OnDestroy()
    {
        SavePowerState();
    }

    void SavePowerState()
    {
        if (GameManager.instance == null)
            return;

        GameManager.instance.SavePlayerPower(
            currentPower
        );
    }

    public void AddPower()
    {
        if (isCharged)
            return;

        currentPower += powerPerHit;

        if (currentPower >= maxPower)
            currentPower = maxPower;

        UpdateAuraVisualAndBuff();
        SavePowerState();
    }

    void UpdateAuraVisualAndBuff()
    {
        if (currentPower >= maxPower)
        {
            currentPower = maxPower;
            isCharged = true;

            ApplyBuff();

            if (powerAura != null)
            {
                powerAura.gameObject.SetActive(true);
                powerAura.Clear();
                powerAura.Play();
            }
        }
        else
        {
            isCharged = false;
            RemoveBuff();

            if (powerAura != null)
            {
                powerAura.Stop();
                powerAura.Clear();
            }
        }
    }

    void ApplyBuff()
    {
        if (buffActive)
            return;

        buffActive = true;

        if (playerHealth != null)
        {
            playerHealth.maxHealth =
                originalMaxHealth * powerMultiplier;

            playerHealth.currentHealth *=
                powerMultiplier;
        }

        if (playerMovement != null)
        {
            playerMovement.moveSpeed =
                originalMoveSpeed * powerMultiplier;

            playerMovement.sprintSpeed =
                originalSprintSpeed * powerMultiplier;

            playerMovement.staminaRecovery =
                originalStaminaRecovery * powerMultiplier;
        }

        if (sword != null)
        {
            sword.damage =
                originalSwordDamage * powerMultiplier;
        }
    }

    void RemoveBuff()
    {
        if (!buffActive)
            return;

        buffActive = false;

        if (playerHealth != null)
        {
            playerHealth.maxHealth = originalMaxHealth;

            if (playerHealth.currentHealth >
                playerHealth.maxHealth)
            {
                playerHealth.currentHealth =
                    playerHealth.maxHealth;
            }
        }

        if (playerMovement != null)
        {
            playerMovement.moveSpeed =
                originalMoveSpeed;

            playerMovement.sprintSpeed =
                originalSprintSpeed;

            playerMovement.staminaRecovery =
                originalStaminaRecovery;
        }

        if (sword != null)
            sword.damage = originalSwordDamage;
    }

    void ActivatePower()
    {
        if (powerExplosion != null)
        {
            powerExplosion.gameObject.SetActive(true);
            powerExplosion.Clear();
            powerExplosion.Play();
        }

        Collider[] colliders =
            Physics.OverlapSphere(
                transform.position,
                powerRadius
            );

        foreach (Collider col in colliders)
        {
            if (!col.CompareTag("Enemy"))
                continue;

            BossChase bossChase =
                col.GetComponent<BossChase>();

            EnemtStatus enemyStatus =
                col.GetComponent<EnemtStatus>();

            if (enemyStatus != null)
                enemyStatus.GetHit(powerDamage);

            EnemyHealth enemyHealth =
                col.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                float damageToApply = powerDamage;

                if (bossChase != null)
                    damageToApply *= bossDamageMultiplier;

                enemyHealth.TakeDamage(damageToApply);
            }

            Chase chase =
                col.GetComponent<Chase>();

            if (chase != null)
            {
                chase.PushAwayFrom(
                    transform.position,
                    auraPushDistance,
                    auraPushDuration
                );
            }

            if (bossChase != null)
            {
                bossChase.PushAwayFrom(
                    transform.position,
                    auraPushDistance * bossPushMultiplier,
                    auraPushDuration
                );
            }
        }

        ResetPower();
    }

    public void ResetPower()
    {
        currentPower = 0f;
        isCharged = false;

        RemoveBuff();

        if (powerAura != null)
        {
            powerAura.Stop();
            powerAura.Clear();
        }

        SavePowerState();
    }
}