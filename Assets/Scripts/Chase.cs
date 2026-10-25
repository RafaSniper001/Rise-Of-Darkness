using UnityEngine;
using UnityEngine.AI;

// controla patrulha, visão, perseguição, recuo, movimento lateral,
// rotação, tiro e empurrão do inimigo.
// Sem ver o player, o robô patrulha andando (arma abaixada) entre pontos do NavMesh perto de
// onde nasceu. Ao ver o player (ou levar um golpe, ou um aliado perto avisar) entra em alerta
// e persegue/atira como antes. Se perde o player de vista, vai até o último ponto em que o viu
// e, sem achar, volta a patrulhar
public class Chase : MonoBehaviour
{
    GameObject player;
    NavMeshAgent agent;

    [Header("Detecção")]
    // alcance da visão
    public float detectionDistance = 30f;

    [Header("Visão")]
    // cone de visão (ângulo total, para a frente do corpo)
    public float viewAngle = 120f;
    // perto assim percebe o player mesmo de costas (se nada estiver no meio)
    public float hearDistance = 5f;
    // altura dos olhos acima do centro do robô, em fração da altura da cápsula
    public float eyeHeight = 0.8f;
    // o que bloqueia a visão (paredes, objetos); gatilhos são ignorados
    public LayerMask visionMask = ~0;
    // intervalo entre as checagens de visão
    public float visionInterval = 0.15f;
    // tempo sem ver o player até desistir e ir ao último ponto em que o viu
    public float loseSightTime = 4f;
    // ao ver o player, avisa os robôs neste raio
    public float alertAllyRadius = 12f;

    [Header("Patrulha")]
    public float patrolRadius = 18f;
    // distância mínima até o próximo ponto: trechos longos, em que ele chega à velocidade cheia
    public float patrolMinDistance = 8f;
    // velocidade da patrulha em relação à velocidade normal. Em 1 usa a mesma animação de
    // quando vem na direção do player (o blend do Animator escolhe o clipe pela velocidade:
    // abaixo de ~2 m/s fica o andar lento, perto de 6 m/s o passo de quando persegue)
    public float patrolSpeedFactor = 1f;
    public float patrolWaitMin = 1.5f;
    public float patrolWaitMax = 3.5f;

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
    // ponta da arma (na mão do modelo animado): o tiro sai dela, mirando
    // no mesmo ponto em que o firePoint mirava. Vazio = sai do firePoint
    public Transform muzzle;
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
    float difficulty = 1f;

    // modelo animado: animação de tiro a cada disparo
    EnemyAnimator enemyAnimator;

    enum State { Patrol, Alert, Search }
    State state = State.Patrol;

    // em alerta (perseguindo/atirando): o EnemyAnimator só levanta a arma assim
    public bool IsAlerted => state == State.Alert;

    Vector3 home;
    Vector3 lastSeenPosition;
    float lastSeenTime = float.NegativeInfinity;
    float nextVisionCheck;
    bool canSeePlayer;
    float waitUntil;
    bool waiting;
    Transform playerHead;
    CapsuleCollider capsule;
    NavMeshPath testPath;

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

        enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        capsule = GetComponent<CapsuleCollider>();
        testPath = new NavMeshPath();
        home = transform.position;

        // cabeça do player (para a visão mirar no rosto e no peito)
        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        if (playerAnimator != null && playerAnimator.isHuman)
            playerHead = playerAnimator.GetBoneTransform(HumanBodyBones.Head);

        UpdateSpeed();
        StartWaiting();
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

        if (Time.time >= nextVisionCheck)
        {
            nextVisionCheck = Time.time + visionInterval;
            canSeePlayer = CanSeePlayer(distance);

            if (canSeePlayer)
            {
                if (state != State.Alert)
                    AlertAllies();

                Alert(player.transform.position);
            }
        }

        if (state == State.Alert &&
            !canSeePlayer &&
            Time.time - lastSeenTime > loseSightTime)
        {
            // perdeu o player: vai até onde o viu por último
            state = State.Search;
            UpdateSpeed();
            agent.SetDestination(lastSeenPosition);
        }

        if (state != State.Alert)
        {
            Patrol();
            return;
        }

        Vector3 directionToPlayer =
            player.transform.position - transform.position;

        directionToPlayer.y = 0f;
        directionToPlayer.Normalize();

        LookAtPlayer();

        // só atira com o player à vista (não atravessa paredes)
        if (distance <= shootDistance && canSeePlayer)
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
        difficulty = multiplier;
        UpdateSpeed();

