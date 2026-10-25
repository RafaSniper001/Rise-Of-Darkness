using UnityEngine;
using UnityEngine.AI;

// fica no modelo do robô (Visual), junto do Animator (como o EnemyAnimator do conteúdo):
// SetSpeed passa a velocidade do NavMeshAgent para o parâmetro "Speed".
// Camada 0: parado / andar / correr (para frente e para trás).
// Camada 1 (UpperBody): mira com o player ao alcance e recuo a cada disparo do Chase.
// A arma fica presa ao corpo e as mãos vão até ela por IK.
public class EnemyAnimator : MonoBehaviour
{
    public Animator animator;

    [Header("Parâmetros do Animator")]
    public string speedParameter = "Speed";
    public string moveSpeedParameter = "MoveSpeed";
    public string shootTrigger = "Shoot";
    public string aimingParameter = "Aiming";

    [Header("Movimento")]
    // velocidade em que a corrida acompanha os pés (preenchida pelo Setup Robots);
    // mais rápido que isso, a animação acelera junto
    public float runAnimationSpeed = 5.5f;
    public float turnSpeed = 10f;
    // recuando (andando para longe do player) olha para ele e anda de costas
    public float backwardAngle = 120f;

    [Header("Arma")]
    // a arma fica presa ao corpo (este Visual), com a coronha no ombro esquerdo, e as mãos vão até ela por IK
    // (esquerda no gatilho, direita no cano). Duas posições, preenchidas pelo Setup Robots:
    // mirando (cano para a frente) e pronta (cano para baixo e para dentro)
    public Transform weapon;
    public Vector3 weaponAimPosition;
    public Quaternion weaponAimRotation = Quaternion.identity;
    public Vector3 weaponReadyPosition;
    public Quaternion weaponReadyRotation = Quaternion.identity;
    public float aimRaiseSpeed = 3f;
    public Transform leftHandGrip;
    public Transform rightHandGrip;
    // cotovelos (no espaço do corpo), tirados da pose de mira da animação
    public Vector3 leftElbowHint;
    public Vector3 rightElbowHint;
    // dedos com pose própria (preenchidos pelo Setup Robots): os da mão direita fechados em volta
    // do cano (nas animações deste robô eles dobram para as costas da mão) e o indicador
    // esquerdo no gatilho. Giro local de cada falange
    public Transform[] fingers;
    public Quaternion[] fingerRotations;

    [Header("Chão")]
    // a sola do pé fica na origem do Visual (alinhado pelo Setup Robots): o Visual sobe ou desce
    // até o chão de verdade, porque a altura em que o NavMeshAgent põe o robô depende do
    // Base Offset, da escala e da malha do NavMesh (que fica um pouco acima ou abaixo do piso)
    public float groundSnapSpeed = 12f;
    public float maxGroundOffset = 1.5f;

    [Header("Recuo do tiro")]
    // tranco da arma para trás a cada disparo (no espaço do corpo)
    public Vector3 recoilKick;
    public float recoilTime = 0.15f;

    [Header("Mira")]
    // o tronco gira para o player enquanto anda de lado
    [Range(0f, 1f)]
    public float aimBodyWeight = 0.7f;
    // folga além do alcance de tiro do Chase para já levantar a arma
    public float aimMargin = 1f;

    NavMeshAgent agent;
    Chase chase;
    Transform player;

