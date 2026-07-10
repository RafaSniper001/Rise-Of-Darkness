using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class EnemyWave
{
    public GameObject enemyPrefab;
    public BoxCollider spawnArea;
    public int enemyCount;

    [HideInInspector]
    public bool alreadySpawned = false;
}

public class EnemySpawner : MonoBehaviour
{
    public EnemyWave[] waves;

    void Start()
    {
        int waveToSpawn = 0;

        if (GameSaveManager.HasSave())
        {
            string currentScene =
                SceneManager.GetActiveScene().name;

            string savedScene =
                GameSaveManager.GetSceneName(currentScene);

            if (savedScene == currentScene)
            {
                waveToSpawn =
                    GameSaveManager.GetCheckpoint();
            }
        }

        SpawnWave(waveToSpawn);
    }

    public void SpawnWave(int waveIndex)
    {
        if (waveIndex < 0 ||
            waveIndex >= waves.Length)
        {
            Debug.LogError("Fase inválida.");
            return;
        }

        EnemyWave wave = waves[waveIndex];

        if (wave.alreadySpawned)
        {
            Debug.Log("Essa fase já foi instanciada.");
            return;
        }

        wave.alreadySpawned = true;

        for (int i = 0; i < wave.enemyCount; i++)
        {
            Vector3 randomPoint =
                GetRandomPointOnTopOfBox(wave.spawnArea);

            Instantiate(
                wave.enemyPrefab,
                randomPoint,
                Quaternion.identity
            );
        }

        Debug.Log(
            "Fase " +
            (waveIndex + 1) +
            " instanciada."
        );
    }

    Vector3 GetRandomPointOnTopOfBox(BoxCollider area)
    {
        Bounds bounds = area.bounds;

        float randomX =
            Random.Range(bounds.min.x, bounds.max.x);

        float randomZ =
            Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(
            randomX,
            bounds.max.y,
            randomZ
        );
    }
}