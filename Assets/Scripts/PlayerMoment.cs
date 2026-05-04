using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    public float baseSpeed = 8f;            // Reduced from 20 — FPS games move slower
    public float dashForce = 6f;
    public float dashCooldown = 3f;

    private float currentSpeed;
    private bool canDash = true;
    private bool isDashing = false;
    private bool hasShield = false;

    // Replicated from owner to server each tick
    private Vector2 moveInput = Vector2.zero;
    private float currentYaw = 0f;         // Horizontal facing angle in degrees

    void Awake()
    {
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (GameManager.gameOver)
        {
            // Unlock cursor on game over so UI buttons are clickable
            TopDownFollowOwner.UnlockCursor();
            return;
        }

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

    /// <summary>
    /// Called by FollowOwner on the local client every LateUpdate with the
    /// current mouse-look yaw so the server can rotate the player body.
    /// </summary>
    public void SetYaw(float yaw)
    {
        SetYawServerRpc(yaw);
    }

    [ServerRpc]
    void SetYawServerRpc(float yaw)
    {
        currentYaw = yaw;
        // Rotate the visible player body to face the direction they are looking
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    [ServerRpc]
    void SubmitInputServerRpc(float h, float v)
    {
        moveInput = new Vector2(h, v);
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (GameManager.gameOver) return;
        if (isDashing) return;  // Dash coroutine handles movement during dash

        // Move relative to the player's current facing direction (yaw)
        Vector3 forward = new Vector3(
            Mathf.Sin(currentYaw * Mathf.Deg2Rad), 0f,
            Mathf.Cos(currentYaw * Mathf.Deg2Rad));
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);

        Vector3 move = (forward * moveInput.y + right * moveInput.x).normalized;
        Vector3 nextPosition = transform.position + move * currentSpeed * Time.fixedDeltaTime;

        if (!Physics.CheckSphere(nextPosition, 0.4f, LayerMask.GetMask("Wall")))
        {
            transform.position = nextPosition;
        }
    }

    [ServerRpc]
    void DashServerRpc()
    {
        if (!canDash) return;
        if (isDashing) return;

        StartCoroutine(DashCooldownRoutine());

        Vector3 forward = new Vector3(
            Mathf.Sin(currentYaw * Mathf.Deg2Rad), 0f,
            Mathf.Cos(currentYaw * Mathf.Deg2Rad));

        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        Vector3 dashDir = (forward * moveInput.y + right * moveInput.x).normalized;
        if (dashDir == Vector3.zero) dashDir = forward;

        StartCoroutine(SmoothDashRoutine(dashDir));
    }

    IEnumerator SmoothDashRoutine(Vector3 dashDir)
    {
        isDashing = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + dashDir * dashForce;

        float elapsed = 0f;
        float dashDuration = 0.18f;  // How long the dash takes in seconds

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = elapsed / dashDuration;

            // Ease out — fast start, smooth stop
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, eased);

            // Still respect wall collisions during the dash
            if (!Physics.CheckSphere(nextPos, 0.4f, LayerMask.GetMask("Wall")))
            {
                transform.position = nextPos;
            }
            else
            {
                break; // Stop dash if we hit a wall
            }

            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
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

    public bool HasShield() => hasShield;

    public bool IsSpeedBoosted() => currentSpeed > baseSpeed;
}