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

    // tempo máximo esperando a animação de poder chegar na mão no alto
    public float releaseTimeout = 2f;

    BossChase bossChase;
    BossSword bossSword;
    BossAnimator bossAnimator;

    bool waitingRelease = false;

    float originalMoveSpeed;
    float originalSwordDamage;

    bool isCharged = false;
    bool buffActive = false;

    void Start()
    {
        bossChase = GetComponent<BossChase>();
        bossSword = GetComponentInChildren<BossSword>();
        bossAnimator = GetComponentInChildren<BossAnimator>();

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

            // só lança o poder depois de identificar o player (estado Idle da IA não ataca)
            if (IsPlayerNear() && (bossChase == null || bossChase.IsEngaged))
            {
                yield return StartCoroutine(CastAura());
            }
        }
    }

    // com animação: a explosão sai quando a mão chega no alto (Animation Event PowerRelease)
    IEnumerator CastAura()
    {
        if (bossAnimator == null)
        {
            UseAura();
            yield break;
        }

        // termina o golpe de sabre em andamento antes de lançar o poder
        float waited = 0f;

        while (bossAnimator.IsBusy() && waited < releaseTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (!bossAnimator.TryPlayPower())
        {
            UseAura();
            yield break;
        }

        waitingRelease = true;
        waited = 0f;

        while (waitingRelease && waited < releaseTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // a animação não chegou no evento: explode do mesmo jeito
        if (waitingRelease)
            ReleasePower();
    }

    // chamado pelo BossAnimator no evento PowerRelease
    public void ReleasePower()
    {
        if (!waitingRelease)
            return;

        waitingRelease = false;
        UseAura();
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