using UnityEngine;

public static class GameSaveManager
{
    public static bool ShouldLoadSavedGame = false;

    const string HasSaveKey = "HasSave";

    const string SceneKey = "SceneName";
    const string CheckpointKey = "Checkpoint";
    const string HealthKey = "Health";
    const string StaminaKey = "Stamina";
    const string PowerKey = "Power";
    const string LivesKey = "Lives";

    public static void SaveGame(
        string sceneName,
        int checkpoint,
        float health,
        float stamina,
        float power,
        int lives
    )
    {
        PlayerPrefs.SetInt(HasSaveKey, 1);

        PlayerPrefs.SetString(SceneKey, sceneName);
        PlayerPrefs.SetInt(CheckpointKey, checkpoint);
        PlayerPrefs.SetFloat(HealthKey, health);
        PlayerPrefs.SetFloat(StaminaKey, stamina);
        PlayerPrefs.SetFloat(PowerKey, power);
        PlayerPrefs.SetInt(LivesKey, lives);

        PlayerPrefs.Save();

        Debug.Log(
            "Jogo salvo na cena: " +
            sceneName +
            " | checkpoint: " +
            checkpoint
        );
    }

    public static bool HasSave()
    {
        return PlayerPrefs.GetInt(HasSaveKey, 0) == 1;
    }

    public static string GetSceneName(string defaultScene)
    {
        return PlayerPrefs.GetString(SceneKey, defaultScene);
    }

    public static int GetCheckpoint()
    {
        return PlayerPrefs.GetInt(CheckpointKey, 0);
    }

    public static float GetHealth(float defaultValue)
    {
        return PlayerPrefs.GetFloat(HealthKey, defaultValue);
    }

    public static float GetStamina(float defaultValue)
    {
        return PlayerPrefs.GetFloat(StaminaKey, defaultValue);
    }

    public static float GetPower(float defaultValue)
    {
        return PlayerPrefs.GetFloat(PowerKey, defaultValue);
    }

    public static int GetLives(int defaultValue)
    {
        return PlayerPrefs.GetInt(LivesKey, defaultValue);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(HasSaveKey);

        PlayerPrefs.DeleteKey(SceneKey);
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.DeleteKey(HealthKey);
        PlayerPrefs.DeleteKey(StaminaKey);
        PlayerPrefs.DeleteKey(PowerKey);
        PlayerPrefs.DeleteKey(LivesKey);

        PlayerPrefs.Save();

        Debug.Log("Save apagado.");
    }
}