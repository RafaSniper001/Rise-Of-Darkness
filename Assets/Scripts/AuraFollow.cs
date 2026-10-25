using UnityEngine;

// fica no objeto da aura do poder: acompanha o quadril do modelo, mantendo a distância que tinha.
// É um componente separado porque na morte o PlayerHealth desliga os scripts do player
// (o PlayerPower inclusive), e a aura precisa descer junto com o corpo que cai
public class AuraFollow : MonoBehaviour
{
    public Transform anchor;
    public Transform body;
    public Vector3 offset;

    void LateUpdate()
    {
        if (anchor != null && body != null)
            transform.position = anchor.position + body.TransformVector(offset);
    }
}
