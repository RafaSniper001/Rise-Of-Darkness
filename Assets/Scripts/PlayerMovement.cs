using UnityEngine;

// controla a movimentação do player
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float sprintSpeed = 14f;

    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaDrain = 25f;
    public float staminaRecovery = 15f;

    public Transform cameraTransform;

    Rigidbody rb;

    bool exhausted = false;

    public AudioSource audioSource;

    // cada pé tem seus sons: se os dois baterem juntos, os dois tocam
    public AudioClip[] leftFootSounds;
    public AudioClip[] rightFootSounds;

    // evita som dobrado do mesmo pé quando andar e correr se misturam no blend
    public float minStepInterval = 0.15f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    // folga (em unidades locais) para continuar "no chão" em rampas íngremes
    public float slopeGroundTolerance = 0.35f;

    // volume extra do som de pouso (os dois pés juntos)
    public float landingVolume = 1.3f;

    public bool IsGrounded { get; private set; }

    bool canStep = false;
    float[] lastStepTime = { -1f, -1f };

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (GameSaveManager.ShouldLoadSavedGame &&
            GameSaveManager.HasSave())
        {
            currentStamina =
                GameSaveManager.GetStamina(maxStamina);
        }
        else if (GameManager.instance != null &&
                 GameManager.instance.HasSavedPlayerState())
        {
            currentStamina =
                GameManager.instance.savedStamina;
        }
        else
        {
            currentStamina = maxStamina;
        }

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );

        exhausted = currentStamina <= 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }

    void FixedUpdate()
    {
        bool isGrounded = CheckGround();
        IsGrounded = isGrounded;

        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector3 forward =
            cameraTransform.forward;

        Vector3 right =
            cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection =
            forward * vertical +
            right * horizontal;

        bool isMoving =
            moveDirection.magnitude > 0f;

        // os passos tocam pelo Animation Event Footstep (PlayFootstep)
        canStep = isMoving && isGrounded;

        bool isSprinting =
            Input.GetKey(KeyCode.LeftShift) &&
            currentStamina > 0f &&
            isMoving &&
            !exhausted;

        float currentSpeed =
            isSprinting
                ? sprintSpeed
                : moveSpeed;

        if (isSprinting)
        {
            currentStamina -=
                staminaDrain *
                Time.fixedDeltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                exhausted = true;
            }
        }
        else
        {
            currentStamina +=
                staminaRecovery *
                Time.fixedDeltaTime;

            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                exhausted = false;
            }
        }

        Vector3 velocity =
            moveDirection.normalized *
            currentSpeed;

        rb.linearVelocity = new Vector3(
            velocity.x,
            rb.linearVelocity.y,
            velocity.z
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
        return !col.CompareTag("Player") &&
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
        float radius = capsule.radius * scale * 0.9f;
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

    // atualiza o "no chão" na hora (ex.: depois de renascer, com o script ainda travado)
    public void RefreshGround()
    {
        IsGrounded = CheckGround();
    }

    // posição com a base da cápsula apoiada no chão logo abaixo
    // (checkpoints mais altos que o chão faziam o player renascer caindo)
    public Vector3 GetGroundedPosition(Vector3 position)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        if (capsule == null)
            return position;

        float bottom =
            transform.TransformPoint(
                capsule.center - Vector3.up * capsule.height * 0.5f
            ).y - transform.position.y;

        Vector3 origin =
            position + Vector3.up * capsule.height * transform.lossyScale.y * 0.5f;

        float ground = float.NaN;
        float closest = float.MaxValue;

        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 20f))
        {
            // ignora o que se move pela física (BB-8, inimigos): só chão de verdade
            Rigidbody body = hit.collider.attachedRigidbody;

            if (body != null && !body.isKinematic)
                continue;

            if (IsGround(hit.collider) && hit.distance < closest)
            {
                closest = hit.distance;
                ground = hit.point.y;
            }
        }

        if (float.IsNaN(ground))
            return position;

        position.y = ground - bottom + 0.01f;
        return position;
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

        int randomIndex =
            Random.Range(0, sounds.Length);

        audioSource.PlayOneShot(
            sounds[randomIndex]
        );
    }

    // pouso depois de cair/pular: os dois pés batem juntos
    public void PlayLanding()
    {
        if (audioSource == null)
            return;

        foreach (AudioClip[] sounds in new[] { leftFootSounds, rightFootSounds })
        {
            if (sounds == null || sounds.Length == 0)
                continue;

            audioSource.PlayOneShot(
                sounds[Random.Range(0, sounds.Length)],
                landingVolume
            );
        }

        lastStepTime[0] = lastStepTime[1] = Time.time;
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        exhausted = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}