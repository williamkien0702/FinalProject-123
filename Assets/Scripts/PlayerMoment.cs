using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float baseSpeed = 20f;
    public float dashForce = 12f;
    public float dashCooldown = 3f;

    private Vector2 moveInput = Vector2.zero;
    private Vector3 lastMoveDirection = Vector3.forward;
    private float dashReadyTime;
    private float activeSlowSpeed = 6f;
    private float activeBoostSpeed = 35f;

    private readonly NetworkVariable<float> speedBoostStartTime = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> speedBoostEndTime = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> slowStartTime = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> slowEndTime = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> shieldStartTime = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> shieldEndTime = new NetworkVariable<float>(0f);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ResetRoundState();
        }
    }

    private void Update()
    {
        if (!IsOwner || GameManager.gameOver)
        {
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

    [ServerRpc]
    private void SubmitInputServerRpc(float h, float v)
    {
        moveInput = new Vector2(h, v);

        Vector3 move = new Vector3(h, 0f, v).normalized;
        if (move != Vector3.zero)
        {
            lastMoveDirection = move;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        float currentSpeed = GetCurrentMoveSpeedServer();
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        Vector3 nextPosition = transform.position + move * currentSpeed * Time.fixedDeltaTime;

        if (!Physics.CheckSphere(nextPosition, 0.6f, LayerMask.GetMask("Wall")))
        {
            transform.position = nextPosition;
        }
    }

    [ServerRpc]
    private void DashServerRpc()
    {
        if (GameManager.gameOver)
        {
            return;
        }

        float now = GetAuthoritativeTime();
        if (now < dashReadyTime)
        {
            return;
        }

        dashReadyTime = now + dashCooldown;
        transform.position += lastMoveDirection * dashForce;
    }

    public void ApplySlow(float slowSpeed, float duration)
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        float now = GetAuthoritativeTime();
        activeSlowSpeed = slowSpeed;
        slowStartTime.Value = now;
        slowEndTime.Value = now + duration;

        ShowOwnerNotification("SLOWED!", "Fake coin penalty active.", ScoreUI.HudNoticeType.Warning, Mathf.Max(1.3f, duration));
    }

    public void ApplySpeedBoost(float boostedSpeed, float duration)
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        float now = GetAuthoritativeTime();
        activeBoostSpeed = boostedSpeed;
        speedBoostStartTime.Value = now;
        speedBoostEndTime.Value = now + duration;

        ShowOwnerNotification("SPEED UP", "Movement boosted.", ScoreUI.HudNoticeType.Success, Mathf.Max(1.3f, duration));
    }

    public void GiveShield(float duration)
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        float now = GetAuthoritativeTime();
        shieldStartTime.Value = now;
        shieldEndTime.Value = now + duration;

        ShowOwnerNotification("SHIELD READY", "Bomb damage is blocked.", ScoreUI.HudNoticeType.Success, Mathf.Max(1.3f, duration));
    }

    public void ResetRoundState()
    {
        if (!IsServer)
        {
            return;
        }

        moveInput = Vector2.zero;
        lastMoveDirection = Vector3.forward;
        dashReadyTime = 0f;
        activeSlowSpeed = 6f;
        activeBoostSpeed = 35f;

        speedBoostStartTime.Value = 0f;
        speedBoostEndTime.Value = 0f;
        slowStartTime.Value = 0f;
        slowEndTime.Value = 0f;
        shieldStartTime.Value = 0f;
        shieldEndTime.Value = 0f;
    }

    public bool HasShield()
    {
        return GetShieldRemaining() > 0f;
    }

    public bool IsSpeedBoosted()
    {
        return GetSpeedBoostRemaining() > 0f;
    }

    public bool IsSlowed()
    {
        return GetSlowRemaining() > 0f;
    }

    public float GetShieldRemaining()
    {
        return GetRemainingTime(shieldEndTime.Value);
    }

    public float GetSpeedBoostRemaining()
    {
        return GetRemainingTime(speedBoostEndTime.Value);
    }

    public float GetSlowRemaining()
    {
        return GetRemainingTime(slowEndTime.Value);
    }

    public float GetShieldNormalized()
    {
        return GetEffectNormalized(shieldStartTime.Value, shieldEndTime.Value);
    }

    public float GetSpeedBoostNormalized()
    {
        return GetEffectNormalized(speedBoostStartTime.Value, speedBoostEndTime.Value);
    }

    public float GetSlowNormalized()
    {
        return GetEffectNormalized(slowStartTime.Value, slowEndTime.Value);
    }

    public void ShowOwnerNotification(string title, string subtitle, ScoreUI.HudNoticeType type, float duration = 1.8f)
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }

        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        ShowOwnerNotificationClientRpc(title, subtitle, (int)type, duration, rpcParams);
    }

    [ClientRpc]
    private void ShowOwnerNotificationClientRpc(string title, string subtitle, int type, float duration, ClientRpcParams clientRpcParams = default(ClientRpcParams))
    {
        ScoreUI.QueueLocalNotification(title, subtitle, (ScoreUI.HudNoticeType)type, duration);
    }

    private float GetCurrentMoveSpeedServer()
    {
        float speed = baseSpeed;

        if (GetRemainingTime(slowEndTime.Value) > 0f)
        {
            speed = activeSlowSpeed;
        }

        if (GetRemainingTime(speedBoostEndTime.Value) > 0f)
        {
            speed = Mathf.Max(speed, activeBoostSpeed);
        }

        return speed;
    }

    private float GetRemainingTime(float endTime)
    {
        return Mathf.Max(0f, endTime - GetAuthoritativeTime());
    }

    private float GetEffectNormalized(float startTime, float endTime)
    {
        float total = endTime - startTime;
        if (total <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01((endTime - GetAuthoritativeTime()) / total);
    }

    private float GetAuthoritativeTime()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            return (float)NetworkManager.Singleton.ServerTime.Time;
        }

        return Time.time;
    }
}
