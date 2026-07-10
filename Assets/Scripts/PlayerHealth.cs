using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public string defeatScene = "DefeatScene";
    public int maxLives = 3;

    public AudioClip deathSound;

    public float respawnLockTime = 0.3f;

    AudioSource audioSource;
    Rigidbody rb;

    bool isDead = false;
    bool isInvulnerable = false;

    static int deathCount = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        if (GameSaveManager.ShouldLoadSavedGame &&
            GameSaveManager.HasSave())
        {
            currentHealth =
                GameSaveManager.GetHealth(maxHealth);

            int livesRemaining =
                GameSaveManager.GetLives(maxLives);

            deathCount =
                maxLives - livesRemaining;

            deathCount =
                Mathf.Clamp(
                    deathCount,
                    0,
                    maxLives - 1
                );

            if (CheckpointManager.instance != null)
            {
                Vector3 checkpointPosition =
                    CheckpointManager.instance
                        .GetCurrentCheckpointPosition();

                if (rb != null)
                {
                    rb.position = checkpointPosition;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    transform.position = checkpointPosition;
                }
            }
        }
        else if (GameManager.instance != null &&
                 GameManager.instance.HasSavedPlayerState())
        {
            currentHealth =
                GameManager.instance.savedHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );
    }

    void LateUpdate()
    {
        SaveState();
    }

    void OnDestroy()
    {
        SaveState();
    }

    void SaveState()
    {
        if (GameManager.instance == null)
            return;

        if (isDead)
            return;

        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement == null)
            return;

        GameManager.instance.SavePlayerState(
            currentHealth,
            playerMovement.currentStamina,
            GetLivesRemaining()
        );
    }

    public void TakeDamage(float damage)
    {
        if (isDead || isInvulnerable)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            KillPlayer();
        }
    }

    public void KillPlayer()
    {
        if (isDead || isInvulnerable)
            return;

        currentHealth = 0f;
        isDead = true;
        isInvulnerable = true;

        StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        deathCount++;

        LockPlayer();

        if (deathSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
            yield return new WaitForSeconds(
                deathSound.length
            );
        }

        if (deathCount >= maxLives)
        {
            deathCount = 0;

            if (GameManager.instance != null)
            {
                GameManager.instance.ClearPlayerState();
            }

            SceneManager.LoadScene(defeatScene);
            yield break;
        }

        yield return StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        Vector3 checkpointPosition =
            CheckpointManager.instance
                .GetCurrentCheckpointPosition();

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        transform.position = checkpointPosition;

        currentHealth = maxHealth;

        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetStamina();
        }

        PlayerPower playerPower =
            GetComponent<PlayerPower>();

        if (playerPower != null)
        {
            playerPower.ResetPower();
        }

        SaveState();

        yield return new WaitForSeconds(
            respawnLockTime
        );

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        UnlockPlayer();

        isDead = false;
        isInvulnerable = false;
    }

    void LockPlayer()
    {
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script != this)
                script.enabled = false;
        }

        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    void UnlockPlayer()
    {
        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = true;
        }

        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script != this)
                script.enabled = true;
        }
    }

    public int GetLivesRemaining()
    {
        return maxLives - deathCount;
    }

    public static void ResetLives()
    {
        deathCount = 0;
    }
}