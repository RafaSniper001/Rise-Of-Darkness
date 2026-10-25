using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float damage = 25f;

    public AudioClip[] attackSounds;

    // usado só se o player não tiver PlayerAnimator
    public float damageWindow = 0.5f;

    AudioSource audioSource;
    PlayerPower playerPower;
    PlayerAnimator playerAnimator;

    bool canDamage = false;

    // inimigos já atingidos neste golpe: cada um leva dano uma vez,
    // mas o golpe acerta todos em que o sabre encostar
    readonly HashSet<GameObject> hitThisSwing = new HashSet<GameObject>();

    void Start()
    {
        audioSource = GetComponentInParent<AudioSource>();
        playerPower = GetComponentInParent<PlayerPower>();

        PlayerAttack owner = GetComponentInParent<PlayerAttack>();
        if (owner != null)
            playerAnimator = owner.GetComponentInChildren<PlayerAnimator>();
    }

    public void Attack()
    {
        // som e dano vêm dos Animation Events (SwingSound, AttackStart/AttackEnd)
        if (playerAnimator != null)
        {
            playerAnimator.RequestAttack();
            return;
        }

        PlaySwingSound();

        StopAllCoroutines();
        StartCoroutine(DamageWindow());
    }

    public void PlaySwingSound()
    {
        if (audioSource != null && attackSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, attackSounds.Length);
            audioSource.PlayOneShot(attackSounds[randomIndex]);
        }
    }

    public void OpenDamageWindow()
    {
        canDamage = true;
        hitThisSwing.Clear();
    }

    public void CloseDamageWindow()
    {
        canDamage = false;
    }

    IEnumerator DamageWindow()
    {
        OpenDamageWindow();

        yield return new WaitForSeconds(damageWindow);

        CloseDamageWindow();
    }

    private void OnTriggerEnter(Collider collision)
    {
        TryHit(collision);
    }

    // o golpe pode começar com o sabre já encostado no inimigo
    private void OnTriggerStay(Collider collision)
    {
        TryHit(collision);
    }

    void TryHit(Collider collision)
    {
        if (!canDamage || hitThisSwing.Contains(collision.gameObject))
            return;

        // objetos de cenário quebram com o sabre (sem carregar o poder)
        Destructible destructible =
            collision.gameObject.GetComponent<Destructible>();

        if (destructible != null)
        {
            hitThisSwing.Add(collision.gameObject);
            destructible.TakeHit(damage);
            return;
        }

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
            hitThisSwing.Add(collision.gameObject);

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