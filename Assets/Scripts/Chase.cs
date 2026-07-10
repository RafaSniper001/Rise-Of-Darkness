using UnityEngine;
using UnityEngine.AI;

// controla perseguição, recuo, movimento lateral,
// rotação, tiro e empurrão do inimigo
public class Chase : MonoBehaviour
{
    GameObject player;
    NavMeshAgent agent;

    [Header("Detecção")]
    public float detectionDistance = 30f;

    [Header("Movimento")]
    public float idealDistance = 10f;
    public float tooCloseDistance = 9f;
    public float retreatDistance = 6f;

    [Header("Performance")]
    public float pathUpdateRate = 0.3f;
    float nextPathUpdate;

    [Header("Movimento em volta")]
    public float strafeDistance = 3f;
    public float strafeChangeTime = 2f;
    float nextStrafeTime;
    int strafeDirection = 1;

    [Header("Rotação")]
    public float rotationSpeed = 8f;

    [Header("Tiro")]
    public float shootDistance = 10f;
    public float fireRate = 2f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    float nextFireTime;
    public AudioClip[] shotSounds;
    AudioSource audioSource;

    [Header("Empurrão padrão")]
    public float pushDistance = 3f;
    public float pushDuration = 0.25f;

    bool isBeingPushed = false;
    Vector3 pushDirection;
    float pushTimer;
    float pushSpeed;

    float baseSpeed;
    float baseFireRate;

    void OnEnable()
    {
        GameSettings.OnDifficultyChanged += ApplyDifficulty;
    }

    void OnDisable()
    {
        GameSettings.OnDifficultyChanged -= ApplyDifficulty;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            Debug.LogError("Player não encontrado.");
            enabled = false;
            return;
        }

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent não encontrado.");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(gameObject.name + " não está sobre o NavMesh.");
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.autoBraking = false;
        agent.autoRepath = true;
        agent.avoidancePriority = Random.Range(20, 80);

        baseSpeed = agent.speed;
        baseFireRate = fireRate;

        ApplyDifficulty(GameSettings.difficultyMultiplier);

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (player == null || agent == null)
            return;

        if (isBeingPushed)
        {
            PushMovement();
            return;
        }

        float distance =
            Vector3.Distance(transform.position, player.transform.position);

        if (distance > detectionDistance)
        {
            agent.ResetPath();
            return;
        }

        Vector3 directionToPlayer =
            player.transform.position - transform.position;

        directionToPlayer.y = 0f;
        directionToPlayer.Normalize();

        LookAtPlayer();

        if (distance <= shootDistance)
        {
            Shoot();
        }

        if (Time.time < nextPathUpdate)
            return;

        nextPathUpdate = Time.time + pathUpdateRate;

        if (distance > idealDistance)
        {
            MoveToPlayer();
        }
        else if (distance < tooCloseDistance)
        {
            RetreatFromPlayer(directionToPlayer);
        }
        else
        {
            StrafeAroundPlayer(directionToPlayer);
        }
    }

    void ApplyDifficulty(float multiplier)
    {
        if (agent != null)
        {
            agent.speed = baseSpeed * multiplier;
        }

        fireRate = baseFireRate / multiplier;
    }

    void MoveToPlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.SetDestination(player.transform.position);
    }

    void RetreatFromPlayer(Vector3 directionToPlayer)
    {
        Vector3 retreatPosition =
            transform.position -
            directionToPlayer *
            retreatDistance;

        NavMeshHit hit;

        if (
            NavMesh.SamplePosition(
                retreatPosition,
                out hit,
                4f,
                NavMesh.AllAreas
            )
        )
        {
            agent.SetDestination(hit.position);
        }
    }

    void StrafeAroundPlayer(Vector3 directionToPlayer)
    {
        if (Time.time >= nextStrafeTime)
        {
            nextStrafeTime = Time.time + strafeChangeTime;
            strafeDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        }

        Vector3 sideDirection =
            Vector3.Cross(Vector3.up, directionToPlayer) *
            strafeDirection;

        Vector3 strafePosition =
            transform.position +
            sideDirection *
            strafeDistance;

        NavMeshHit hit;

        if (
            NavMesh.SamplePosition(
                strafePosition,
                out hit,
                3f,
                NavMesh.AllAreas
            )
        )
        {
            agent.SetDestination(hit.position);
        }
    }

    void LookAtPlayer()
    {
        Vector3 direction =
            player.transform.position - transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireRate;

        if (bulletPrefab == null || firePoint == null)
            return;

        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        if (audioSource != null && shotSounds.Length > 0)
        {
            int randomIndex =
                Random.Range(0, shotSounds.Length);

            audioSource.PlayOneShot(shotSounds[randomIndex]);
        }
    }

    // empurrão padrão usado pela espada
    public void PushAwayFrom(Vector3 originPosition)
    {
        PushAwayFrom(
            originPosition,
            pushDistance,
            pushDuration
        );
    }

    // empurrão personalizado usado pela aura
    public void PushAwayFrom(
        Vector3 originPosition,
        float customDistance,
        float customDuration
    )
    {
        pushDirection =
            transform.position - originPosition;

        pushDirection.y = 0f;

        if (pushDirection == Vector3.zero)
            return;

        pushDirection.Normalize();

        pushTimer = customDuration;

        pushSpeed =
            customDistance /
            customDuration;

        isBeingPushed = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    void PushMovement()
    {
        pushTimer -= Time.deltaTime;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.Move(
                pushDirection *
                pushSpeed *
                Time.deltaTime
            );
        }

        if (pushTimer <= 0f)
        {
            isBeingPushed = false;
        }
    }
}