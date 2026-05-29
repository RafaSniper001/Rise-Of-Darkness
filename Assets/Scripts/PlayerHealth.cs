using UnityEngine;
using UnityEngine.SceneManagement;

// controla vida do player
public class PlayerHealth : MonoBehaviour
{
    // vida máxima do player
    public float maxHealth = 100f;

    // vida atual do player
    public float currentHealth;

    // nome da cena para respawn
    // pode escolher no Inspector
    public string respawnScene;

    // nome da cena de derrota final
    public string defeatScene = "DefeatScene";

    // quantidade máxima de vidas
    public int maxLives = 3;

    // quantidade de mortes atuais
    // static mantém o valor entre cenas
    static int deathCount = 0;

    // executa ao iniciar
    void Start()
    {
        // começa com vida cheia
        currentHealth = maxHealth;
    }

    // recebe dano
    public void TakeDamage(float damage)
    {
        // diminui a vida atual
        currentHealth -= damage;

        // impede vida negativa
        if (currentHealth <= 0f)
        {
            // trava vida em zero
            currentHealth = 0f;

            // executa morte
            Die();
        }
    }

    // controla morte do player
    void Die()
    {
        // adiciona uma morte
        deathCount++;

        // verifica se acabou todas as vidas
        if (deathCount >= maxLives)
        {
            // reseta contador de mortes
            deathCount = 0;

            // vai para cena de derrota
            SceneManager.LoadScene(defeatScene);
        }
        else
        {
            // verifica se existe uma cena definida
            if (respawnScene != "")
            {
                // carrega cena escolhida
                SceneManager.LoadScene(respawnScene);
            }
            else
            {
                // reinicia cena atual
                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().buildIndex
                );
            }
        }
    }

    // retorna quantas vidas restam
    // usado pelo HUD
    public int GetLivesRemaining()
    {
        return maxLives - deathCount;
    }

    // reseta vidas manualmente
    // pode ser usado no menu ou vitória
    public static void ResetLives()
    {
        deathCount = 0;
    }
}