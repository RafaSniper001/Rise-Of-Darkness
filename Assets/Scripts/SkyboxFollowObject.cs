using UnityEngine;

public class SkyboxFollowObject : MonoBehaviour
{
    public Transform estrelaDaMorte;
    public float multiplicador = -1f;

    void Update()
    {
        float rotacao = estrelaDaMorte.localEulerAngles.z;

        RenderSettings.skybox.SetFloat(
            "_Rotation",
            rotacao * multiplicador
        );
    }
}