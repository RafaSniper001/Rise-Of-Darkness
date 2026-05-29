using UnityEngine;
using UnityEngine.SceneManagement;

// controla vitória, derrota e mortes entre cenas
public class GameManager : MonoBehaviour
{
    // instância global do GameManager
    public static GameManager instance;

    // nome da cena de vitória
    public string victorySceneName = "VictoryScene";

    // executa antes do Start
    void Awake()
    {
        // se já existe outro GameManager
        if (instance != null)
        {
            // destrói este para não duplicar
            Destroy(gameObject);
            return;
        }

        // define este como o GameManager principal
        instance = this;

        // não destrói ao trocar de cena
        DontDestroyOnLoad(gameObject);
    }

    // tenta vencer o jogo
    public void TryVictory()
    {
        // procura todos os inimigos vivos pela tag Enemy
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        // se não existe nenhum inimigo vivo
        if (enemies.Length == 0)
        {
            // carrega tela de vitória
            SceneManager.LoadScene(victorySceneName);
        }
        else
        {
            Debug.Log("Ainda existem inimigos vivos.");
        }
    }
}