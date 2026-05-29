using UnityEngine;
using UnityEngine.InputSystem;

// controla ataque do player
public class PlayerAttack : MonoBehaviour
{
    // referência da espada
    public Sword sword;

    // executa ao iniciar o jogo
    void Start()
    {
        // pega automaticamente a espada dentro do player
        sword = GetComponentInChildren<Sword>();
    }

    // executa a cada frame
    void Update()
    {
        // verifica clique esquerdo do mouse
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // mostra mensagem no console
            Debug.Log("Attack");

            // executa ataque da espada
            sword.Attack();
        }
    }
}