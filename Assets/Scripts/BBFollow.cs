using UnityEngine;
using UnityEngine.AI;

// controla o BB8
public class BB8Follow : MonoBehaviour
{
    // referência do player
    public Transform player;

    // se o player chegar mais perto que isso, o BB8 se afasta
    public float fleeDistance = 2f;

    // distância ideal para o BB8 ficar parado
    public float stopDistance = 3f;

    // se passar dessa distância, o BB8 segue o player
    public float followDistance = 5f;

    // quanto ele tenta se afastar quando o player chega perto
    public float fleeStep = 4f;

    // velocidade da rotação do BB8
    public float rotationSpeed = 8f;

    // referência do NavMeshAgent
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        // se o player chegou perto demais, BB8 foge
        if (distance < fleeDistance)
        {
            FleeFromPlayer();
        }
        // se o player está longe demais, BB8 segue
        else if (distance > followDistance)
        {
            agent.SetDestination(player.position);
        }
        // distância segura: BB8 fica parado
        else
        {
            agent.ResetPath();
        }

        LookAtPlayer();
    }

    // faz o BB8 se afastar do player
    void FleeFromPlayer()
    {
        // direção contrária ao player
        Vector3 directionAway =
            transform.position - player.position;

        directionAway.y = 0f;
        directionAway.Normalize();

        // ponto para onde o BB8 vai fugir
        Vector3 fleeTarget =
            transform.position + directionAway * fleeStep;

        // manda o BB8 ir para esse ponto
        agent.SetDestination(fleeTarget);
    }

    // controla rotação para olhar o player
    void LookAtPlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}