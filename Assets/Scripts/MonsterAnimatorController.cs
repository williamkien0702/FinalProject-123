using UnityEngine;
using System.Collections;

public class MonsterAnimatorController : MonoBehaviour
{
    private Animator animator;
    private Monster monster;

    void Start()
    {
        animator = GetComponent<Animator>();
        monster = GetComponentInParent<Monster>();
    }

    void Update()
    {
        if (animator == null || monster == null) return;

        float speed = 0f;

        switch (monster.currentState)
        {
            case Monster.State.Patrol: speed = 0.5f; break;
            case Monster.State.Chase:
            case Monster.State.Flee:   speed = 1f;   break;
            case Monster.State.Attack: speed = 0f;   break;
        }

        animator.SetFloat("Speed", speed);

        if (monster.JustAttacked)
        {
            monster.JustAttacked = false;
            StartCoroutine(PulseAttack());
        }
    }

    // Briefly sets IsAttacking false then true so the Animator
    // sees a fresh transition each consecutive attack
    IEnumerator PulseAttack()
    {
        animator.SetBool("IsAttacking", false);
        yield return null; // Wait one frame
        animator.SetBool("IsAttacking", true);
        yield return new WaitForSeconds(0.3f); // Hold for part of the animation
        animator.SetBool("IsAttacking", false);
    }
}