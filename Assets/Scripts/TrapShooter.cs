using UnityEngine;

// controla disparos automáticos da armadilha
public class TrapShooter : MonoBehaviour
{
    // prefab da bala da armadilha
    public GameObject bulletPrefab;

    // ponto de onde a bala sai
    public Transform firePoint;

    // tempo entre disparos
    public float shootInterval = 1f;

    // dano extra da armadilha
    public float trapDamage = 30f;

    // velocidade da bala
    public float bulletSpeed = 25f;

    // contador interno de tempo
    float timer;

    // executa todo frame
    void Update()
    {
        // soma tempo passado
        timer += Time.deltaTime;

        // verifica se chegou hora de atirar
        if (timer >= shootInterval)
        {
            // dispara
            Shoot();

            // reseta contador
            timer = 0f;
        }
    }

    // cria e configura a bala
    void Shoot()
    {
        // instancia a bala na posição do firePoint
        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        // pega script da bala
        EnemyBullet enemyBullet =
            bullet.GetComponent<EnemyBullet>();

        // verifica se encontrou script
        if (enemyBullet != null)
        {
            // altera dano da bala
            enemyBullet.damage = trapDamage;

            // altera velocidade da bala
            enemyBullet.speed = bulletSpeed;
        }
    }
}