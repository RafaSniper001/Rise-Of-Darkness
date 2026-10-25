using UnityEngine;

// fica no modelo do player (Visual), junto do Animator:
// controla os parâmetros da máquina de estados e recebe os Animation Events.
// Camada 0 (corpo inteiro): andar/correr, queda, aterrissagem e morte.
// Camada 1 (UpperBody, só tronco e braços): ataque em duas partes e poder.
public class PlayerAnimator : MonoBehaviour
{
    enum Action { None, Attack1, Attack2, Power }

    public Animator animator;

    [Header("Parâmetros do Animator")]
    public string speedParameter = "Speed";
    public string groundedParameter = "Grounded";
    public string attackTrigger = "Attack";
    public string comboTrigger = "Combo";
    public string powerTrigger = "Power";
    public string deadParameter = "Dead";
    public string powerSpeedParameter = "PowerSpeed";

    [Header("Poder")]
    // velocidade da animação de poder até a mão chegar no alto (explosão)
    public float powerRiseSpeed = 2.5f;
    // velocidade do resto da animação, depois da explosão
    public float powerSpeed = 1.5f;

    [Header("Camadas")]
    public int upperBodyLayer = 1;

    [Header("Ataque")]
    // tempo mínimo entre o início de dois ataques: o golpe original do sabre
    // durava 1 s e ignorava cliques nesse tempo (mantém o mesmo dano por segundo)
    public float attackInterval = 1f;

    [Header("Rotação do modelo")]
    public float turnSpeed = 12f;

    [Header("Pés")]
    // graus que cada pé gira em torno da canela (positivo = ponta para dentro).
    // Corrige o pé que o retarget deixa virado para fora; o Setup Player Rig preenche
    // (um valor para parado/andando e outro para correndo, misturados pela velocidade).
    // Em torno da canela, e não do eixo vertical, para o pé não ficar de lado
    // quando sobe atrás na passada
    public float leftFootYaw = 0f;
    public float rightFootYaw = 0f;
    public float leftFootYawRun = 0f;
    public float rightFootYawRun = 0f;
    // queda e pouso: o retarget dessas animações deixava o pé torcido; nelas cada pé
    // volta à pose natural do modelo em relação à canela (a pose do prefab). Tempo para
    // entrar nessa pose ao sair do chão e para sair dela no fim do pouso
    public float airFootBlendTime = 0.15f;

    [Header("Queda")]
    // tempo fora do chão antes de trocar para a animação de queda
    public float airborneDelay = 0.15f;

    [Header("Morte")]
    // duração da animação de morte (preenchida pelo Setup Player Rig)
    public float deathDuration = 2f;

    Rigidbody rb;
    PlayerMovement playerMovement;
    PlayerPower playerPower;
    Sword sword;

    Action action = Action.None;
    bool comboQueued = false;
    float airTime = 0f;
    float lastAttackTime = float.NegativeInfinity;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // antes da primeira animação: os ossos ainda estão na pose do prefab
        if (animator != null && animator.isHuman)
            feetRest.Capture(animator);

