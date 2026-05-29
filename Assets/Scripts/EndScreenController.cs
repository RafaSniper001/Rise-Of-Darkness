using UnityEngine;
using UnityEngine.SceneManagement;

// controla botões da tela de vitória/derrota
public class EndScreenController : MonoBehaviour
{
    // nome da primeira fase ou cena para jogar novamente
    public string playAgainScene = "Scene 1";

    // nome da cena do menu principal
    public string mainMenuScene = "MainMenu";

    // executa ao abrir a tela
    void Start()
    {
        // mostra o cursor
        Cursor.visible = true;

        // libera o cursor para clicar nos botões
        Cursor.lockState = CursorLockMode.None;

        // garante que o jogo não fique pausado
        Time.timeScale = 1f;
    }

    // botão Jogar Novamente
    public void PlayAgain()
    {
        // garante tempo normal
        Time.timeScale = 1f;

        // restaura as 3 vidas
        PlayerHealth.ResetLives();

        // carrega a primeira fase
        SceneManager.LoadScene(playAgainScene);
    }

    // botão Menu Principal
    public void MainMenu()
    {
        // garante tempo normal
        Time.timeScale = 1f;

        // restaura as 3 vidas
        PlayerHealth.ResetLives();

        // volta para o menu principal
        SceneManager.LoadScene(mainMenuScene);
    }
}