        fireRate = baseFireRate / multiplier;
    }

    // velocidade normal em alerta; andando (fração dela) na patrulha e na busca
    void UpdateSpeed()
    {
        if (agent == null)
            return;

        float factor = state == State.Alert ? 1f : patrolSpeedFactor;
        agent.speed = baseSpeed * difficulty * factor;
    }

    // ---------- Visão ----------

    // cone de visão + distância + linha de visada: um raio dos olhos até o peito e outro até a
    // cabeça do player. Raio (e não SphereCast) porque o que importa é se existe uma linha
    // livre até o corpo: o SphereCast, por ser grosso, bate na quina de portas e objetos e
    // deixa de ver o player que está visível pela fresta; o OverlapSphere não sabe de paredes
    bool CanSeePlayer(float distance)
    {
        if (distance > detectionDistance)
            return false;

        Vector3 eye = EyePosition();
        Vector3 chest = player.transform.position;
        Vector3 forward = enemyAnimator != null ? enemyAnimator.transform.forward : transform.forward;
        Vector3 flat = chest - eye;
        flat.y = 0f;

        // fora do cone só percebe se estiver bem perto
        if (distance > hearDistance &&
            Vector3.Angle(forward, flat) > viewAngle * 0.5f)
            return false;

        if (HasLineOfSight(eye, chest))
            return true;

        return playerHead != null && HasLineOfSight(eye, playerHead.position);
    }

    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        RaycastHit[] hits = Physics.RaycastAll(from, direction.normalized, direction.magnitude + 0.5f, visionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // o primeiro obstáculo que não é o próprio robô (corpo, arma) decide
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            return hit.transform.IsChildOf(player.transform);
        }

        return true;
    }

    Vector3 EyePosition()
    {
        float height = capsule != null ? capsule.height * transform.lossyScale.y : 2f;
        return transform.position + Vector3.up * height * (eyeHeight - 0.5f);
    }

    // entra em alerta (viu o player, levou golpe ou um aliado avisou)
    public void Alert(Vector3 playerPosition)
    {
        lastSeenPosition = playerPosition;
        lastSeenTime = Time.time;

        if (state == State.Alert)
            return;

        state = State.Alert;
        waiting = false;
        UpdateSpeed();
    }

    void AlertAllies()
    {
        foreach (Collider col in Physics.OverlapSphere(transform.position, alertAllyRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            Chase ally = col.GetComponent<Chase>();
            if (ally != null && ally != this)
                ally.Alert(player.transform.position);
        }
    }

    // ---------- Patrulha ----------

    void Patrol()
    {
        if (!agent.isOnNavMesh)
            return;

        bool arrived =
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.5f;

        if (state == State.Search)
        {
            // chegou onde viu o player por último e não achou: volta a patrulhar
            if (arrived)
            {
                state = State.Patrol;
                UpdateSpeed();
                StartWaiting();
            }
            return;
        }

        if (waiting)
        {
            if (Time.time >= waitUntil)
            {
                waiting = false;
                GoToRandomPatrolPoint();
            }
            return;
        }

        if (arrived)
            StartWaiting();
    }

    void StartWaiting()
    {
        waiting = true;
        waitUntil = Time.time + Random.Range(patrolWaitMin, patrolWaitMax);

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }

    // consultas ao NavMesh no tipo de agente do robô ("Robo", do tamanho dele)
    NavMeshQueryFilter QueryFilter()
    {
        return new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = NavMesh.AllAreas
        };
    }

    // ponto aleatório do NavMesh perto de onde nasceu, com caminho completo até ele
    void GoToRandomPatrolPoint()
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, QueryFilter()))
                continue;

            // perto demais: mal acelera e parece andar mais devagar
            if (attempt < 12 &&
                Vector3.Distance(hit.position, transform.position) < patrolMinDistance)
                continue;

            if (agent.CalculatePath(hit.position, testPath) &&
                testPath.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(testPath);
                return;
            }
        }

        StartWaiting();
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
                QueryFilter()
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
                QueryFilter()
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

        Vector3 spawnPosition = firePoint.position;
        Quaternion spawnRotation = firePoint.rotation;

        if (muzzle != null)
        {
            // mesmo alvo de antes: o ponto à frente do firePoint na distância do player
            float distance =
                Vector3.Distance(firePoint.position, player.transform.position);

            Vector3 target =
                firePoint.position + firePoint.forward * distance;

            spawnPosition = muzzle.position;
            spawnRotation = Quaternion.LookRotation(target - muzzle.position);
        }

        Instantiate(
            bulletPrefab,
            spawnPosition,
            spawnRotation
        );

        if (enemyAnimator != null)
            enemyAnimator.PlayShoot();

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
        // levou golpe: entra em alerta
        if (player != null)
            Alert(player.transform.position);

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