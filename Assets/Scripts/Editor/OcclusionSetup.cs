using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Occlusion Culling (Tools/Occlusion Culling/Configurar e assar cenas 1 e 2):
// 1) marca como Occluder Static + Occludee Static os objetos que ficam parados (paredes, piso,
//    props da nave). Quem se mexe (player, inimigos, armadilhas, portas, céu, objetos
//    que giram ou se destroem) fica de fora: um occluder que sai do lugar continuaria
//    tampando o que está atrás dele no assado.
//    Objetos que podem ser destruídos (caixas) entram só como Occludee Static: podem ser
//    escondidos, mas não escondem nada.
// 2) coloca um Occlusion Portal na porta de cada FinalButton (a que abre entre as fases),
//    do tamanho do BoxCollider dela. O FinalButton liga/desliga o "open" no jogo.
// 3) assa os dados de occlusion culling de cada cena.
// O frustum culling (não desenhar o que está fora do ângulo da câmera) o Unity já faz sozinho.
public static class OcclusionSetup
{
    // (BB8Follow é o nome real da classe do script BBFollow.cs)
    static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Scene 1.unity",
        "Assets/Scenes/Scene 2.unity",
    };

    // scripts que mexem no objeto (ou nos filhos) durante o jogo
    static readonly System.Type[] Movers =
    {
        typeof(Rotacao), typeof(RotacaoEstrelaDaMorte), typeof(SkyboxFollowObject),
        typeof(MovingTrap), typeof(BB8Follow), typeof(AuraFollow), typeof(PlayerMovement),
        typeof(Chase), typeof(BossChase), typeof(ThirdPersonCamera), typeof(ScanCameraController),
        typeof(HealthPickup), typeof(FinalButton), typeof(Sword), typeof(BossSword),
        typeof(EnemyBullet), typeof(TrapShooter),
    };

    [MenuItem("Tools/Occlusion Culling/Configurar e assar cenas 1 e 2")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string original = SceneManager.GetActiveScene().path;

        foreach (string path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int occluders = 0, occludeesOnly = 0, skipped = 0;
            MarkStatic(scene, ref occluders, ref occludeesOnly, ref skipped);
            int portals = AddPortals();
            Debug.Log($"OcclusionSetup [{scene.name}]: {occluders} occluders+occludees, {occludeesOnly} só occludees, {skipped} móveis ignorados, {portals} portais");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            StaticOcclusionCulling.smallestOccluder = 3f;
            StaticOcclusionCulling.smallestHole = 0.5f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            bool ok = StaticOcclusionCulling.Compute();
            Debug.Log($"OcclusionSetup [{scene.name}]: bake {(ok ? "concluído" : "FALHOU")}");
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    static void MarkStatic(Scene scene, ref int occluders, ref int occludeesOnly, ref int skipped)
    {
        var doors = new HashSet<GameObject>();
        foreach (var fb in Object.FindObjectsByType<FinalButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (fb.door != null)
                doors.Add(fb.door);

        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = r.gameObject;
            if (go.scene != scene)
                continue;

            if (IsMoving(go, doors))
            {
                skipped++;
                continue;
            }

            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            bool destructible = go.GetComponentInParent<Destructible>() != null;
            flags |= StaticEditorFlags.OccludeeStatic;
            if (!destructible)
                flags |= StaticEditorFlags.OccluderStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);

            if (destructible) occludeesOnly++;
            else occluders++;
        }
    }

    static bool IsMoving(GameObject go, HashSet<GameObject> doors)
    {
        for (var t = go.transform; t != null; t = t.parent)
        {
            var g = t.gameObject;
            if (doors.Contains(g)) return true;
            if (g.CompareTag("Player") || g.CompareTag("Enemy")) return true;
            if (g.GetComponent<Animator>() != null) return true;
            if (g.GetComponent<Rigidbody>() != null) return true;
            if (g.GetComponent<UnityEngine.AI.NavMeshAgent>() != null) return true;
            if (g.GetComponent<ParticleSystem>() != null) return true;
            foreach (var m in Movers)
                if (g.GetComponent(m) != null) return true;
        }
        return false;
    }

    static int AddPortals()
    {
        int count = 0;
        foreach (var fb in Object.FindObjectsByType<FinalButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fb.door == null)
                continue;

            var portal = fb.door.GetComponent<OcclusionPortal>();
            if (portal == null)
                portal = fb.door.AddComponent<OcclusionPortal>();

            // tamanho e posição do portal = os do BoxCollider da porta
            var box = fb.door.GetComponent<BoxCollider>();
            if (box != null)
            {
                var so = new SerializedObject(portal);
                var c = so.FindProperty("m_Center");
                var s = so.FindProperty("m_Size");
                if (c != null) c.vector3Value = box.center;
                if (s != null) s.vector3Value = box.size;
                so.ApplyModifiedProperties();
            }

            portal.open = false; // porta fechada: não vê o que está atrás dela
            EditorUtility.SetDirty(portal);
            count++;
        }
        return count;
    }
}
