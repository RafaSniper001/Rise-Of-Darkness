using UnityEngine;

// controla armadilha laser
public class TrapDamage : MonoBehaviour
{
    // tempo entre aparecer/desaparecer
    public float interval = 1f;

    // dano causado ao player
    public float damage = 100f;

    // som do laser ligado
    public AudioClip laserSound;

    // renderer do objeto
    Renderer objectRenderer;

    // collider do objeto
    Collider objectCollider;

    // áudio
    AudioSource audioSource;

    // contador interno
    float timer;

    // estado atual da armadilha
    bool isVisible = true;

    // executa ao iniciar
    void Start()
    {
        // pega renderer do objeto
        objectRenderer = GetComponent<Renderer>();

        // pega collider do objeto
        objectCollider = GetComponent<Collider>();

        // pega áudio
        audioSource = GetComponent<AudioSource>();

        // garante que o collider seja trigger
        objectCollider.isTrigger = true;

        // configura áudio
        if (audioSource != null)
        {
            audioSource.clip = laserSound;
            audioSource.loop = true;

            if (isVisible && laserSound != null)
            {
                audioSource.Play();
            }
        }
    }

    // executa todo frame
    void Update()
    {
        // soma o tempo passado
        timer += Time.deltaTime;

        // verifica se chegou ao intervalo
        if (timer >= interval)
        {
            // alterna estado da armadilha
            ToggleTrap();

            // reinicia contador
            timer = 0f;
        }
    }

    // mostra ou esconde a armadilha
    void ToggleTrap()
    {
        // inverte estado atual
        isVisible = !isVisible;

        // mostra ou esconde visualmente
        objectRenderer.enabled = isVisible;

        // ativa ou desativa o trigger
        objectCollider.enabled = isVisible;

        // controla som
        if (audioSource != null)
        {
            if (isVisible)
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }
    }

    // executa quando algo entra no laser
    void OnTriggerEnter(Collider other)
    {
        // verifica se é o player
        if (other.CompareTag("Player"))
        {
            // pega script de vida
            PlayerHealth playerHealth =
                other.GetComponent<PlayerHealth>();

            // verifica se encontrou
            if (playerHealth != null)
            {
                // aplica dano
                playerHealth.TakeDamage(damage);
            }
        }
    }
}