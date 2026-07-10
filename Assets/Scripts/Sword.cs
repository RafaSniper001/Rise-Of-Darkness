using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float damage = 25f;

    public AudioClip[] attackSounds;

    public float damageWindow = 0.5f;

    Animator animator;
    AudioSource audioSource;
    PlayerPower playerPower;

    bool canDamage = false;
    bool alreadyHit = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponentInParent<AudioSource>();
        playerPower = GetComponentInParent<PlayerPower>();
    }

    public void Attack()
    {
        if (animator != null &&
            animator.GetCurrentAnimatorStateInfo(0).IsName("Ataque Sabre"))
        {
            return;
        }

        if (audioSource != null && attackSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, attackSounds.Length);
            audioSource.PlayOneShot(attackSounds[randomIndex]);
        }

        if (animator != null)
        {
            animator.CrossFade("Ataque Sabre", 0f, 0, 0f);
        }

        StopAllCoroutines();
        StartCoroutine(DamageWindow());
    }

    IEnumerator DamageWindow()
    {
        canDamage = true;
        alreadyHit = false;

        yield return new WaitForSeconds(damageWindow);

        canDamage = false;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!canDamage || alreadyHit)
            return;

        bool hitEnemy = false;

        EnemtStatus enemyStatus =
            collision.gameObject.GetComponent<EnemtStatus>();

        if (enemyStatus != null)
        {
            enemyStatus.GetHit(damage);
            hitEnemy = true;
        }

        EnemyHealth enemyHealth =
            collision.gameObject.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            float finalDamage = damage;

            BossChase bossChase =
                collision.gameObject.GetComponent<BossChase>();

            if (bossChase != null)
            {
                finalDamage =
                    damage / GameSettings.difficultyMultiplier;
            }

            enemyHealth.TakeDamage(finalDamage);
            hitEnemy = true;
        }

        if (hitEnemy)
        {
            alreadyHit = true;

            if (playerPower != null)
            {
                playerPower.AddPower();
            }

            Chase chase =
                collision.gameObject.GetComponent<Chase>();

            if (chase != null)
            {
                chase.PushAwayFrom(transform.position);
            }
        }
    }
}