        rb = GetComponentInParent<Rigidbody>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerPower = GetComponentInParent<PlayerPower>();
        sword = transform.parent != null
            ? transform.parent.GetComponentInChildren<Sword>()
            : null;
    }

    void Update()
    {
        if (animator == null || rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;

        animator.SetFloat(
            speedParameter,
            GetSpeedLevel(velocity.magnitude),
            0.1f,
            Time.deltaTime
        );

        UpdateGrounded();
        FaceMoveDirection(velocity);
    }

    // depois da animação: corrige o pé virado para fora do modelo
    void LateUpdate()
    {
        ApplyFootTurnIn();
    }

    // gira cada pé em torno da canela (só a direção da ponta do pé,
    // sem mudar a inclinação do pé durante a passada)
    public void ApplyFootTurnIn()
    {
        if (animator == null || !animator.isHuman)
            return;

        // Speed: 1 = andando, 2 = correndo
        float run = Mathf.Clamp01(animator.GetFloat(speedParameter) - 1f);
        TurnFoot(animator, HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg, Mathf.Lerp(leftFootYaw, leftFootYawRun, run));
        TurnFoot(animator, HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg, Mathf.Lerp(rightFootYaw, rightFootYawRun, run));

        float air = AirWeight(animator, groundedParameter, ref airFootWeight, airFootBlendTime);
        feetRest.Apply(animator, air);
        swordGrip.Update(animator, air);
    }

    float airFootWeight;
    FootRest feetRest;
    HandGrip swordGrip;

    // mão do sabre: as animações de queda e pouso (sem arma) abrem a mão. No chão guarda como
    // os dedos da mão direita estão fechados no cabo; na queda e no pouso mantém essa pegada
    public struct HandGrip
    {
        static readonly HumanBodyBones[] Fingers =
        {
            HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
            HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal,
        };

        Quaternion[] grip;
        bool recorded;

        // weight 0 (no chão): guarda a pegada; acima disso puxa os dedos para ela
        public void Update(Animator animator, float weight)
        {
            if (grip == null)
                grip = new Quaternion[Fingers.Length];

            if (weight > 0f && !recorded)
                return;

            for (int i = 0; i < Fingers.Length; i++)
            {
                Transform bone = animator.GetBoneTransform(Fingers[i]);
                if (bone == null)
                    continue;

                if (weight <= 0f)
                    grip[i] = bone.localRotation;
                else
                    bone.localRotation = Quaternion.Slerp(bone.localRotation, grip[i], weight);
            }

            if (weight <= 0f)
                recorded = true;
        }
    }

    // pose natural de cada pé em relação à canela (a do prefab, antes da primeira animação)
    public struct FootRest
    {
        Quaternion left, right;
        bool captured;

        public void Capture(Animator animator)
        {
            Transform l = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform r = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (l == null || r == null)
                return;

            left = l.localRotation;
            right = r.localRotation;
            captured = true;
        }

        // weight 1: pé na pose natural; 0: pé como a animação deixou
        public void Apply(Animator animator, float weight)
        {
            if (!captured || weight <= 0f)
                return;

            Transform l = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform r = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            l.localRotation = Quaternion.Slerp(l.localRotation, left, weight);
            r.localRotation = Quaternion.Slerp(r.localRotation, right, weight);
        }
    }

    // 1 no ar (parâmetro Grounded falso, animação de queda) e durante a animação de pouso,
    // 0 no resto, com transição suave (também usado pelo BossAnimator)
    public static float AirWeight(Animator animator, string groundedParameter, ref float weight, float blendTime)
    {
        bool landing =
            animator.GetCurrentAnimatorStateInfo(0).IsName("Landing") ||
            (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Landing"));

        float target = !animator.GetBool(groundedParameter) || landing ? 1f : 0f;
        weight = Mathf.MoveTowards(weight, target, Time.deltaTime / Mathf.Max(blendTime, 0.01f));
        return weight;
    }

    // também usado pelo inimigo mago (BossAnimator), que tem o mesmo modelo
    public static void TurnFoot(Animator animator, HumanBodyBones foot, HumanBodyBones lowerLeg, float angle)
    {
        if (angle == 0f)
            return;

        Transform footT = animator.GetBoneTransform(foot);
        Transform legT = animator.GetBoneTransform(lowerLeg);

        if (footT == null || legT == null)
            return;

        // eixo da canela, do pé para o joelho (com a perna em pé, é o eixo vertical)
        Vector3 shin = (legT.position - footT.position).normalized;
        footT.rotation = Quaternion.AngleAxis(angle, shin) * footT.rotation;
    }

    // 0 = parado, 1 = andando, 2 = correndo
    float GetSpeedLevel(float speed)
    {
        if (speed < 0.1f)
            return 0f;

        if (playerMovement == null)
            return 1f;

        float runLimit =
            (playerMovement.moveSpeed +
             playerMovement.sprintSpeed) * 0.5f;

        return speed > runLimit ? 2f : 1f;
    }

    void UpdateGrounded()
    {
        bool grounded =
            playerMovement == null ||
            playerMovement.IsGrounded;

        // tocou o chão depois de estar no ar: som de pouso
        if (grounded && airTime >= airborneDelay && playerMovement != null)
            playerMovement.PlayLanding();

        airTime = grounded ? 0f : airTime + Time.deltaTime;

        animator.SetBool(
            groundedParameter,
            airTime < airborneDelay
        );
    }

    // o corpo (root) gira com o mouse; o modelo gira para o lado em que anda
    // e volta para a frente durante ataques
    void FaceMoveDirection(Vector3 velocity)
    {
        Quaternion target = transform.parent.rotation;

        if (action == Action.None && velocity.sqrMagnitude > 0.01f)
            target = Quaternion.LookRotation(velocity.normalized, Vector3.up);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                target,
                turnSpeed * Time.deltaTime
            );
    }

    public bool IsBusy()
    {
        return action != Action.None;
    }

    // 1 clique: primeira parte do ataque
    // 2º clique durante a primeira parte: continua para a animação inteira
    public bool RequestAttack()
    {
        if (animator == null)
            return false;

        if (action == Action.None)
        {
            if (Time.time < lastAttackTime + attackInterval)
                return false;

            lastAttackTime = Time.time;
            action = Action.Attack1;
            comboQueued = false;
            animator.SetTrigger(attackTrigger);
            return true;
        }

        if (action == Action.Attack1)
        {
            comboQueued = true;
            return true;
        }

        return false;
    }

    // o poder interrompe o golpe de sabre em andamento: fecha o dano do sabre, descarta o
    // combo e passa direto para a animação do poder (a transição do Animator só sai do Empty)
    public bool TryPlayPower()
    {
        if (animator == null || action == Action.Power)
            return false;

        bool interrupting = action != Action.None;

        action = Action.Power;
        comboQueued = false;
        AttackEnd();
        animator.ResetTrigger(attackTrigger);
        animator.ResetTrigger(comboTrigger);
        animator.SetFloat(powerSpeedParameter, powerRiseSpeed);

        if (interrupting)
        {
            animator.ResetTrigger(powerTrigger);
            animator.CrossFadeInFixedTime("Power", 0.05f, upperBodyLayer, 0f);
        }
        else
        {
            animator.SetTrigger(powerTrigger);
        }

        return true;
    }

    public void SetDead(bool dead)
    {
        if (animator == null)
            return;

        animator.SetBool(deadParameter, dead);

        // a morte é do corpo inteiro: desliga a camada de cima
        animator.SetLayerWeight(upperBodyLayer, dead ? 0f : 1f);

        action = Action.None;
        comboQueued = false;
        animator.ResetTrigger(attackTrigger);
        animator.ResetTrigger(comboTrigger);
        animator.ResetTrigger(powerTrigger);

        if (sword != null)
            sword.CloseDamageWindow();

        if (!dead)
        {
            // renasce no chão: sem queda nem som de pouso
            airTime = 0f;
            animator.SetBool(groundedParameter, true);
            animator.Play("Locomotion", 0, 0f);
            animator.Play("Empty", upperBodyLayer, 0f);
        }
    }

    // ---------- Animation Events ----------

    // ataque com o sabre: abre a janela de dano
    public void AttackStart()
    {
        if (sword != null)
            sword.OpenDamageWindow();
    }

    // ataque com o sabre: fecha a janela de dano
    public void AttackEnd()
    {
        if (sword != null)
            sword.CloseDamageWindow();
    }

    // som do sabre alinhado com o golpe
    public void SwingSound()
    {
        if (sword != null)
            sword.PlaySwingSound();
    }

    // fim da primeira parte do ataque: segue o combo se houve 2º clique
    public void ComboCheck()
    {
        if (action != Action.Attack1)
            return;

        if (comboQueued)
        {
            comboQueued = false;
            action = Action.Attack2;
            animator.SetTrigger(comboTrigger);
        }
        else
        {
            ActionEnd();
        }
    }

    // magic attack: momento em que o poder é liberado
    public void PowerRelease()
    {
        animator.SetFloat(powerSpeedParameter, powerSpeed);

        if (playerPower != null)
            playerPower.ReleasePower();
    }

    // fim do ataque ou do poder: volta a agir livremente
    // (o dano do sabre só fecha no AttackEnd, no fim da animação do golpe)
    public void ActionEnd()
    {
        action = Action.None;
        comboQueued = false;
    }

    // batida de cada pé nas animações de andar e correr (0 = esquerdo, 1 = direito)
    public void Footstep(int foot)
    {
        if (playerMovement != null)
            playerMovement.PlayFootstep(foot);
    }
}
