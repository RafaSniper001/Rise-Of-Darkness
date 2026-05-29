using UnityEngine;

public class Rotacao : MonoBehaviour
{
    public float velocidade = 30f;

    void Update()
    {
        transform.Rotate(0, velocidade * Time.deltaTime, 0, Space.World);
    }
}