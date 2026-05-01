using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    public float baseSpeed = 20f;
    public float dashForce = 12f;
    public float dashCooldown = 3f;

    private float currentSpeed;
    private bool canDash = true;
    private bool hasShield = false;

    private Vector2 moveInput = Vector2.zero;
    private Vector3 lastMoveDirection = Vector3.forward;

    void Awake()
    {
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (GameManager.gameOver) return;

        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;
        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;

        SubmitInputServerRpc(h, v);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DashServerRpc();
        }
    }

    [ServerRpc]
    void SubmitInputServerRpc(float h, float v)
    {
        moveInput = new Vector2(h, v);

        Vector3 move = new Vector3(h, 0, v).normalized;

        if (move != Vector3.zero)
        {
            lastMoveDirection = move;
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (GameManager.gameOver) return;

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 nextPosition = transform.position + move * currentSpeed * Time.fixedDeltaTime;

        if (!Physics.CheckSphere(nextPosition, 0.6f, LayerMask.GetMask("Wall")))
        {
            transform.position = nextPosition;
        }
    }

    [ServerRpc]
    void DashServerRpc()
    {
        if (!canDash) return;

        StartCoroutine(DashCooldownRoutine());

        transform.position += lastMoveDirection * dashForce;
    }

    IEnumerator DashCooldownRoutine()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void ApplySlow(float slowSpeed, float duration)
    {
        if (!IsServer) return;
        StartCoroutine(SlowRoutine(slowSpeed, duration));
    }

    IEnumerator SlowRoutine(float slowSpeed, float duration)
    {
        currentSpeed = slowSpeed;
        yield return new WaitForSeconds(duration);
        currentSpeed = baseSpeed;
    }

    public void ApplySpeedBoost(float boostedSpeed, float duration)
    {
        if (!IsServer) return;
        StartCoroutine(SpeedBoostRoutine(boostedSpeed, duration));
    }

    IEnumerator SpeedBoostRoutine(float boostedSpeed, float duration)
    {
        currentSpeed = boostedSpeed;
        yield return new WaitForSeconds(duration);
        currentSpeed = baseSpeed;
    }

    public void GiveShield(float duration)
    {
        if (!IsServer) return;
        StartCoroutine(ShieldRoutine(duration));
    }

    IEnumerator ShieldRoutine(float duration)
    {
        hasShield = true;
        yield return new WaitForSeconds(duration);
        hasShield = false;
    }

    public bool HasShield()
    {
        return hasShield;
    }

    public bool IsSpeedBoosted()
    {
        return currentSpeed > baseSpeed;
    }
}