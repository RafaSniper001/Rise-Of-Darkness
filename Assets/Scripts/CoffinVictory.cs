using UnityEngine;
using UnityEngine.SceneManagement;

// controla vitória ao encostar na nave
public class ShipVictory : MonoBehaviour
{
    public string victorySceneName = "VictoryScene";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameObject.FindGameObjectWithTag("Enemy") == null)
        {
            SceneManager.LoadScene(victorySceneName);
        }
        else
        {
            Debug.Log("Ainda existe inimigo vivo.");
        }
    }
}