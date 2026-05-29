using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    // Referência ao Transform do player para aplicar a rotação horizontal
    public Transform player;

    // Velocidade de resposta do mouse (ajustável no Inspector)
    public float mouseSensitivity = 2f;

    // Ângulo vertical atual da câmera em graus
    private float xRotation = 0f;

    // Acumulador do input horizontal entre Update e FixedUpdate
    private float accumulatedMouseX = 0f;

    // Rigidbody do player para rotacionar sem conflitar com a física
    private Rigidbody playerRb;

    void Start()
    {
        // Obtém o Rigidbody do player para usar MoveRotation no FixedUpdate
        playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Pausa o input se o jogo estiver pausado
        if (Time.timeScale == 0f) return;

        // Acumula o movimento horizontal do mouse entre frames
        accumulatedMouseX += Input.GetAxisRaw("Mouse X") * mouseSensitivity;

        // Captura o movimento vertical do mouse
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Subtrai para inverter o eixo Y (mover mouse para cima olha para cima)
        xRotation -= mouseY;

        // Limita a rotação vertical entre -35 (acima) e 60 graus (abaixo)
        xRotation = Mathf.Clamp(xRotation, -35f, 60f);

        // Aplica a rotação vertical no Transform da câmera (não envolve Rigidbody)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void FixedUpdate()
    {
        // Evita chamada desnecessária se não houve input horizontal
        if (accumulatedMouseX == 0f) return;

        // Rotaciona o Rigidbody horizontalmente sem conflitar com a física
        playerRb.MoveRotation(
            playerRb.rotation * Quaternion.Euler(0f, accumulatedMouseX, 0f)
        );

        // Reseta o acumulado após aplicar a rotação
        accumulatedMouseX = 0f;
    }
}