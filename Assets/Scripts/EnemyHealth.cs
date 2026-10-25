using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 300f;
    public float currentHealth;

    // som de morte
    public AudioClip deathSound;

    AudioSource audioSource;

    bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        audioSource =
            GetComponent<AudioSource>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            StartCoroutine(Die());
        }
    }

    IEnumerator Die()
    {
        isDead = true;

        float wait = 0f;

        // modelo animado: para de agir e toca a animação de morte
        BossAnimator bossAnimator =
            GetComponentInChildren<BossAnimator>();

        if (bossAnimator != null)
        {
            StopActing();
            bossAnimator.SetDead();
            wait = bossAnimator.deathDuration;
        }

        // toca som de morte
        if (deathSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                deathSound
            );

            wait = Mathf.Max(wait, deathSound.length);
        }

        // espera o som e a animação de morte terminarem
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }

        // destrói o inimigo
        Destroy(gameObject);
    }

    // desliga perseguição, ataque e aura enquanto a animação de morte toca
    void StopActing()
    {
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script == this)
                continue;

            script.StopAllCoroutines();
            script.enabled = false;
        }
    }
}