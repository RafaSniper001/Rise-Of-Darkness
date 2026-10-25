using UnityEngine;
using UnityEngine.AI;

// IA do boss (Veyron): máquina de estados andando pelo NavMesh.
// Idle     - parado até identificar o player (visão com linha de visada, ouvir de perto
//            ou levar dano); depois disso não larga mais o alvo
// Chase    - longe do player: corre atrás dele
// Approach - chegando perto: anda (Walk)
// Attack   - ao alcance: para, encara o player e o BossAttack golpeia com o sabre
// Pushed   - empurrado pela explosão de aura do player
// (o poder de aura do boss fica no BossAura)
public class BossChase : MonoBehaviour
{
    public enum BossState { Idle, Chase, Approach, Attack, Pushed }

    public BossState State { get; private set; } = BossState.Idle;

    // já identificou o player como alvo
    public bool IsEngaged { get; private set; }

    // chegando perto: anima andando em vez de correndo
    public bool IsWalking => State == BossState.Approach;

    public Transform player;

    public float moveSpeed = 4f;
    public float stopDistance = 2.2f;

    [Header("Detecção do alvo")]
    // vê o player até esta distância, se nada estiver no meio
    public float detectionDistance = 40f;
    // perto assim percebe o player mesmo sem ver
    public float hearDistance = 8f;
    // altura dos olhos acima do pivô (metros)
    public float eyeHeight = 1.5f;
    public float visionInterval = 0.2f;

    [Header("Andar / correr")]
    // mais perto que isto anda em vez de correr
    public float walkDistance = 9f;
    // velocidade andando, em fração de moveSpeed (o reforço da aura vale para as duas)
    [Range(0.1f, 1f)]
    public float walkSpeedFactor = 0.5f;

    // sons de passos
    public AudioSource audioSource;
    public AudioClip[] footstepSounds;

    // com animação: cada pé tem seus sons, tocados na batida do pé (Animation Event)
    public AudioClip[] leftFootSounds;
    public AudioClip[] rightFootSounds;

    // evita som dobrado do mesmo pé quando andar e correr se misturam no blend
    public float minStepInterval = 0.15f;

    // intervalo entre passos (só sem animação)
    public float stepInterval = 0.5f;

    // verificação de chão
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    // folga (em unidades locais) para continuar "no chão" em rampas íngremes
    public float slopeGroundTolerance = 0.35f;

    public bool IsGrounded { get; private set; } = true;

    float stepTimer = 0f;
    bool canStep = false;
    float[] lastStepTime = { -1f, -1f };

    BossAnimator bossAnimator;

    // giro para encarar o player (o agente não gira sozinho)
    public float turnSpeed = 10f;

    // de quanto em quanto tempo recalcula o caminho até o player
    public float repathInterval = 0.2f;

    // controla se está sendo empurrado
    bool isBeingPushed = false;

    // movimento pelo NavMesh: o agente desvia de paredes e objetos, sobe rampas e
    // desníveis e fica sempre no chão. O Rigidbody fica cinemático (só para as colisões
    // do sabre e dos gatilhos)
    NavMeshAgent agent;
    Vector3 pushVelocity;
    float pushTimeLeft;
    float repathTimer;
    float nextVisionCheck;
    EnemyHealth health;
    float lastHealth;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        lastHealth = health != null ? health.currentHealth : 0f;

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.stoppingDistance = stopDistance;

            Rigidbody body = GetComponent<Rigidbody>();

            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        if (player == null)
        {
            GameObject foundPlayer =
                GameObject.FindGameObjectWithTag("Player");

            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        bossAnimator = GetComponentInChildren<BossAnimator>();
    }

