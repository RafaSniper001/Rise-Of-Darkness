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

    // slider da vida
    public Slider healthSlider;

    // slider da stamina
    public Slider staminaSlider;

    // texto das vidas
    public TMP_Text livesText;

    void Start()
    {
        // configura barra de vida
        if (healthSlider != null && playerHealth != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            healthSlider.value = playerHealth.currentHealth;
        }

        // configura barra de stamina
        if (staminaSlider != null && playerMovement != null)
        {
            staminaSlider.maxValue = playerMovement.maxStamina;
            staminaSlider.value = playerMovement.currentStamina;
        }
    }

    void Update()
    {
        // atualiza barra de vida
        if (healthSlider != null && playerHealth != null)
        {
            healthSlider.value = playerHealth.currentHealth;
        }

        // atualiza barra de stamina
        if (staminaSlider != null && playerMovement != null)
        {
            staminaSlider.value = playerMovement.currentStamina;
        }

        // atualiza texto das vidas
        if (livesText != null && playerHealth != null)
        {
            livesText.text = "Vidas: " + playerHealth.GetLivesRemaining();
        }
    }
}