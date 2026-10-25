using UnityEngine;

public class BossAttack : MonoBehaviour
{
    // distância necessária para atacar
    public float attackDistance = 2.5f;

    // tempo entre ataques
    public float attackCooldown = 1.5f;

    // nome exato da animação
    public string attackAnimationName = "Ataque Sabre";

    // referência do player
    public Transform player;

    // referência do sabre do boss
    public BossSword bossSword;

    // Animator responsável pela animação do sabre
    public Animator attackAnimator;

    // modelo animado: o golpe é a animação de ataque do mago (Animation Events)
    BossAnimator bossAnimator;
    BossChase bossChase;

    float nextAttackTime = 0f;

    void Start()
    {
        // procura o player automaticamente
        if (player == null)
        {
            GameObject foundPlayer =
                GameObject.FindGameObjectWithTag("Player");

            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        // procura BossSword automaticamente
        if (bossSword == null)
        {
            bossSword =
                GetComponentInChildren<BossSword>();
        }

        bossAnimator =
            GetComponentInChildren<BossAnimator>();

        bossChase = GetComponent<BossChase>();

        // procura Animator automaticamente nos filhos
        if (attackAnimator == null && bossAnimator == null)
        {
            attackAnimator =
                GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (player == null || bossSword == null)
            return;

        // estado Idle da IA (ainda não identificou o player): não ataca
        if (bossChase != null && !bossChase.IsEngaged)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distance <= attackDistance &&
            Time.time >= nextAttackTime)
        {
            Attack();

            nextAttackTime =
                Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        if (bossAnimator != null)
        {
            bossAnimator.RequestAttack();
            return;
        }

        // toca som do sabre
        bossSword.Attack();

        // executa animação
        if (attackAnimator != null)
        {
            attackAnimator.CrossFade(
                attackAnimationName,
                0f,
                0,
                0f
            );
        }
    }
}