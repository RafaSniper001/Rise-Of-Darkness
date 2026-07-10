using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextSceneButton : MonoBehaviour
{
    public string nextScene = "Scene 2";

    public int nextCheckpoint = 0;

    public ParticleSystem botonExplosion;
    public float sceneChangeDelay = 1f;

    bool activated = false;

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

        SavePermanentGame();

        if (botonExplosion != null)
        {
            botonExplosion.Play();
        }

        StartCoroutine(ChangeScene());
    }

    IEnumerator ChangeScene()
    {
        yield return new WaitForSeconds(sceneChangeDelay);

        SceneManager.LoadScene(nextScene);
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
            Debug.LogError("Não foi possível salvar o jogo.");
            return;
        }

        GameSaveManager.SaveGame(
            nextScene,
            nextCheckpoint,
            playerHealth.currentHealth,
            playerMovement.currentStamina,
            playerPower.currentPower,
            playerHealth.GetLivesRemaining()
        );
    }
}