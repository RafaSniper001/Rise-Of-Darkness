using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // mata player
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.KillPlayer();
            return;
        }

        // mata inimigo antigo
        EnemtStatus enemyStatus =
            other.GetComponent<EnemtStatus>();

        if (enemyStatus != null)
        {
            enemyStatus.GetHit(9999f);
            return;
        }

        // mata novo inimigo
        EnemyHealth enemyHealth =
            other.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(999999f);
        }
    }
}