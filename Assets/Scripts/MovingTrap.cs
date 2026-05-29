using UnityEngine;

// controla armadilha que sobe e desce
public class MovingTrap : MonoBehaviour
{
    // velocidade do movimento
    public float moveSpeed = 2f;

    // altura máxima acima da posição inicial
    public float moveHeight = 4.5f;

    // dano causado ao player
    public float damage = 100f;

    // posição inicial do objeto
    Vector3 startPosition;

    // controla se está subindo
    bool goingUp = true;

    // executa ao iniciar
    void Start()
    {
        // salva posição inicial
        startPosition = transform.position;
    }

    // executa todo frame
    void Update()
    {
        // pega posição atual
        Vector3 position = transform.position;

        // verifica se está subindo
        if (goingUp)
        {
            // move para cima
            position.y += moveSpeed * Time.deltaTime;

            // verifica se chegou no topo
            if (position.y >= startPosition.y + moveHeight)
            {
                // trava exatamente no topo
                position.y = startPosition.y + moveHeight;

                // começa a descer
                goingUp = false;
            }
        }
        else
        {
            // move para baixo
            position.y -= moveSpeed * Time.deltaTime;

            // verifica se voltou posição inicial
            if (position.y <= startPosition.y)
            {
                // trava exatamente na posição inicial
                position.y = startPosition.y;

                // começa subir novamente
                goingUp = true;
            }
        }

        // aplica nova posição
        transform.position = position;
    }

    // executa ao colidir com algo
    void OnCollisionEnter(Collision collision)
    {
        // verifica se é o player
        if (collision.gameObject.CompareTag("Player"))
        {
            // pega script de vida do player
            PlayerHealth playerHealth =
                collision.gameObject.GetComponent<PlayerHealth>();

            // verifica se encontrou
            if (playerHealth != null)
            {
                // aplica dano
                playerHealth.TakeDamage(damage);
            }
        }
    }
}