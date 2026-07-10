using System.Collections;
using UnityEngine;

public class BossAura : MonoBehaviour
{
    public float chargeTime = 15f;
    public float useDelay = 5f;

    public float auraRadius = 10f;
    public float auraDamage = 35f;
    public float pushForce = 12f;

    public float powerMultiplier = 1.5f;

    public ParticleSystem auraChargedEffect;
    public ParticleSystem auraExplosionEffect;

    BossChase bossChase;
    BossSword bossSword;

    float originalMoveSpeed;
    float originalSwordDamage;

    bool isCharged = false;
    bool buffActive = false;

    void Start()
    {
        bossChase = GetComponent<BossChase>();
        bossSword = GetComponentInChildren<BossSword>();

        if (bossChase != null)
            originalMoveSpeed = bossChase.moveSpeed;

        if (bossSword != null)
            originalSwordDamage = bossSword.damage;

        if (auraChargedEffect != null)
        {
            auraChargedEffect.Stop();
            auraChargedEffect.Clear();
        }

        if (auraExplosionEffect != null)
        {
            auraExplosionEffect.Stop();
            auraExplosionEffect.Clear();
        }

        StartCoroutine(AuraRoutine());
    }

    IEnumerator AuraRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(chargeTime);

            ChargeAura();

            yield return new WaitForSeconds(useDelay);

            if (IsPlayerNear())
            {
                UseAura();
            }
        }
    }

    void ChargeAura()
    {
        isCharged = true;
        ApplyBuff();

        if (auraChargedEffect != null)
        {
            auraChargedEffect.gameObject.SetActive(true);
            auraChargedEffect.Clear();
            auraChargedEffect.Play();
        }
    }

    bool IsPlayerNear()
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                transform.position,
                auraRadius
            );

        foreach (Collider col in colliders)
        {
            if (col.GetComponent<PlayerHealth>() != null)
            {
                return true;
            }
        }

        return false;
    }

    void UseAura()
    {
        if (!isCharged)
            return;

        if (auraChargedEffect != null)
        {
            auraChargedEffect.Stop();
            auraChargedEffect.Clear();
        }

        if (auraExplosionEffect != null)
        {
            auraExplosionEffect.gameObject.SetActive(true);
            auraExplosionEffect.Clear();
            auraExplosionEffect.Play();
        }

        Collider[] colliders =
            Physics.OverlapSphere(
                transform.position,
                auraRadius
            );

        foreach (Collider col in colliders)
        {
            PlayerHealth playerHealth =
                col.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                continue;

            playerHealth.TakeDamage(auraDamage);

            Rigidbody playerRb =
                col.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                Vector3 direction =
                    col.transform.position - transform.position;

                direction.y = 0.5f;
                direction.Normalize();

                playerRb.AddForce(
                    direction * pushForce,
                    ForceMode.Impulse
                );
            }
        }

        isCharged = false;
        RemoveBuff();
    }

    void ApplyBuff()
    {
        if (buffActive)
            return;

        buffActive = true;

        if (bossChase != null)
        {
            bossChase.moveSpeed =
                originalMoveSpeed * powerMultiplier;
        }

        if (bossSword != null)
        {
            bossSword.damage =
                originalSwordDamage * powerMultiplier;
        }
    }

    void RemoveBuff()
    {
        if (!buffActive)
            return;

        buffActive = false;

        if (bossChase != null)
        {
            bossChase.moveSpeed =
                originalMoveSpeed;
        }

        if (bossSword != null)
        {
            bossSword.damage =
                originalSwordDamage;
        }
    }
}