    float aimBlend = 0f;
    float recoil = 0f;
    float defaultVisualY;
    float lookWeight;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        agent = GetComponentInParent<NavMeshAgent>();
        chase = GetComponentInParent<Chase>();
        defaultVisualY = transform.localPosition.y;
    }

    void Start()
    {
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer != null)
            player = foundPlayer.transform;
    }

    void Update()
    {
        if (animator == null || agent == null)
            return;

        // na patrulha a passada segue a velocidade pretendida (a mesma da perseguição), sem
        // ficar no passo lento enquanto acelera no começo de cada trecho
        bool patrolling = chase != null && !chase.IsAlerted;
        Vector3 velocity = patrolling && agent.hasPath ? agent.desiredVelocity : agent.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        Vector3 toPlayer = transform.parent.forward;

        if (player != null)
        {
            toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
        }

        // em alerta (viu o player): na patrulha anda com a arma abaixada, olhando para onde vai
        bool alerted = chase == null || chase.IsAlerted;
        lookWeight = Mathf.MoveTowards(lookWeight, alerted ? 1f : 0f, 2f * Time.deltaTime);

        // em alerta e com o player ao alcance de tiro: mantém a arma apontada (estado Aim)
        bool aiming =
            alerted &&
            player != null &&
            chase != null &&
            toPlayer.magnitude <= chase.shootDistance + aimMargin;

        animator.SetBool(aimingParameter, aiming);

        // em alerta encara o player; na patrulha, parado, mantém para onde estava olhando
        Vector3 facing = alerted ? toPlayer : Vector3.zero;

        if (aiming)
        {
            // mirando: sempre de frente para o player, com a arma na direção do tiro.
            // As pernas usam só o quanto anda para frente/para trás (de lado, quase parado)
            SetSpeed(Vector3.Dot(velocity, toPlayer.normalized));
        }
        else
        {
            // recuando: de frente para o player, andando de costas (velocidade negativa);
            // andando: vira para onde anda
            bool backward =
                alerted &&
                speed > 0.1f &&
                toPlayer.sqrMagnitude > 0.01f &&
                Vector3.Angle(velocity, toPlayer) > backwardAngle;

            SetSpeed(backward ? -speed : speed);

            if (speed > 0.1f && !backward)
                facing = velocity;
        }

        animator.SetFloat(
            moveSpeedParameter,
            Mathf.Max(1f, speed / runAnimationSpeed)
        );

        if (facing.sqrMagnitude > 0.01f)
        {
            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(facing, Vector3.up),
                    turnSpeed * Time.deltaTime
                );
        }

        UpdateWeapon(aiming);
        SnapToGround();
    }

    // pés no chão: raio para baixo a partir do centro do robô (dentro da própria cápsula,
    // que o raio ignora), sem gatilhos
    void SnapToGround()
    {
        Transform root = transform.parent;
        if (root == null)
            return;

        float defaultY = root.TransformPoint(new Vector3(0f, defaultVisualY, 0f)).y;
        float targetY = defaultY;

        if (Physics.Raycast(root.position, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            targetY = Mathf.Clamp(hit.point.y, defaultY - maxGroundOffset, defaultY + maxGroundOffset);

        Vector3 position = transform.position;
        position.y = Mathf.Lerp(position.y, targetY, groundSnapSpeed * Time.deltaTime);
        transform.position = position;
    }

    // arma entre a posição pronta e a de mira, com o tranco do tiro
    void UpdateWeapon(bool aiming)
    {
        if (weapon == null)
            return;

        aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1f : 0f, aimRaiseSpeed * Time.deltaTime);
        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime / recoilTime);

        weapon.localPosition =
            Vector3.Lerp(weaponReadyPosition, weaponAimPosition, aimBlend) +
            recoilKick * recoil;

        weapon.localRotation =
            Quaternion.Slerp(weaponReadyRotation, weaponAimRotation, aimBlend);
    }

    public void SetSpeed(float speed)
    {
        if (animator != null)
            animator.SetFloat(speedParameter, speed, 0.1f, Time.deltaTime);
    }

    // chamado pelo Chase no momento do disparo
    public void PlayShoot()
    {
        recoil = 1f;

        if (animator != null)
            animator.SetTrigger(shootTrigger);
    }

    // tronco e cabeça viram para o player
    // e as duas mãos seguram a arma em qualquer animação
    void OnAnimatorIK(int layerIndex)
    {
        if (player != null && lookWeight > 0f)
        {
            animator.SetLookAtPosition(player.position + Vector3.up);
            animator.SetLookAtWeight(lookWeight, aimBodyWeight, 1f, 0f, 0.5f);
        }

        if (weapon == null)
            return;

        Transform body = weapon.parent;
        Hold(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, leftHandGrip, body.TransformPoint(leftElbowHint));
        Hold(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, rightHandGrip, body.TransformPoint(rightElbowHint));

        // os dedos vêm das animações de rifle (já fecham em volta da arma)
    }

    // o IK leva o pulso até o ponto; o giro da mão é aplicado direto no osso (LateUpdate),
    // porque a rotação do alvo de IK do humanoide não é a rotação do osso da mão
    void Hold(AvatarIKGoal hand, AvatarIKHint elbow, Transform grip, Vector3 elbowPosition)
    {
        if (grip == null)
            return;

        animator.SetIKPosition(hand, grip.position);
        animator.SetIKPositionWeight(hand, 1f);
        animator.SetIKRotationWeight(hand, 0f);
        animator.SetIKHintPosition(elbow, elbowPosition);
        animator.SetIKHintPositionWeight(elbow, 0.7f);
    }

    void LateUpdate()
    {
        ApplyHandRotations();
    }

    // mãos giradas para segurar a arma (depois da animação e do IK)
    public void ApplyHandRotations()
    {
        if (weapon == null || animator == null)
            return;

        if (leftHandGrip != null)
            animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation = leftHandGrip.rotation;

        if (rightHandGrip != null)
            animator.GetBoneTransform(HumanBodyBones.RightHand).rotation = rightHandGrip.rotation;

        if (fingers != null)
            for (int i = 0; i < fingers.Length; i++)
                if (fingers[i] != null)
                    fingers[i].localRotation = fingerRotations[i];
    }
}
