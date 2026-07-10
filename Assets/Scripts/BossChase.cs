using System.Collections;
using UnityEngine;

public class BossChase : MonoBehaviour
{
    public Transform player;

    public float moveSpeed = 4f;
    public float stopDistance = 2.2f;

    // sons de passos
    public AudioSource audioSource;
    public AudioClip[] footstepSounds;

    // intervalo entre passos
    public float stepInterval = 0.5f;

    // verificação de chão
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    float stepTimer = 0f;

    // controla se está sendo empurrado
    bool isBeingPushed = false;

    void Start()
    {
        if (player == null)
        {
            GameObject foundPlayer =
                GameObject.FindGameObjectWithTag("Player");

            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }
    }

    void Update()
    {
        if (player == null)
            return;

        // enquanto está sendo empurrado,
        // não persegue o player
        if (isBeingPushed)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        transform.LookAt(
            new Vector3(
                player.position.x,
                transform.position.y,
                player.position.z
            )
        );

        bool isMoving =
            distance > stopDistance;

        if (isMoving)
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    player.position,
                    moveSpeed * Time.deltaTime
                );
        }

        bool isGrounded = CheckGround();

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;

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

                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
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
            // ignora tag Enemy
            if (col.CompareTag("Enemy"))
                continue;

            // ignora filhos do próprio boss
            if (col.transform.IsChildOf(transform))
                continue;

            // ignora triggers
            if (col.isTrigger)
                continue;

            return true;
        }

        return false;
    }

    public void PushAwayFrom(
        Vector3 sourcePosition,
        float pushDistance,
        float pushDuration
    )
    {
        StopCoroutine(nameof(PushRoutine));

        StartCoroutine(
            PushRoutine(
                sourcePosition,
                pushDistance,
                pushDuration
            )
        );
    }

    IEnumerator PushRoutine(
        Vector3 sourcePosition,
        float pushDistance,
        float pushDuration
    )
    {
        isBeingPushed = true;

        Vector3 direction =
            transform.position - sourcePosition;

        direction.y = 0f;
        direction.Normalize();

        Vector3 startPosition =
            transform.position;

        Vector3 targetPosition =
            startPosition +
            direction * pushDistance;

        float elapsed = 0f;

        while (elapsed < pushDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                elapsed / pushDuration;

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

            yield return null;
        }

        transform.position = targetPosition;

        isBeingPushed = false;
    }
}