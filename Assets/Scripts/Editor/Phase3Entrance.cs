using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// entrada da fase 3 da cena 1 (Tools/Scenes/Nivelar Entrada da Fase 3):
// o chão vermelho da fase 3 (NavMeshGround/Cube, que tem o NavMesh dos inimigos) ficava 24 cm
// acima do piso, com uma rampa inclinada (Cube (1)) na porta: o player subia a rampa, saltava
// no degrau e batia no laser.
// Aqui os dois cubos descem até 2 cm acima do piso (o vermelho continua aparecendo) e a rampa
// fica reta. O NavMesh assado de cada cubo está no espaço do próprio cubo (NavMeshSurface),
// então desce e endireita junto, sem refazer.
// As barras do laser (MovingTrap) ficam na mesma altura do chão vermelho.
public static class Phase3Entrance
{
    const float FloorY = 5.25f;

    // o chão vermelho fica um pouco acima do piso: aparece sem piscar e o degrau é imperceptível
    const float Lift = 0.02f;

    [MenuItem("Tools/Scenes/Nivelar Entrada da Fase 3")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");

        Transform ground = GameObject.Find("Millennium Falcon/NavMeshGround")?.transform;
        if (ground == null)
        {
            Debug.LogError("Phase3Entrance: NavMeshGround não encontrado");
            return;
        }

        float target = FloorY + Lift;

        foreach (Transform cube in ground)
        {
            var box = cube.GetComponent<BoxCollider>();
            if (box == null)
                continue;

            // rampa: tira a inclinação (mantém só o giro em volta do eixo vertical)
            Vector3 euler = cube.eulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > 0.01f || Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f)) > 0.01f)
            {
                cube.rotation = Quaternion.Euler(0f, euler.y, 0f);
                Debug.Log("Phase3Entrance: " + cube.name + " endireitado (era " + euler + ")");
            }

            Physics.SyncTransforms();
            float delta = box.bounds.max.y - target;
            if (Mathf.Abs(delta) > 0.001f)
            {
                cube.position -= Vector3.up * delta;
                Debug.Log("Phase3Entrance: " + cube.name + " mudou " + (-delta).ToString("F3") + " m na altura");
            }

            var renderer = cube.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = true;
        }

        // barras do laser rente ao chão vermelho
        Transform trap = GameObject.Find("Millennium Falcon/MovingTrap")?.transform;
        if (trap != null)
        {
            foreach (var moving in trap.GetComponentsInChildren<MovingTrap>(true))
            {
                var col = moving.GetComponent<Collider>();
                Physics.SyncTransforms();
                float bottom = col != null ? col.bounds.min.y : moving.transform.position.y;
                if (Mathf.Abs(bottom - target) > 0.001f)
                {
                    moving.transform.position -= Vector3.up * (bottom - target);
                    Debug.Log("Phase3Entrance: laser " + moving.name + " mudou " + (target - bottom).ToString("F3") + " m na altura");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Phase3Entrance: pronto");
    }
}
