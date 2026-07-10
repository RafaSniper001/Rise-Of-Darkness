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

        // toca som de morte
        if (deathSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                deathSound
            );

            // espera o som terminar
            yield return new WaitForSeconds(
                deathSound.length
            );
        }

        // destrói o inimigo
        Destroy(gameObject);
    }
}