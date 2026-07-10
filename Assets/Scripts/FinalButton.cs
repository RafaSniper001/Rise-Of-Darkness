using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalButton : MonoBehaviour
{
    public GameObject door;
    public Animator doorAnimator;
    public string animationStateName;
    public float destroyDelay = 1.8f;

    public int nextCheckpoint;
    public int nextWave;

    public EnemySpawner enemySpawner;

    public ParticleSystem botonExplosion;

    bool activated = false;

    void Start()
    {
        if (GameSaveManager.HasSave())
        {
            string currentScene =
                SceneManager.GetActiveScene().name;

            string savedScene =
                GameSaveManager.GetSceneName(currentScene);

            int savedCheckpoint =
                GameSaveManager.GetCheckpoint();

            if (savedScene == currentScene &&
                savedCheckpoint >= nextCheckpoint)
            {
                activated = true;

                if (door != null)
                {
                    Destroy(door);
                }
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.GetComponentInParent<Sword>() == null)
            return;

        if (activated)
            return;

        if (GameObject.FindGameObjectWithTag("Enemy") != null)
        {
            Debug.Log("Ainda existem inimigos.");
            return;
        }

        activated = true;

        if (botonExplosion != null)
        {
            botonExplosion.Play();
        }

        StartCoroutine(OpenDoor());
    }

    IEnumerator OpenDoor()
    {
        CheckpointManager.instance.SetCheckpoint(nextCheckpoint);

        SavePermanentGame();

        if (enemySpawner != null)
        {
            enemySpawner.SpawnWave(nextWave);
        }

        if (doorAnimator != null)
        {
            doorAnimator.Play(animationStateName);
        }

        yield return new WaitForSeconds(destroyDelay);

        if (door != null)
        {
            Destroy(door);
        }
    }

    void SavePermanentGame()
    {
        PlayerHealth playerHealth =
            FindFirstObjectByType<PlayerHealth>();

        PlayerMovement playerMovement =
            FindFirstObjectByType<PlayerMovement>();

        PlayerPower playerPower =
            FindFirstObjectByType<PlayerPower>();

        if (playerHealth == null ||
            playerMovement == null ||
            playerPower == null)
        {
            Debug.LogError(
                "Não foi possível salvar o jogo."
            );

            return;
        }

        string currentScene =
            SceneManager.GetActiveScene().name;

        GameSaveManager.SaveGame(
            currentScene,
            nextCheckpoint,
            playerHealth.currentHealth,
            playerMovement.currentStamina,
            playerPower.currentPower,
            playerHealth.GetLivesRemaining()
        );
    }
}