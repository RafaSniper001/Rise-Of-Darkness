using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string gameScene = "Scene 1";

    public void StartGame()
    {
        GameSaveManager.ClearSave();
        GameSaveManager.ShouldLoadSavedGame = false;

        if (GameManager.instance != null)
        {
            GameManager.instance.ClearPlayerState();
        }

        PlayerHealth.ResetLives();

        SceneManager.LoadScene(gameScene);
    }

    public void ContinueGame()
    {
        if (!GameSaveManager.HasSave())
        {
            Debug.Log("Nenhum save encontrado.");
            return;
        }

        GameSaveManager.ShouldLoadSavedGame = true;

        if (GameManager.instance != null)
        {
            GameManager.instance.ClearPlayerState();
        }

        string savedScene =
            GameSaveManager.GetSceneName(gameScene);

        SceneManager.LoadScene(savedScene);
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Saiu do jogo");
    }
}