using UnityEngine;

public class RotacaoEstrelaDaMorte : MonoBehaviour
{
    public float velocidade = 10f;

    void Update()
    {
        transform.Rotate(0, 0, velocidade * Time.deltaTime, Space.Self);
    }
}