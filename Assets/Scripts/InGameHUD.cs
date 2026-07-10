using UnityEngine;
using UnityEngine.UI;
using TMPro;

// controla o HUD dentro do jogo
public class InGameHUD : MonoBehaviour
{
    // vida do player
    public PlayerHealth playerHealth;

    // movimento/stamina do player
    public PlayerMovement playerMovement;

    // vida do inimigo/boss
    public EnemyHealth enemyHealth;

    // slider da vida do player
    public Slider healthSlider;

    // slider da stamina
    public Slider staminaSlider;

    // slider da vida do inimigo/boss
    public Slider enemyHealthSlider;

    // texto das vidas
    public TMP_Text livesText;

    void Start()
    {
        // configura barra de vida do player
        if (healthSlider != null &&
            playerHealth != null)
        {
            healthSlider.maxValue =
                playerHealth.maxHealth;

            healthSlider.value =
                playerHealth.currentHealth;
        }

        // configura barra de stamina
        if (staminaSlider != null &&
            playerMovement != null)
        {
            staminaSlider.maxValue =
                playerMovement.maxStamina;

            staminaSlider.value =
                playerMovement.currentStamina;
        }

        // configura barra de vida do inimigo
        if (enemyHealthSlider != null &&
            enemyHealth != null)
        {
            enemyHealthSlider.maxValue =
                enemyHealth.maxHealth;

            enemyHealthSlider.value =
                enemyHealth.currentHealth;
        }
    }

    void Update()
    {
        // atualiza vida do player
        if (healthSlider != null &&
            playerHealth != null)
        {
            healthSlider.maxValue =
                playerHealth.maxHealth;

            healthSlider.value =
                playerHealth.currentHealth;
        }

        // atualiza stamina
        if (staminaSlider != null &&
            playerMovement != null)
        {
            staminaSlider.maxValue =
                playerMovement.maxStamina;

            staminaSlider.value =
                playerMovement.currentStamina;
        }

        // atualiza vida do inimigo
        if (enemyHealthSlider != null &&
            enemyHealth != null)
        {
            enemyHealthSlider.maxValue =
                enemyHealth.maxHealth;

            enemyHealthSlider.value =
                enemyHealth.currentHealth;
        }

        // atualiza texto das vidas
        if (livesText != null &&
            playerHealth != null)
        {
            livesText.text =
                "Vidas: " +
                playerHealth.GetLivesRemaining();
        }
    }
}