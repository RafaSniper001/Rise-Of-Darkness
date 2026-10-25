using System.Collections;
using UnityEngine;

public class BossSword : MonoBehaviour
{
    public float damage = 20f;

    public AudioClip[] attackSounds;

    // duração em que o golpe pode causar dano (só sem BossAnimator)
    public float damageWindow = 0.5f;

    AudioSource audioSource;

    bool canDamage = false;
    bool alreadyHit = false;

    void Start()
    {
        audioSource =
            GetComponentInParent<AudioSource>();
    }

    // sem BossAnimator: som e dano na hora do ataque
    // (com BossAnimator, vêm dos Animation Events SwingSound e AttackStart/AttackEnd)
    public void Attack()
    {
        PlaySwingSound();

        StopAllCoroutines();
        StartCoroutine(DamageWindow());
    }

    public void PlaySwingSound()
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
    }

    public void OpenDamageWindow()
    {
        canDamage = true;
        alreadyHit = false;
    }

    public void CloseDamageWindow()
    {
        canDamage = false;
    }

    IEnumerator DamageWindow()
    {
        OpenDamageWindow();

        yield return new WaitForSeconds(
            damageWindow
        );

        CloseDamageWindow();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    // o golpe pode começar com o sabre já encostado no player
    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    void TryHit(Collider other)
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
