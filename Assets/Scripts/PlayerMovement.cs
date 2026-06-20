using UnityEngine;

// controla a movimentação do player
public class PlayerMovement : MonoBehaviour
{
    // velocidade normal
    public float moveSpeed = 8f;

    // velocidade correndo
    public float sprintSpeed = 14f;

    // stamina máxima
    public float maxStamina = 100f;

    // stamina atual
    public float currentStamina = 100f;

    // quanto perde correndo
    public float staminaDrain = 25f;

    // quanto recupera quando não está correndo
    public float staminaRecovery = 15f;

    // referência da câmera
    public Transform cameraTransform;

    // rigidbody do player
    Rigidbody rb;

    // controla se o player está exausto
    bool exhausted = false;

    // Audio Source responsável pelos sons de movimento
    public AudioSource audioSource;

    // lista com os 8 sons de corrida
    public AudioClip[] footstepSounds;

    // intervalo entre os passos andando
    public float walkStepInterval = 0.5f;

    // intervalo entre os passos correndo
    public float sprintStepInterval = 0.3f;

    // controla quando tocar o próximo passo
    float stepTimer = 0f;

    // executa ao iniciar a cena
    void Start()
    {
        // pega o Rigidbody do player
        rb = GetComponent<Rigidbody>();

        // começa com stamina cheia
        currentStamina = maxStamina;

        // trava o mouse no centro da tela durante o jogo
        Cursor.lockState = CursorLockMode.Locked;

        // deixa o cursor invisível durante o jogo
        Cursor.visible = false;

        // garante que o jogo esteja rodando em velocidade normal
        Time.timeScale = 1f;
    }

    // executa em intervalo fixo, ideal para física
    void FixedUpdate()
    {
        // pega input horizontal: A e D
        float horizontal = Input.GetAxisRaw("Horizontal");

        // pega input vertical: W e S
        float vertical = Input.GetAxisRaw("Vertical");

        // pega a direção para frente da câmera
        Vector3 forward = cameraTransform.forward;

        // pega a direção lateral da câmera
        Vector3 right = cameraTransform.right;

        // remove inclinação vertical da direção para frente
        forward.y = 0f;

        // remove inclinação vertical da direção lateral
        right.y = 0f;

        // normaliza para evitar velocidade maior na diagonal
        forward.Normalize();
        right.Normalize();

        // calcula a direção final do movimento
        Vector3 moveDirection = forward * vertical + right * horizontal;

        // verifica se o player está tentando se mover
        bool isMoving = moveDirection.magnitude > 0f;

        // controla os sons de passos
        if (isMoving)
        {
            // escolhe o intervalo baseado na velocidade
            float currentInterval = Input.GetKey(KeyCode.LeftShift)
                && currentStamina > 0f
                && !exhausted
                ? sprintStepInterval
                : walkStepInterval;

            // diminui o contador
            stepTimer -= Time.fixedDeltaTime;

            // hora de tocar um novo passo
            if (stepTimer <= 0f)
            {
                // evita erro caso nenhum som tenha sido configurado
                if (footstepSounds.Length > 0)
                {
                    // escolhe um dos 8 sons aleatoriamente
                    int randomIndex = Random.Range(0, footstepSounds.Length);

                    // toca o som
                    audioSource.PlayOneShot(footstepSounds[randomIndex]);
                }

                // reinicia o contador
                stepTimer = currentInterval;
            }
        }
        else
        {
            // quando parar de andar, reinicia o contador
            stepTimer = 0f;
        }

        // verifica se pode correr
        bool isSprinting =
            Input.GetKey(KeyCode.LeftShift) &&
            currentStamina > 0f &&
            isMoving &&
            !exhausted;

        // define a velocidade atual
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // se estiver correndo
        if (isSprinting)
        {
            // gasta stamina
            currentStamina -= staminaDrain * Time.fixedDeltaTime;

            // se a stamina acabar
            if (currentStamina <= 0f)
            {
                // trava em zero
                currentStamina = 0f;

                // entra em estado de exaustão
                exhausted = true;
            }
        }
        else
        {
            // recupera stamina
            currentStamina += staminaRecovery * Time.fixedDeltaTime;

            // limita no máximo
            if (currentStamina >= maxStamina)
            {
                // trava no máximo
                currentStamina = maxStamina;

                // remove exaustão
                exhausted = false;
            }
        }

        // calcula a velocidade final
        Vector3 velocity = moveDirection.normalized * currentSpeed;

        // aplica a velocidade no Rigidbody
        rb.linearVelocity = new Vector3(
            velocity.x,
            rb.linearVelocity.y,
            velocity.z
        );
    }

    // quando o objeto for desativado ou destruído
    void OnDisable()
    {
        // solta o mouse
        Cursor.lockState = CursorLockMode.None;

        // mostra o cursor
        Cursor.visible = true;
    }
}