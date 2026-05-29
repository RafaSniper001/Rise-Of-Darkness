using UnityEngine;
using UnityEngine.AI;

// controla perseguição, recuo, movimento lateral,
// rotação, tiro e empurrão do inimigo
public class Chase : MonoBehaviour
{
    // referência do player
    GameObject player;

    // referência do NavMeshAgent
    NavMeshAgent agent;

    [Header("Detecção")]

    // distância máxima para detectar o player
    public float detectionDistance = 30f;

    [Header("Movimento")]

    // distância ideal que o inimigo tenta manter
    public float idealDistance = 10f;

    // se ficar mais perto que isso, recua
    public float tooCloseDistance = 9f;

    // distância do recuo
    public float retreatDistance = 6f;

    [Header("Performance")]

    // tempo entre cada recalculo de rota
    // ajuda muito no Build
    public float pathUpdateRate = 0.3f;

    // controla quando poderá recalcular novamente
    float nextPathUpdate;

    [Header("Movimento em volta")]

    // distância lateral ao circular o player
    public float strafeDistance = 3f;

    // tempo para trocar lado
    public float strafeChangeTime = 2f;

    // próximo momento da troca
    float nextStrafeTime;

    // direção lateral atual
    // 1 = direita
    // -1 = esquerda
    int strafeDirection = 1;

    [Header("Rotação")]

    // velocidade para girar olhando para o player
    public float rotationSpeed = 8f;

    [Header("Tiro")]

    // distância máxima para atirar
    public float shootDistance = 10f;

    // tempo entre tiros
    public float fireRate = 2f;

    // prefab do projétil
    public GameObject bulletPrefab;

    // ponto de saída do tiro
    public Transform firePoint;

    // controla cooldown do tiro
    float nextFireTime;

    [Header("Empurrão")]

    // distância total do empurrão
    public float pushDistance = 3f;

    // tempo do empurrão
    public float pushDuration = 0.25f;

    // controla se está sendo empurrado
    bool isBeingPushed = false;

    // direção atual do empurrão
    Vector3 pushDirection;

    // tempo restante do empurrão
    float pushTimer;

    // velocidade do empurrão
    float pushSpeed;

    // velocidade original do agente
    float baseSpeed;

    // fire rate original
    float baseFireRate;

    // executa ao iniciar
    void Start()
    {
        // encontra o player pela tag
        player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        // pega NavMeshAgent
        agent =
            GetComponent<NavMeshAgent>();

        // verifica se encontrou o player
        if (player == null)
        {
            Debug.LogError(
                "Player não encontrado."
            );

            enabled = false;
            return;
        }

        // verifica se existe agent
        if (agent == null)
        {
            Debug.LogError(
                "NavMeshAgent não encontrado."
            );

            enabled = false;
            return;
        }

        // verifica se iniciou sobre o NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                gameObject.name +
                " não está sobre o NavMesh."
            );

