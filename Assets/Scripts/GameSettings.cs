using UnityEngine;

public static class GameSettings
{
    public static event System.Action<float> OnDifficultyChanged;

    const string DifficultyKey = "Difficulty";

    static float _difficultyMultiplier = -1f;

    public static float difficultyMultiplier
    {
        get
        {
            LoadDifficulty();
            return _difficultyMultiplier;
        }

        set
        {
            SetDifficulty(value);
        }
    }

    public static void LoadDifficulty()
    {
        if (_difficultyMultiplier >= 0f)
            return;

        _difficultyMultiplier =
            PlayerPrefs.GetFloat(
                DifficultyKey,
                1f
            );

        _difficultyMultiplier =
            Mathf.Clamp(
                _difficultyMultiplier,
                0.5f,
                2f
            );
    }

    public static void SetDifficulty(float value)
    {
        LoadDifficulty();

        float clampedValue =
            Mathf.Clamp(value, 0.5f, 2f);

        if (Mathf.Approximately(
            _difficultyMultiplier,
            clampedValue
        ))
        {
            return;
        }

        _difficultyMultiplier = clampedValue;

        PlayerPrefs.SetFloat(
            DifficultyKey,
            _difficultyMultiplier
        );

        PlayerPrefs.Save();

        OnDifficultyChanged?.Invoke(
            _difficultyMultiplier
        );
    }
}