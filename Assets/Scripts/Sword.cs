using UnityEngine;

// controla a espada
public class Sword : MonoBehaviour
{
    // dano da espada
    public float damage;

    // sons do sabre
    public AudioClip[] attackSounds;

    // referência do animator
    Animator animator;

    // Audio Source do player
    AudioSource audioSource;

    // executa ao iniciar
    void Start()
    {
        // pega animator
        animator = GetComponent<Animator>();

        // pega Audio Source do player
        audioSource =
            GetComponentInParent<AudioSource>();
    }

    // executa ataque
    public void Attack()
    {
        // verifica se já está atacando
        if (
            animator
            .GetCurrentAnimatorStateInfo(0)
            .IsName("Ataque Sabre")
        )
        {
            // impede ataque duplicado
            return;
        }

        // toca som apenas se o ataque for válido
        if (
            audioSource != null &&
            attackSounds.Length > 0
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    attackSounds.Length
                );

            audioSource.PlayOneShot(
                attackSounds[randomIndex]
            );
        }

        // toca animação
        animator.CrossFade(
            "Ataque Sabre",
            0f,
            0,
            0f
        );
    }

    // detecta colisão da espada
    private void OnTriggerEnter(Collider collision)
    {
        // pega vida do inimigo
        EnemtStatus enemyStatus =
            collision.gameObject
            .GetComponent<EnemtStatus>();

        // verifica se acertou inimigo
        if (enemyStatus != null)
        {
            // aplica dano
            enemyStatus.GetHit(damage);

            // pega script Chase do inimigo
            Chase chase =
                collision.gameObject
                .GetComponent<Chase>();

            // verifica se possui Chase
            if (chase != null)
            {
                // empurra inimigo
                chase.PushAwayFrom(
                    transform.position
                );
            }
        }
    }
}