    void Update()
    {
        IsGrounded = CheckGround();

        if (player == null)
            return;

        bool onNavMesh = agent != null && agent.enabled && agent.isOnNavMesh;

        // enquanto está sendo empurrado, não persegue o player: desliza pelo NavMesh
        // (para na borda e nas paredes)
        if (isBeingPushed)
        {
            State = BossState.Pushed;
            pushTimeLeft -= Time.deltaTime;

            if (onNavMesh)
            {
                agent.ResetPath();
                agent.Move(pushVelocity * Time.deltaTime);
            }
            else
            {
                transform.position += pushVelocity * Time.deltaTime;
            }

            if (pushTimeLeft <= 0f)
                isBeingPushed = false;

            canStep = false;
            return;
        }

        // distância no chão (horizontal): com o player no ar a altura não conta,
        // senão o boss continuava andando até ficar embaixo dele
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        UpdateEngagement(distance);

        // Idle: ainda não identificou o player, fica parado
        if (!IsEngaged)
        {
            State = BossState.Idle;

            if (onNavMesh && agent.hasPath)
                agent.ResetPath();

            canStep = false;
            return;
        }

        // encara o player
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(toPlayer, Vector3.up),
                    turnSpeed * Time.deltaTime
                );
        }

        bool isMoving =
            distance > stopDistance;

        State =
            !isMoving ? BossState.Attack :
            distance <= walkDistance ? BossState.Approach :
            BossState.Chase;

        if (onNavMesh)
        {
            // o reforço da aura do boss muda moveSpeed; chegando perto, anda
            agent.speed = IsWalking ? moveSpeed * walkSpeedFactor : moveSpeed;
            agent.stoppingDistance = stopDistance;

            if (isMoving)
            {
                repathTimer -= Time.deltaTime;

                if (repathTimer <= 0f || !agent.hasPath)
                {
                    repathTimer = repathInterval;

                    // player no ar: vai até o ponto do chão embaixo dele
                    if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, 6f, agent.areaMask))
                        agent.SetDestination(hit.position);
                }
            }
            else if (agent.hasPath)
            {
                agent.ResetPath();
            }

            isMoving = isMoving && agent.velocity.sqrMagnitude > 0.01f;
        }
        else if (isMoving)
        {
            // sem NavMesh: anda como antes, direto pelo transform
            transform.position += toPlayer.normalized * (IsWalking ? moveSpeed * walkSpeedFactor : moveSpeed) * Time.deltaTime;
        }

        bool isGrounded = IsGrounded;

        // os passos tocam pelo Animation Event Footstep (PlayFootstep)
        canStep = isMoving && isGrounded;

        if (bossAnimator != null)
            return;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                if (footstepSounds.Length > 0 &&
                    audioSource != null)
                {
                    int randomIndex =
                        Random.Range(
                            0,
                            footstepSounds.Length
                        );

                    audioSource.PlayOneShot(
                        footstepSounds[randomIndex]
                    );
                }

                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    // chamado na batida de cada pé (Animation Event): 0 = esquerdo, 1 = direito
    public void PlayFootstep(int foot)
    {
        foot = Mathf.Clamp(foot, 0, 1);

        AudioClip[] sounds =
            foot == 0 ? leftFootSounds : rightFootSounds;

        if (!canStep ||
            audioSource == null ||
            sounds == null ||
            sounds.Length == 0)
            return;

        if (Time.time - lastStepTime[foot] < minStepInterval)
            return;

        lastStepTime[foot] = Time.time;

        audioSource.PlayOneShot(
            sounds[Random.Range(0, sounds.Length)]
        );
    }

    bool CheckGround()
    {
        if (groundCheck == null)
            return false;

        Collider[] colliders =
            Physics.OverlapSphere(
                groundCheck.position,
                groundCheckRadius
            );

        foreach (Collider col in colliders)
        {
            if (IsGround(col))
                return true;
        }

        return CheckGroundOnSlope();
    }

    bool IsGround(Collider col)
    {
        // ignora tag Enemy, filhos do próprio boss e triggers
        return !col.CompareTag("Enemy") &&
               !col.transform.IsChildOf(transform) &&
               !col.isTrigger;
    }

    // em rampa íngreme a base da cápsula fica longe do chão e a esfera do
    // groundCheck não alcança: lança a cápsula para baixo com uma folga
    bool CheckGroundOnSlope()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        if (capsule == null)
            return false;

        float scale = transform.lossyScale.y;
        float radius = capsule.radius * transform.lossyScale.x * 0.9f;
        Vector3 center = transform.TransformPoint(capsule.center);
        float distance =
            capsule.height * scale * 0.5f - radius +
            slopeGroundTolerance * scale;

        RaycastHit[] hits =
            Physics.SphereCastAll(
                center,
                radius,
                Vector3.down,
                distance
            );

        foreach (RaycastHit hit in hits)
        {
            if (hit.distance > 0f && IsGround(hit.collider))
                return true;
        }

        return false;
    }

    // empurrão (aura do player): a mesma distância no mesmo tempo, deslizando pelo
    // NavMesh (Update), então paredes e objetos seguram o boss em vez de ele entrar neles
    public void PushAwayFrom(
        Vector3 sourcePosition,
        float pushDistance,
        float pushDuration
    )
    {
        Vector3 direction =
            transform.position - sourcePosition;

        direction.y = 0f;
        direction.Normalize();

        pushDuration = Mathf.Max(pushDuration, 0.01f);
        pushVelocity = direction * (pushDistance / pushDuration);
        pushTimeLeft = pushDuration;
        isBeingPushed = true;
        Engage();
    }

    // identifica o player como alvo: vendo (distância + linha de visada), ouvindo de perto
    // ou levando dano. Depois disso persegue até o fim da luta
    void UpdateEngagement(float distance)
    {
        if (IsEngaged)
            return;

        if (health != null)
        {
            if (health.currentHealth < lastHealth)
            {
                Engage();
                return;
            }

            lastHealth = health.currentHealth;
        }

        if (distance <= hearDistance)
        {
            Engage();
            return;
        }

        if (distance > detectionDistance || Time.time < nextVisionCheck)
            return;

        nextVisionCheck = Time.time + visionInterval;

        if (HasLineOfSight())
            Engage();
    }

    public void Engage()
    {
        IsEngaged = true;
    }

    // nada (fora o próprio boss, o player e gatilhos) entre os olhos do boss e o player
    bool HasLineOfSight()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 direction = target - eye;
        float length = direction.magnitude;

        foreach (RaycastHit hit in Physics.RaycastAll(eye, direction / length, length, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player))
                continue;

            return false;
        }

        return true;
    }

    // desligado (morte, fim de jogo): o agente para onde está
    void OnDisable()
    {
        pushTimeLeft = 0f;
        isBeingPushed = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }
}
