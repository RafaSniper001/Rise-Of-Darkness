using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    public Transform[] checkpoints;

    public int currentCheckpointIndex = 0;

    void Awake()
    {
        instance = this;

        if (GameSaveManager.ShouldLoadSavedGame &&
            GameSaveManager.HasSave())
        {
            currentCheckpointIndex =
                GameSaveManager.GetCheckpoint();

            if (currentCheckpointIndex < 0 ||
                currentCheckpointIndex >= checkpoints.Length)
            {
                currentCheckpointIndex = 0;
            }

            Debug.Log(
                "Checkpoint carregado: " +
                currentCheckpointIndex
            );
        }
    }

    public Vector3 GetCurrentCheckpointPosition()
    {
        return checkpoints[currentCheckpointIndex].position;
    }

    public void SetCheckpoint(int checkpointIndex)
    {
        if (checkpointIndex < 0 ||
            checkpointIndex >= checkpoints.Length)
        {
            Debug.LogError("Checkpoint inválido.");
            return;
        }

        currentCheckpointIndex = checkpointIndex;

        Debug.Log("Checkpoint salvo: " + checkpointIndex);
    }
}