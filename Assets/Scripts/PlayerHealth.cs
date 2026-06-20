using System.Collections;
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
    public string respawnScene;

    // nome da cena de derrota final
    public string defeatScene = "DefeatScene";

    // quantidade máxima de vidas
    public int maxLives = 3;

    // som de morte
    public AudioClip deathSound;

    // componente de áudio
    private AudioSource audioSource;

    // impede executar a morte mais de uma vez
    private bool isDead = false;

    // quantidade de mortes atuais
    // static mantém o valor entre cenas
    static int deathCount = 0;

    // executa ao iniciar
    void Start()
    {
        // começa com vida cheia
        currentHealth = maxHealth;

        // pega o AudioSource do objeto
        audioSource = GetComponent<AudioSource>();
    }

    // recebe dano
    public void TakeDamage(float damage)
    {
        // ignora dano se já morreu
        if (isDead)
            return;

        // diminui a vida atual
        currentHealth -= damage;

        // impede vida negativa
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isDead = true;

            StartCoroutine(Die());
        }
    }

    // controla morte do player
    IEnumerator Die()
    {
        // desabilita todos os scripts do player
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
                script.enabled = false;
        }

        // para Rigidbody2D
        Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.simulated = false;
        }

        // para Rigidbody 3D
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // toca o som de morte
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);

            // espera terminar
            yield return new WaitForSeconds(deathSound.length);
        }

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
            if (!string.IsNullOrEmpty(respawnScene))
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