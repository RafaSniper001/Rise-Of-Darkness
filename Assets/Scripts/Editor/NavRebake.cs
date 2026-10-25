using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// NavMesh da cena 1 (Tools/Scenes/Refazer NavMesh da Cena 1):
// a cena tinha 7 NavMeshSurface, cada uma com uma cópia assada do NavMesh da nave inteira,
// presa ao próprio objeto. Mover um deles (o chão vermelho da fase 3) moveu a cópia toda
// e as fases 2 e 3 ficaram com camadas de NavMesh em alturas diferentes.
// Aqui fica um NavMesh só, na raiz da nave, assado com a geometria atual.
// As superfícies antigas só são desligadas (os dados continuam no projeto).
// Os enfeites não entram no assado (estão fora da nave na hierarquia e recortam o NavMesh
// com NavMeshObstacle), nem o que se move ou só machuca o player: player, BB-8, R2D2,
// as barras e o feixe dos lasers e as portas entre as fases (que recortam o NavMesh
// enquanto estão fechadas).
public static class NavRebake
{
    const string AssetPath = "Assets/Scenes/Scene 1/NavMesh-Cena 1.asset";

    static readonly string[] Ignore =
    {
        "Millennium Falcon/Player",
        "Millennium Falcon/BB-8_COLORIDO",
        "Millennium Falcon/R2D2",
        "Millennium Falcon/MovingTrap",
        "Millennium Falcon/Millennium Falcon completo",
        // portas entre as fases: o NavMeshObstacle fica no pai e a malha num filho, e o
        // assado só ignora a malha que tem o obstáculo no mesmo objeto. Sem isto a porta
        // fechada vira parede no NavMesh e os robôs não passam de uma sala para outra
        "Millennium Falcon/Porta",
        "Millennium Falcon/Porta (1)",
        // laser da fase 1: o feixe fino atravessa o corredor a 1 m do chão e viraria parede
        "Millennium Falcon/TrapDamage",
        // scanners e torretas presos na parede a 2-3 m do chão, ao lado das portas: com a
        // altura dos robôs (4 m) fechariam as passagens
        "Millennium Falcon/Scaner",
        "Millennium Falcon/Scaner (1)",
        "Millennium Falcon/Scaner (2)",
        "Millennium Falcon/Scaner (3)",
        "Millennium Falcon/TrapGuns",
    };

    [MenuItem("Tools/Scenes/Refazer NavMesh da Cena 1")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");
        var root = GameObject.Find("Millennium Falcon");

        foreach (var old in Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.gameObject != root && old.enabled)
            {
                old.enabled = false;
                Debug.Log("NavRebake: superfície antiga desligada em " + old.name);
            }

        foreach (string path in Ignore)
        {
            var go = GameObject.Find(path);
            if (go == null)
                continue;

            var modifier = go.GetComponent<NavMeshModifier>();
            if (modifier == null)
                modifier = go.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }

        // portas entre as fases: fora do assado (senão a passagem nunca existe), mas recortando
        // o NavMesh enquanto estão fechadas. A animação de abrir desliza o próprio objeto com o
        // obstáculo, então o recorte sai junto e a passagem abre (carveOnlyStationary desligado
        // para o recorte acompanhar o movimento)
        foreach (string door in new[] { "Millennium Falcon/Porta", "Millennium Falcon/Porta (1)" })
        {
            var go = GameObject.Find(door);
            var obstacle = go != null ? go.GetComponent<UnityEngine.AI.NavMeshObstacle>() : null;
            var box = go != null ? go.GetComponent<BoxCollider>() : null;
            if (obstacle == null)
                continue;

            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            if (box != null)
            {
                obstacle.center = box.center;
                obstacle.size = box.size;
            }
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            Debug.Log("NavRebake: porta " + go.name + " recorta o NavMesh enquanto fechada");
        }

        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = root.AddComponent<NavMeshSurface>();

        // tipo de agente "Robo" (Project Settings > Navigation): raio 1 m e altura 4 m, do tamanho
        // dos robôs. Com o "Humanoid" padrão (raio 0,5) o NavMesh deixava o centro do robô a meio
        // metro das paredes e dos objetos, e o corpo dele entrava neles
        int robotAgent = RobotAgentType();
        SetRobotAgents(robotAgent);

        surface.agentTypeID = robotAgent;
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = ~0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.enabled = true;

        surface.BuildNavMesh();

        AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, AssetPath);
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(AssetPath);
        surface.AddData();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("NavRebake: pronto, " + UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length / 3 + " triângulos");
    }

    static int RobotAgentType()
    {
        for (int i = 0; i < UnityEngine.AI.NavMesh.GetSettingsCount(); i++)
        {
            var settings = UnityEngine.AI.NavMesh.GetSettingsByIndex(i);
            if (UnityEngine.AI.NavMesh.GetSettingsNameFromID(settings.agentTypeID) == "Robo")
                return settings.agentTypeID;
        }

        Debug.LogError("NavRebake: tipo de agente 'Robo' não existe em Project Settings > Navigation");
        return 0;
    }

    // robôs (prefabs) e o BB-8 da cena 1 usam o NavMesh dos robôs. O raio do agente do robô fica
    // em 0,5 (vezes a escala 1,99 dele = 1 m, o mesmo do NavMesh), para o desvio entre eles
    static void SetRobotAgents(int agentType)
    {
        foreach (string path in new[] { "Assets/Objects/Enemy.prefab", "Assets/Objects/Enemy 1.prefab", "Assets/Objects/Enemy 2.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.agentTypeID = agentType;
                    agent.radius = 0.5f;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (var agent in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            agent.agentTypeID = agentType;
            Debug.Log("NavRebake: agente " + agent.name + " no NavMesh dos robôs");
        }
    }
}
