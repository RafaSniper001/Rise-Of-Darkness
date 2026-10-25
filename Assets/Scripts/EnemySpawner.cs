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

    // raio livre em volta do ponto: nenhum objeto de cenário (Destructible) por perto
    public float clearRadius = 2f;

    Vector3 GetRandomPointOnTopOfBox(BoxCollider area)
    {
        Bounds bounds = area.bounds;
        Vector3 point = bounds.center;

        // sorteia de novo se cair em cima de um objeto de cenário
        for (int attempt = 0; attempt < 30; attempt++)
        {
            point = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (!NearProp(point))
                break;
        }

        return point;
    }

    bool NearProp(Vector3 point)
    {
        foreach (Collider col in Physics.OverlapCapsule(
                     point + Vector3.down * 3f,
                     point + Vector3.up * 3f,
                     clearRadius,
                     ~0,
                     QueryTriggerInteraction.Ignore))
        {
            if (col.GetComponentInParent<Destructible>() != null)
                return true;
        }

        return false;
    }
}