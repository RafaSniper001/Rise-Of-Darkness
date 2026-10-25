using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// boss da cena 2 andando pelo NavMesh (Tools/Scenes/Boss pelo NavMesh): o prefab Enemy 4
// ganha um NavMeshAgent do tamanho dele (cápsula de ~4 m, raio ~0,5 m) e o Rigidbody fica
// cinemático (o agente move o transform; o Rigidbody só serve às colisões do sabre).
// Usa o NavMesh que a cena 2 já tem (agente Humanoid) e confere o caminho até o player
public static class BossNavSetup
{
    const string PrefabPath = "Assets/Objects/Enemy 4.prefab";

    [MenuItem("Tools/Scenes/Boss pelo NavMesh")]
    public static void Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var capsule = root.GetComponent<CapsuleCollider>();
            Vector3 scale = root.transform.localScale;

            var agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = root.AddComponent<NavMeshAgent>();

            agent.agentTypeID = 0;
            // medidas locais (o agente multiplica pela escala): raio 0,5 m e a cápsula inteira
            agent.radius = 0.5f / Mathf.Max(scale.x, scale.z);
            agent.height = capsule != null ? capsule.height : 2f;
            // o pivô fica no meio da cápsula: a base fica meia altura abaixo
            agent.baseOffset = capsule != null ? capsule.height * 0.5f - capsule.center.y : 1f;
            agent.speed = root.GetComponent<BossChase>().moveSpeed;
            agent.acceleration = 40f;
            agent.angularSpeed = 0f;
            agent.updateRotation = false;
            agent.autoBraking = true;
            agent.stoppingDistance = root.GetComponent<BossChase>().stopDistance;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            var body = root.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.None;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("BossNavSetup: agente radius=" + agent.radius + " height=" + agent.height + " baseOffset=" + agent.baseOffset);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Check();
    }

    // o boss e o player estão no NavMesh e há caminho entre eles?
    static void Check()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Scene 2.unity");
        var boss = Object.FindFirstObjectByType<BossChase>();
        var player = Object.FindFirstObjectByType<PlayerHealth>();
        var agent = boss.GetComponent<NavMeshAgent>();

        Vector3 feet = boss.transform.position - Vector3.up * agent.baseOffset * boss.transform.lossyScale.y;
        bool bossOn = NavMesh.SamplePosition(feet, out NavMeshHit bossHit, 1f, NavMesh.AllAreas);
        bool playerOn = NavMesh.SamplePosition(player.transform.position, out NavMeshHit playerHit, 6f, NavMesh.AllAreas);
        Debug.Log("BossNavSetup: boss no NavMesh=" + bossOn + " (" + (bossOn ? Vector3.Distance(feet, bossHit.position).ToString("F2") : "-") + " m)" +
                  " player=" + playerOn + " override=" + PrefabUtility.GetPropertyModifications(boss.gameObject)?.Length);

        if (bossOn && playerOn)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(bossHit.position, playerHit.position, NavMesh.AllAreas, path);
            Debug.Log("BossNavSetup: caminho até o player " + path.status + " (" + path.corners.Length + " cantos)");
        }
    }
}
