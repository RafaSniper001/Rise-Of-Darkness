// importa a API principal da Unity
using UnityEngine;

// importa sistema de troca de cenas
using UnityEngine.SceneManagement;

// importa componentes de UI padrão
using UnityEngine.UI;

// importa suporte ao TextMesh Pro
using TMPro;

// classe principal do controlador da cena
public class InGameController : MonoBehaviour
{
    // referência do menu de pausa
    public GameObject pauseMenu;

    // referência do slider de dificuldade
    public Slider difficultySlider;

    // referência do texto de dificuldade
    public TMP_Text difficultyText;

    // componente responsável pelo input da câmera
    // aqui será arrastado o componente do Cinemachine
    // que controla movimentação da câmera pelo mouse
    public Behaviour cinemachineInput;

    // guarda se o jogo está pausado ou não
    bool isPaused = false;

    // executa automaticamente ao iniciar a cena
    void Start()
    {
        // garante que o jogo começa sem pausa
        Time.timeScale = 1f;

        // esconde o menu de pausa
        pauseMenu.SetActive(false);

        // define valor mínimo do slider
        difficultySlider.minValue = 0.5f;

        // define valor máximo do slider
        difficultySlider.maxValue = 2f;

        // coloca no slider o valor salvo globalmente
        difficultySlider.value =
            GameSettings.difficultyMultiplier;

        // atualiza texto da dificuldade ao iniciar
        UpdateDifficulty(difficultySlider.value);

        // adiciona evento para atualizar dificuldade
        // sempre que o slider mudar
        difficultySlider.onValueChanged.AddListener(
            UpdateDifficulty
        );

        // trava o cursor no centro da tela
        Cursor.lockState = CursorLockMode.Locked;

        // esconde o cursor
        Cursor.visible = false;

        // verifica se existe componente de input
        if (cinemachineInput != null)
        {
            // garante que o input da câmera inicia ativo
            cinemachineInput.enabled = true;
        }
    }

    // executa uma vez por frame
    void Update()
    {
        // verifica se apertou ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // alterna entre pausado e despausado
            TogglePause();
        }
    }

    // alterna o estado de pausa
    public void TogglePause()
    {
        // inverte o valor atual
        isPaused = !isPaused;

        // mostra ou esconde menu de pausa
        pauseMenu.SetActive(isPaused);

        // pausa ou continua o tempo do jogo
        Time.timeScale = isPaused ? 0f : 1f;

        // verifica se existe componente da câmera
        if (cinemachineInput != null)
        {
            // desativa input quando pausa
            // ativa novamente ao voltar
            cinemachineInput.enabled = !isPaused;
        }

        // verifica se o jogo está pausado
        if (isPaused)
        {
            // libera cursor da tela
            Cursor.lockState = CursorLockMode.None;

            // mostra cursor
            Cursor.visible = true;
        }
        else
        {
            // trava cursor novamente
            Cursor.lockState = CursorLockMode.Locked;

            // esconde cursor
            Cursor.visible = false;
        }
    }

    // atualiza dificuldade do jogo
    public void UpdateDifficulty(float value)
    {
        // salva valor globalmente
        GameSettings.difficultyMultiplier = value;

        // atualiza texto exibido na tela
        difficultyText.text =
            "DIFICULDADE: " +
            value.ToString("F1") +
            "x";
    }

    // volta para o menu principal
    public void MainMenu()
    {
        // garante que o jogo não fique pausado
        Time.timeScale = 1f;

        // libera cursor antes de trocar de cena
        Cursor.lockState = CursorLockMode.None;

        // mostra cursor
        Cursor.visible = true;

        // carrega cena de índice 0
        SceneManager.LoadScene(0);
    }
}