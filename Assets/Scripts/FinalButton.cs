using UnityEngine;
using UnityEngine.SceneManagement;

// controla botão final
public class FinalButton : MonoBehaviour
{
    // nome da próxima cena
    public string nextSceneName;

    // executa quando algo sai do trigger
    private void OnTriggerExit(Collider collision)
    {
        // verifica se colidiu com espada
        if (
            collision.GetComponentInParent<Sword>() != null
        )
        {
            // verifica se ainda existe inimigo
            if (
                GameObject.FindGameObjectWithTag("Enemy")
                == null
            )
            {
                // muda de cena
                SceneManager.LoadScene(
                    nextSceneName
                );
            }
        }
    }
}