using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (animator == null || playerMovement == null) return;

        float speed = 0f;

        if (playerMovement.IsMoving())
        {
            speed = playerMovement.IsSpeedBoosted() ? 2f : 1f;
        }

        animator.SetFloat("Speed", speed);
    }
}