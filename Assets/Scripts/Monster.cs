using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class Monster : NetworkBehaviour
{
    public enum State { Patrol, Chase, Attack, Flee }

    [Header("Movement")]
    public float patrolSpeed = 4f;
    public float chaseSpeed = 7f;
    public float fleeSpeed = 9f;

    [Header("Detection")]
    public float detectionRadius = 12f;     // How far it sees players
    public float attackRadius = 2f;         // How close to deal damage
    public float fleeRadius = 15f;          // How far it runs when shot

    [Header("Attack")]
    public int scorePenalty = 3;
    public float attackCooldown = 1.5f;     // Seconds between hits

    [Header("Patrol")]
    public float patrolRadius = 20f;        // How far from start it wanders
    public float waypointReachedDistance = 2f;

    [Header("Arena Bounds")]
    public float arenaMin = -46f;
    public float arenaMax = 46f;

    private State currentState = State.Patrol;
    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private Transform chaseTarget;
    private Vector3 fleeDirection;

    private float lastAttackTime = 0f;
    private bool isFleeing = false;
    private float fleeStartTime = 0f;
    private float fleeTimeout = 3f;  // Max seconds to spend fleeing before giving up

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        startPosition = transform.position;
        SetNewPatrolTarget();
    }

    void Update()
    {
        if (!IsServer) return;
        if (GameManager.gameOver) return;

        switch (currentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
            case State.Flee:   UpdateFlee();   break;
        }
    }

    // ─── PATROL ───────────────────────────────────────────────────────────

    void UpdatePatrol()
    {
        // Check if any player is close enough to chase
        Transform nearest = GetNearestPlayer();
        if (nearest != null && Vector3.Distance(transform.position, nearest.position) <= detectionRadius)
        {
            chaseTarget = nearest;
            TransitionTo(State.Chase);
            return;
        }

        // Move toward patrol waypoint
        MoveToward(patrolTarget, patrolSpeed);

        if (Vector3.Distance(transform.position, patrolTarget) <= waypointReachedDistance)
            SetNewPatrolTarget();
    }

    void SetNewPatrolTarget()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-patrolRadius, patrolRadius),
            0f,
            Random.Range(-patrolRadius, patrolRadius)
        );

        Vector3 candidate = startPosition + randomOffset;
        candidate.x = Mathf.Clamp(candidate.x, arenaMin, arenaMax);
        candidate.z = Mathf.Clamp(candidate.z, arenaMin, arenaMax);

        patrolTarget = candidate;
    }

    // ─── CHASE ────────────────────────────────────────────────────────────

    void UpdateChase()
    {
        // Re-check for a closer player each tick
        Transform nearest = GetNearestPlayer();
        if (nearest != null) chaseTarget = nearest;

        // Target disappeared or left the arena
        if (chaseTarget == null)
        {
            TransitionTo(State.Patrol);
            return;
        }

        float dist = Vector3.Distance(transform.position, chaseTarget.position);

        // Close enough to attack
        if (dist <= attackRadius)
        {
            TransitionTo(State.Attack);
            return;
        }

        // Lost sight of player — go back to patrol
        if (dist > detectionRadius * 2f)
        {
            chaseTarget = null;
            TransitionTo(State.Patrol);
            return;
        }

        MoveToward(chaseTarget.position, chaseSpeed);
    }

    // ─── ATTACK ───────────────────────────────────────────────────────────

    void UpdateAttack()
    {
        if (chaseTarget == null)
        {
            TransitionTo(State.Patrol);
            return;
        }

        float dist = Vector3.Distance(transform.position, chaseTarget.position);

        // Player escaped — go back to chasing
        if (dist > attackRadius * 1.5f)
        {
            TransitionTo(State.Chase);
            return;
        }

        // Deal damage on cooldown
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            PlayerNetwork playerNetwork = chaseTarget.GetComponent<PlayerNetwork>();
            PlayerMovement playerMovement = chaseTarget.GetComponent<PlayerMovement>();

            if (playerNetwork != null && playerMovement != null && !playerMovement.HasShield())
            {
                playerNetwork.score.Value -= scorePenalty;
                if (playerNetwork.score.Value < 0) playerNetwork.score.Value = 0;
            }
        }
    }

    // ─── FLEE ─────────────────────────────────────────────────────────────

    void UpdateFlee()
    {
        // Timeout — stop fleeing if it's taking too long
        if (Time.time - fleeStartTime >= fleeTimeout)
        {
            isFleeing = false;
            TransitionTo(State.Patrol);
            return;
        }

        // Continuously update flee direction away from nearest player
        Transform nearest = GetNearestPlayer();
        if (nearest != null)
        {
            Vector3 awayDir = transform.position - nearest.position;
            awayDir.y = 0f;
            if (awayDir != Vector3.zero)
                fleeDirection = awayDir.normalized;
        }

        Vector3 fleeTarget = transform.position + fleeDirection * fleeRadius;
        fleeTarget.x = Mathf.Clamp(fleeTarget.x, arenaMin, arenaMax);
        fleeTarget.z = Mathf.Clamp(fleeTarget.z, arenaMin, arenaMax);

        Vector3 prevPos = transform.position;
        MoveToward(fleeTarget, fleeSpeed);

        // If barely moving, we're stuck — pick a random direction
        if (Vector3.Distance(transform.position, prevPos) < 0.01f)
        {
            fleeDirection = new Vector3(
                Random.Range(-1f, 1f), 0f,
                Random.Range(-1f, 1f)).normalized;
        }

        if (Vector3.Distance(transform.position, fleeTarget) <= waypointReachedDistance)
        {
            isFleeing = false;
            TransitionTo(State.Patrol);
        }
    }

    // Called by Bullet.cs when it hits the monster
    public void OnHitByBullet(Vector3 bulletDirection)
    {
        if (!IsServer) return;

        fleeDirection = -bulletDirection.normalized;
        fleeDirection.y = 0f;

        isFleeing = true;
        fleeStartTime = Time.time;
        TransitionTo(State.Flee);
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────

    void TransitionTo(State newState)
    {
        currentState = newState;
    }

    void MoveToward(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;

        Vector3 currentPos = transform.position;
        float step = speed * Time.deltaTime;

        // Try full movement first
        Vector3 fullMove = currentPos + dir * step;
        if (!Physics.CheckSphere(fullMove, 0.6f, LayerMask.GetMask("Wall")))
        {
            transform.position = fullMove;
        }
        else
        {
            // Try sliding along X axis only
            Vector3 nextX = currentPos + new Vector3(dir.x, 0f, 0f) * step;
            if (!Physics.CheckSphere(nextX, 0.6f, LayerMask.GetMask("Wall")))
            {
                transform.position = nextX;
            }
            else
            {
                // Try sliding along Z axis only
                Vector3 nextZ = currentPos + new Vector3(0f, 0f, dir.z) * step;
                if (!Physics.CheckSphere(nextZ, 0.6f, LayerMask.GetMask("Wall")))
                {
                    transform.position = nextZ;
                }
                else
                {
                    // Fully blocked — pick a new patrol target to unstick
                    SetNewPatrolTarget();
                }
            }
        }

        // Face movement direction
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    Transform GetNearestPlayer()
    {
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float dist = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = client.PlayerObject.transform;
            }
        }

        return nearest;
    }

    // Debug visualization in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}