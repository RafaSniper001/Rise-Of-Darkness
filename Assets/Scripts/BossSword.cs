using System.Collections;
using UnityEngine;

public class BossSword : MonoBehaviour
{
    public float damage = 20f;

    public AudioClip[] attackSounds;

    // duração em que o golpe pode causar dano
    public float damageWindow = 0.5f;

    AudioSource audioSource;

    bool canDamage = false;
    bool alreadyHit = false;

    void Start()
    {
        audioSource =
            GetComponentInParent<AudioSource>();
    }

    public void Attack()
    {
        if (audioSource != null &&
            attackSounds.Length > 0)
        {
            int randomIndex =
                Random.Range(0, attackSounds.Length);

            audioSource.PlayOneShot(
                attackSounds[randomIndex]
            );
        }

        StopAllCoroutines();
        StartCoroutine(DamageWindow());
    }

    IEnumerator DamageWindow()
    {
        canDamage = true;
        alreadyHit = false;

        yield return new WaitForSeconds(
            damageWindow
        );

        canDamage = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage || alreadyHit)
            return;

        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);

            alreadyHit = true;
        }
    }
}