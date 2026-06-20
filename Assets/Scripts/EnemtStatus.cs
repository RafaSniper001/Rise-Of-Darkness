using UnityEngine;

// controla vida do inimigo
public class EnemtStatus : MonoBehaviour
{
    // vida inicial do inimigo
    public float life = 10;

    // som de morte do inimigo
    public AudioClip deathSound;

    // Audio Source dos efeitos
    AudioSource audioSource;

    // executa ao iniciar
    void Start()
    {
        // aumenta vida conforme dificuldade
        life *= GameSettings.difficultyMultiplier;

        // pega o Audio Source do inimigo
        audioSource = GetComponent<AudioSource>();
    }

    // executa quando o inimigo leva dano
    public void GetHit(float damage)
    {
        // reduz vida
        life -= damage;

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