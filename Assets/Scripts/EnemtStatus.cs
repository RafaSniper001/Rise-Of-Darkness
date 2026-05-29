using UnityEngine;

// controla vida do inimigo
public class EnemtStatus : MonoBehaviour
{
    // vida inicial do inimigo
    public float life = 10;

    // executa ao iniciar
    void Start()
    {
        // aumenta vida conforme dificuldade
        life *= GameSettings.difficultyMultiplier;
    }

    // executa quando o inimigo leva dano
    public void GetHit(float damage)
    {
        // reduz vida
        life -= damage;

        // verifica se morreu
        if (life <= 0)
        {
            // destrói inimigo
            Destroy(gameObject);
        }
    }
}