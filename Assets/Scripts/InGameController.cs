using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class InGameController : MonoBehaviour
{
    public GameObject pauseMenu;

    public Slider difficultySlider;

    public TMP_Text difficultyText;

    public Behaviour cinemachineInput;

    bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;

        AudioListener.pause = false;

        pauseMenu.SetActive(false);

        difficultySlider.minValue = 0.5f;
        difficultySlider.maxValue = 2f;

        difficultySlider.value =
            GameSettings.difficultyMultiplier;

        UpdateDifficulty(difficultySlider.value);

        difficultySlider.onValueChanged.AddListener(
            UpdateDifficulty
        );

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cinemachineInput != null)
        {
            cinemachineInput.enabled = true;
        }

        StartCoroutine(FinishSavedGameLoad());
    }

    IEnumerator FinishSavedGameLoad()
    {
        yield return null;

        GameSaveManager.ShouldLoadSavedGame = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pauseMenu.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        AudioListener.pause = isPaused;

        if (cinemachineInput != null)
        {
            cinemachineInput.enabled = !isPaused;
        }

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void UpdateDifficulty(float value)
    {
        GameSettings.SetDifficulty(value);

        difficultyText.text =
            "DIFICULDADE: " +
            value.ToString("F1") +
            "x";
    }

    // botão Sair do jogo do menu de pausa: fecha o jogo (no editor só registra)
    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Application.Quit();
        Debug.Log("Saiu do jogo");
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;

        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameSaveManager.ShouldLoadSavedGame = false;

        if (GameManager.instance != null)
        {
            GameManager.instance.ClearPlayerState();
        }

        SceneManager.LoadScene(0);
    }
}