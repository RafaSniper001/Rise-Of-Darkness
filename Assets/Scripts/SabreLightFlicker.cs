using UnityEngine;

// controla efeito de tremulação da luz
public class SabreLightFlicker : MonoBehaviour
{
    // intensidade base da luz
    public float intensidadeBase = 5f;

    // quanto a intensidade varia
    public float variacaoIntensidade = 1f;

    // velocidade da tremulação
    public float velocidade = 25f;

    // quanto a luz se move
    public float movimento = 0.01f;

    // posição inicial
    Vector3 posicaoInicial;

    // componente Light
    Light luz;

    void Start()
    {
        // pega componente da luz
        luz = GetComponent<Light>();

        // salva posição original
        posicaoInicial = transform.localPosition;
    }

    void Update()
    {
        // gera valor aleatório suave
        float ruido = Mathf.PerlinNoise(
            Time.time * velocidade,
            0f
        );

        // altera intensidade
        luz.intensity =
            intensidadeBase +
            (ruido * variacaoIntensidade);

        // pequeno movimento aleatório
        Vector3 offset = new Vector3(
            Random.Range(-movimento, movimento),
            Random.Range(-movimento, movimento),
            Random.Range(-movimento, movimento)
        );

        // aplica movimento
        transform.localPosition =
            posicaoInicial + offset;
    }
}