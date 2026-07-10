using UnityEngine;

// controla vida do inimigo
public class EnemtStatus : MonoBehaviour
{
    // vida inicial do inimigo
    public float life = 10f;

    // som de morte do inimigo
    public AudioClip deathSound;

    // Audio Source dos efeitos
    AudioSource audioSource;

    float baseLife;
    float currentLife;
    float previousMultiplier = 1f;

    void Awake()
    {
        baseLife = life;
        currentLife = life;
        previousMultiplier = 1f;
        ApplyDifficulty(GameSettings.difficultyMultiplier, true);

        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        GameSettings.OnDifficultyChanged += ApplyDifficulty;
    }

    void OnDisable()
    {
        GameSettings.OnDifficultyChanged -= ApplyDifficulty;
    }

    void Start()
    {
        ApplyDifficulty(GameSettings.difficultyMultiplier);
    }

    void ApplyDifficulty(float multiplier)
    {
        ApplyDifficulty(multiplier, false);
    }

    void ApplyDifficulty(float multiplier, bool initialSetup)
    {
        if (baseLife <= 0f)
        {
            baseLife = life;
        }

        if (initialSetup)
        {
            currentLife = baseLife * multiplier;
            life = Mathf.Max(0f, currentLife);
            previousMultiplier = multiplier;
            return;
        }

        float healthPercent = currentLife > 0f && previousMultiplier > 0f
            ? currentLife / (baseLife * previousMultiplier)
            : 1f;

        currentLife = baseLife * multiplier * healthPercent;
        life = Mathf.Max(0f, currentLife);
        previousMultiplier = multiplier;
    }

    // executa quando o inimigo leva dano
    public void GetHit(float damage)
    {
        // reduz vida
        life -= damage;
        currentLife = life;

        // verifica se morreu
        if (life <= 0)
        {
            Die();
        }
    }

    // executa a morte do inimigo
    void Die()
    {
        // toca som de morte
        if (
            audioSource != null &&
            deathSound != null
        )
        {
            audioSource.PlayOneShot(deathSound);
        }

        // desativa todos os scripts exceto este
        MonoBehaviour[] scripts =
            GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }

        // desativa colisões
        Collider[] colliders =
            GetComponents<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // esconde todos os renderizadores
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            rend.enabled = false;
        }

        // destrói após o som terminar
        if (deathSound != null)
        {
            Destroy(
                gameObject,
                deathSound.length
            );
        }
        else
        {
            Destroy(gameObject);
        }
    }
}