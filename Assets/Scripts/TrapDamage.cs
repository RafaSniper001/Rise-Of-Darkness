using UnityEngine;

// controla armadilha laser
public class TrapDamage : MonoBehaviour
{
    public float interval = 1f;
    public float damage = 100f;
    public AudioClip laserSound;

    Renderer objectRenderer;
    Collider objectCollider;
    AudioSource audioSource;

    float timer;
    bool isVisible = true;

    // controla se o player já sofreu dano neste ciclo do laser
    bool playerAlreadyDamaged = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        objectCollider.isTrigger = true;

        if (audioSource != null)
        {
            audioSource.clip = laserSound;
            audioSource.loop = true;

            if (isVisible && laserSound != null)
                audioSource.Play();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            ToggleTrap();
            timer = 0f;
        }
    }

    void ToggleTrap()
    {
        isVisible = !isVisible;

        objectRenderer.enabled = isVisible;
        objectCollider.enabled = isVisible;

        // quando o laser some, libera para causar dano no próximo ciclo
        if (!isVisible)
        {
            playerAlreadyDamaged = false;
        }

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

    void OnTriggerEnter(Collider other)
    {
        if (!isVisible)
            return;

        if (playerAlreadyDamaged)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerAlreadyDamaged = true;
                playerHealth.KillPlayer();
            }
        }
    }
}