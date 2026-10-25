using UnityEngine;

// fica no modelo do inimigo mago (Visual), junto do Animator.
// Usa a mesma máquina de estados do player (Player.controller):
// camada 0 com andar/correr, queda, aterrissagem e morte;
// camada 1 (UpperBody) com o golpe de sabre e o poder.
public class BossAnimator : MonoBehaviour
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
    public string locomotionSpeedParameter = "LocomotionSpeed";

    [Header("Corrida")]
    // velocidade em que a corrida acompanha os pés (preenchida pelo Setup Player Rig);
    // o boss se move mais rápido que isso, então a animação acelera junto
    public float runAnimationSpeed = 6.5f;
    public float maxRunAnimationScale = 2.5f;

    [Header("Camadas")]
    public int upperBodyLayer = 1;

    [Header("Ataque")]
    // chance de emendar o segundo golpe (ataque duplo, como o 2º clique do player)
    [Range(0f, 1f)]
    public float comboChance = 0.35f;

    [Header("Pés")]
    // mesma correção do player (o modelo é o mesmo): graus em torno da canela
    public float leftFootYaw = 0f;
    public float rightFootYaw = 0f;
    public float leftFootYawRun = 0f;
    public float rightFootYawRun = 0f;
    // no ar: pés na pose natural do modelo, como no player (mesmo modelo e mesma queda)
    public float airFootBlendTime = 0.15f;
    float airFootWeight;
    PlayerAnimator.FootRest feetRest;
    // mão do sabre fechada no cabo também na queda e no pouso
    PlayerAnimator.HandGrip swordGrip;

    [Header("Queda")]
    public float airborneDelay = 0.15f;

    [Header("Morte")]
    // duração da animação de morte (preenchida pelo Setup Player Rig)
    public float deathDuration = 2f;

    [Header("Poder")]
    public float powerRiseSpeed = 2.5f;
    public float powerSpeed = 1.5f;

    BossChase bossChase;
    BossSword bossSword;
    BossAura bossAura;

    Action action = Action.None;
    bool comboQueued = false;
    float airTime = 0f;
    Vector3 lastPosition;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // antes da primeira animação: os ossos ainda estão na pose do prefab
        if (animator != null && animator.isHuman)
            feetRest.Capture(animator);

        bossChase = GetComponentInParent<BossChase>();
        bossAura = GetComponentInParent<BossAura>();
        bossSword = transform.parent != null
            ? transform.parent.GetComponentInChildren<BossSword>()
            : null;

        lastPosition = transform.parent.position;
    }

    void Update()
    {
        if (animator == null || Time.deltaTime <= 0f)
            return;

        // o boss anda movendo o transform: velocidade pelo deslocamento,
        // só para a frente (empurrado para trás não "corre de costas")
        Vector3 delta = transform.parent.position - lastPosition;
        lastPosition = transform.parent.position;
        delta.y = 0f;

        float speed =
            Mathf.Max(0f, Vector3.Dot(delta, transform.parent.forward)) /
            Time.deltaTime;

        animator.SetFloat(
            speedParameter,
            GetSpeedLevel(speed),
            0.1f,
            Time.deltaTime
        );

        // pés acompanham o chão: com a aura carregada a corrida acelera ainda mais
        // (andando, o passo fica no ritmo normal)
        animator.SetFloat(
            locomotionSpeedParameter,
            IsWalking() ? 1f : Mathf.Clamp(speed / runAnimationSpeed, 1f, maxRunAnimationScale),
            0.2f,
            Time.deltaTime
        );

        UpdateGrounded();
    }

    void LateUpdate()
    {
        if (animator == null || !animator.isHuman)
            return;

        float run = Mathf.Clamp01(animator.GetFloat(speedParameter) - 1f);
        PlayerAnimator.TurnFoot(animator, HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg, Mathf.Lerp(leftFootYaw, leftFootYawRun, run));
        PlayerAnimator.TurnFoot(animator, HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg, Mathf.Lerp(rightFootYaw, rightFootYawRun, run));

        float air = PlayerAnimator.AirWeight(animator, groundedParameter, ref airFootWeight, airFootBlendTime);
        feetRest.Apply(animator, air);
        swordGrip.Update(animator, air);
    }

    // 0 = parado (Idle), 1 = andando (Walk, estado Approach da IA, chegando perto),
    // 2 = correndo (estado Chase; com a aura carregada só se move mais rápido)
    float GetSpeedLevel(float speed)
    {
        if (speed < 0.1f)
            return 0f;

        return IsWalking() ? 1f : 2f;
    }

    bool IsWalking()
    {
        return bossChase != null && bossChase.IsWalking;
    }

    void UpdateGrounded()
    {
        bool grounded =
            bossChase == null ||
            bossChase.IsGrounded;

        airTime = grounded ? 0f : airTime + Time.deltaTime;

        animator.SetBool(
            groundedParameter,
            airTime < airborneDelay
        );
    }

    public bool IsBusy()
    {
        return action != Action.None;
    }

    // primeira parte do ataque; às vezes emenda a segunda (ataque duplo).
    // O BossAttack não ataca de novo enquanto o duplo não termina,
    // então o dano por segundo continua o mesmo de antes
    public bool RequestAttack()
    {
        if (animator == null || action != Action.None)
            return false;

        action = Action.Attack1;
        comboQueued = Random.value < comboChance;
        animator.SetTrigger(attackTrigger);
        return true;
    }

    public bool TryPlayPower()
    {
        if (animator == null || action != Action.None)
            return false;

        action = Action.Power;
        AttackEnd();
        animator.SetFloat(powerSpeedParameter, powerRiseSpeed);
        animator.SetTrigger(powerTrigger);
        return true;
    }

    public void SetDead()
    {
        if (animator == null)
            return;

        animator.SetBool(deadParameter, true);
        animator.SetLayerWeight(upperBodyLayer, 0f);

        action = Action.None;
        comboQueued = false;
        animator.ResetTrigger(attackTrigger);
        animator.ResetTrigger(comboTrigger);
        animator.ResetTrigger(powerTrigger);

        if (bossSword != null)
            bossSword.CloseDamageWindow();
    }

    // ---------- Animation Events ----------

    public void AttackStart()
    {
        if (bossSword != null)
            bossSword.OpenDamageWindow();
    }

    public void AttackEnd()
    {
        if (bossSword != null)
            bossSword.CloseDamageWindow();
    }

    public void SwingSound()
    {
        if (bossSword != null)
            bossSword.PlaySwingSound();
    }

    // fim da primeira parte: segue para o segundo golpe se sorteou o ataque duplo
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

    // mão no alto: explode a aura
    public void PowerRelease()
    {
        animator.SetFloat(powerSpeedParameter, powerSpeed);

        if (bossAura != null)
            bossAura.ReleasePower();
    }

    // (o dano do sabre só fecha no AttackEnd, no fim da animação do golpe)
    public void ActionEnd()
    {
        action = Action.None;
        comboQueued = false;
    }

    public void Footstep(int foot)
    {
        if (bossChase != null)
            bossChase.PlayFootstep(foot);
    }
}
