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
    public AudioClip[] footstepSounds;

    public float walkStepInterval = 0.5f;
    public float sprintStepInterval = 0.3f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    float stepTimer = 0f;

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

        if (isMoving && isGrounded)
        {
            float currentInterval =
                Input.GetKey(KeyCode.LeftShift) &&
                currentStamina > 0f &&
                !exhausted
                    ? sprintStepInterval
                    : walkStepInterval;

            stepTimer -= Time.fixedDeltaTime;

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

                stepTimer = currentInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }

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
            if (col.CompareTag("Player"))
                continue;

            if (col.transform.IsChildOf(transform))
                continue;

            if (col.isTrigger)
                continue;

            return true;
        }

        return false;
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