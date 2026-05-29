using UnityEngine;

// controla o projétil disparado pelo inimigo
public class EnemyBullet : MonoBehaviour
{
    // velocidade do projétil
    public float speed = 20f;

    // dano causado no player
    public float damage = 10f;

    // tempo máximo antes do tiro desaparecer sozinho
    public float lifeTime = 5f;

    // executa quando o objeto nasce
    void Start()
    {
        // multiplica o dano conforme a dificuldade atual
        damage *= GameSettings.difficultyMultiplier;

        // destrói o projétil após alguns segundos
        Destroy(gameObject, lifeTime);
    }

    // executa todo frame
    void Update()
    {
        // move o tiro para frente continuamente
        transform.position +=
            transform.forward *
            speed *
            Time.deltaTime;
    }

    // executa quando colide com algo
    void OnCollisionEnter(Collision collision)
    {
        // tenta pegar o script de vida do player
        PlayerHealth playerHealth =
            collision.gameObject.GetComponent<PlayerHealth>();

        // se encontrou o player
        if (playerHealth != null)
        {
            // aplica dano
            playerHealth.TakeDamage(damage);
        }

        // destrói o tiro ao colidir
        Destroy(gameObject);
    }
}