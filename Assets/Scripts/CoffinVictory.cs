using UnityEngine;
using UnityEngine.SceneManagement;

// controla vitória ao encostar no caixão
public class CoffinVictory : MonoBehaviour
{
    // nome da cena de vitória
    public string victorySceneName = "VictoryScene";

    // executa quando algo entra no trigger
    void OnTriggerEnter(Collider other)
    {
        // verifica se é o player
        if (other.CompareTag("Player"))
        {
            // verifica se não existe nenhum inimigo
            if (GameObject.FindGameObjectWithTag("Enemy") == null)
            {
                // carrega tela de vitória
                SceneManager.LoadScene(victorySceneName);
            }
            else
            {
                Debug.Log("Ainda existe inimigo vivo.");
            }
        }
    }
}