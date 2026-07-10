using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public float savedHealth = -1f;
    public float savedStamina = -1f;
    public float savedPower = -1f;
    public int savedLives = -1;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SavePlayerState(
        float health,
        float stamina,
        int lives
    )
    {
        savedHealth = health;
        savedStamina = stamina;
        savedLives = lives;
    }

    public void SavePlayerPower(float power)
    {
        savedPower = power;
    }

    public bool HasSavedPlayerState()
    {
        return savedHealth >= 0f &&
               savedStamina >= 0f;
    }

    public bool HasSavedPlayerPower()
    {
        return savedPower >= 0f;
    }

    public bool HasSavedLives()
    {
        return savedLives >= 0;
    }

    public void ClearPlayerState()
    {
        savedHealth = -1f;
        savedStamina = -1f;
        savedPower = -1f;
        savedLives = -1;
    }

    public void LoadFromPlayerPrefs(
        float defaultHealth,
        float defaultStamina,
        float defaultPower,
        int defaultLives
    )
    {
        savedHealth =
            GameSaveManager.GetHealth(defaultHealth);

        savedStamina =
            GameSaveManager.GetStamina(defaultStamina);

        savedPower =
            GameSaveManager.GetPower(defaultPower);

        savedLives =
            GameSaveManager.GetLives(defaultLives);
    }
}