            enabled = false;
            return;
        }

        // desativa rotação automática
        agent.updateRotation = false;

        // evita freada brusca
        agent.autoBraking = false;

        // recalcula rota sozinho quando necessário
        agent.autoRepath = true;

        // prioridade aleatória para evitar
        // vários inimigos travando entre si
        agent.avoidancePriority =
            Random.Range(20, 80);

        // salva velocidade original
        baseSpeed = agent.speed;

        // salva fire rate original
        baseFireRate = fireRate;
    }

    // executa todo frame
    void Update()
    {
        // segurança
        if (
            player == null ||
            agent == null
        )
        {
            return;
        }

        // se estiver sendo empurrado
        if (isBeingPushed)
        {
            PushMovement();
            return;
        }

        // aplica dificuldade na velocidade
        agent.speed =
            baseSpeed *
            GameSettings.difficultyMultiplier;

        // aplica dificuldade no tiro
        fireRate =
            baseFireRate /
            GameSettings.difficultyMultiplier;

        // calcula distância até o player
        float distance =
            Vector3.Distance(
                transform.position,
                player.transform.position
            );

        // se estiver longe demais
        if (distance > detectionDistance)
        {
            agent.ResetPath();
            return;
        }

        // direção até o player
        Vector3 directionToPlayer =
            player.transform.position -
            transform.position;

        // remove inclinação vertical
        directionToPlayer.y = 0f;

        // normaliza direção
        directionToPlayer.Normalize();

        // olha sempre para o player
        LookAtPlayer();

        // atira se estiver perto
        if (distance <= shootDistance)
        {
            Shoot();
        }

        // limita quantas vezes o NavMesh
        // recalcula rota
        if (Time.time < nextPathUpdate)
        {
            return;
        }

        // define próximo recalculo
        nextPathUpdate =
            Time.time + pathUpdateRate;

        // muito longe
        if (distance > idealDistance)
        {
            MoveToPlayer();
        }

        // muito perto
        else if (
            distance < tooCloseDistance
        )
        {
            RetreatFromPlayer(
                directionToPlayer
            );
        }

        // distância ideal
        else
        {
            StrafeAroundPlayer(
                directionToPlayer
            );
        }
    }

    // aproxima do player
    void MoveToPlayer()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        agent.SetDestination(
            player.transform.position
        );
    }

    // recua do player
    void RetreatFromPlayer(
        Vector3 directionToPlayer
    )
    {
        // calcula posição de recuo
        Vector3 retreatPosition =
            transform.position -
            directionToPlayer *
            retreatDistance;

        NavMeshHit hit;

        // procura posição válida
        if (
            NavMesh.SamplePosition(
                retreatPosition,
                out hit,
                4f,
                NavMesh.AllAreas
            )
        )
        {
            agent.SetDestination(
                hit.position
            );
        }
    }

    // circula ao redor do player
    void StrafeAroundPlayer(
        Vector3 directionToPlayer
    )
    {
        // troca direção lateral
        if (
            Time.time >=
            nextStrafeTime
        )
        {
            nextStrafeTime =
                Time.time +
                strafeChangeTime;

            strafeDirection =
                Random.Range(0, 2) == 0
                ? -1
                : 1;
        }

        // calcula direção lateral
        Vector3 sideDirection =
            Vector3.Cross(
                Vector3.up,
                directionToPlayer
            ) * strafeDirection;

        // calcula posição lateral
        Vector3 strafePosition =
            transform.position +
            sideDirection *
            strafeDistance;

        NavMeshHit hit;

        // procura posição válida
        if (
            NavMesh.SamplePosition(
                strafePosition,
                out hit,
                3f,
                NavMesh.AllAreas
            )
        )
        {
            agent.SetDestination(
                hit.position
            );
        }
    }

    // gira olhando para o player
    void LookAtPlayer()
    {
        Vector3 direction =
            player.transform.position -
            transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }

    // controla disparo
    void Shoot()
    {
        // verifica cooldown
        if (
            Time.time <
            nextFireTime
        )
        {
            return;
        }

        // define próximo tiro
        nextFireTime =
            Time.time + fireRate;

        // evita erro
        if (
            bulletPrefab == null ||
            firePoint == null
        )
        {
            return;
        }

        // cria projétil
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );
    }

    // inicia empurrão
    public void PushAwayFrom(
        Vector3 originPosition
    )
    {
        // calcula direção
        pushDirection =
            transform.position -
            originPosition;

        // remove altura
        pushDirection.y = 0f;

        if (pushDirection == Vector3.zero)
        {
            return;
        }

        // normaliza direção
        pushDirection.Normalize();

        // configura empurrão
        pushTimer = pushDuration;

        pushSpeed =
            pushDistance /
            pushDuration;

        isBeingPushed = true;

        // limpa caminho atual
        if (
            agent != null &&
            agent.isOnNavMesh
        )
        {
            agent.ResetPath();
        }
    }

    // executa movimento do empurrão
    void PushMovement()
    {
        // reduz tempo restante
        pushTimer -= Time.deltaTime;

        // move suavemente
        if (
            agent != null &&
            agent.isOnNavMesh
        )
        {
            agent.Move(
                pushDirection *
                pushSpeed *
                Time.deltaTime
            );
        }

        // termina empurrão
        if (pushTimer <= 0f)
        {
            isBeingPushed = false;
        }
    }
}