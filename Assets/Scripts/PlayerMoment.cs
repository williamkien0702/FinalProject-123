using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    public float baseSpeed = 20f;
    public float dashForce = 12f;
    public float dashCooldown = 3f;

    private float _slowSpeed = 6f;
    private float _boostSpeed = 35f;
    private float _dashCooldownRemaining;
    private Vector2 _moveInput = Vector2.zero;
    private Vector3 _lastMoveDirection = Vector3.forward;

    private readonly NetworkVariable<float> _speedBoostRemaining = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> _shieldRemaining = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> _slowRemaining = new NetworkVariable<float>(0f);

    private static readonly Dictionary<ulong, string> _cachedPlayerLabels = new Dictionary<ulong, string>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            CacheLocalPlayerLabel();
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            TickStatusTimers(Time.deltaTime);
        }

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

        if (_dashCooldownRemaining > 0f)
        {
            _dashCooldownRemaining -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && _dashCooldownRemaining <= 0f)
        {
            _dashCooldownRemaining = dashCooldown;
            DashServerRpc();
            UiEventFeed.Push("DASH", new Color(0.78f, 0.96f, 1f), 0.7f);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        Vector3 nextPosition = transform.position + move * GetCurrentSpeed() * Time.fixedDeltaTime;

        if (!Physics.CheckSphere(nextPosition, 0.6f, LayerMask.GetMask("Wall")))
        {
            transform.position = nextPosition;
        }
    }

    private void TickStatusTimers(float deltaTime)
    {
        if (_speedBoostRemaining.Value > 0f)
        {
            _speedBoostRemaining.Value = Mathf.Max(0f, _speedBoostRemaining.Value - deltaTime);
        }

        if (_shieldRemaining.Value > 0f)
        {
            _shieldRemaining.Value = Mathf.Max(0f, _shieldRemaining.Value - deltaTime);
        }

        if (_slowRemaining.Value > 0f)
        {
            _slowRemaining.Value = Mathf.Max(0f, _slowRemaining.Value - deltaTime);
        }
    }

    private float GetCurrentSpeed()
    {
        float speed = baseSpeed;

        if (_speedBoostRemaining.Value > 0f)
        {
            speed = Mathf.Max(speed, _boostSpeed);
        }

        if (_slowRemaining.Value > 0f)
        {
            speed = Mathf.Min(speed, _slowSpeed);
        }

        return speed;
    }

    [ServerRpc]
    private void SubmitInputServerRpc(float h, float v)
    {
        _moveInput = new Vector2(h, v);
        Vector3 move = new Vector3(h, 0f, v).normalized;
        if (move != Vector3.zero)
        {
            _lastMoveDirection = move;
        }
    }

    [ServerRpc]
    private void DashServerRpc()
    {
        if (GameManager.gameOver)
        {
            return;
        }

        transform.position += _lastMoveDirection * dashForce;
    }

    public void ApplySlow(float slowSpeed, float duration)
    {
        if (!IsServer)
        {
            return;
        }

        _slowSpeed = slowSpeed;
        _slowRemaining.Value = Mathf.Max(_slowRemaining.Value, duration);
        NotifyLocalClientClientRpc("SLOWED!", new Color(1f, 0.65f, 0.2f), 1.5f, BuildTargetParams());
    }

    public void ApplySpeedBoost(float boostedSpeed, float duration)
    {
        if (!IsServer)
        {
            return;
        }

        _boostSpeed = boostedSpeed;
        _speedBoostRemaining.Value = Mathf.Max(_speedBoostRemaining.Value, duration);
        NotifyLocalClientClientRpc("SPEED UP", new Color(0.3f, 1f, 0.65f), 1.6f, BuildTargetParams());
    }

    public void GiveShield(float duration)
    {
        if (!IsServer)
        {
            return;
        }

        _shieldRemaining.Value = Mathf.Max(_shieldRemaining.Value, duration);
        NotifyLocalClientClientRpc("SHIELD ACTIVE", new Color(0.35f, 0.8f, 1f), 1.6f, BuildTargetParams());
    }

    public void NotifyShieldBlockedDamage()
    {
        if (!IsServer)
        {
            return;
        }

        NotifyLocalClientClientRpc("SHIELD BLOCKED DAMAGE", new Color(0.35f, 0.8f, 1f), 1.8f, BuildTargetParams());
    }

    public void NotifyBombTriggered()
    {
        if (!IsServer)
        {
            return;
        }

        NotifyLocalClientClientRpc("BOMB TRIGGERED", new Color(1f, 0.45f, 0.35f), 1.2f, BuildTargetParams());
    }

    public void NotifyBombDamage(int penalty)
    {
        if (!IsServer)
        {
            return;
        }

        NotifyLocalClientClientRpc("-" + penalty + " POINTS", new Color(1f, 0.35f, 0.35f), 1.5f, BuildTargetParams());
    }

    public void NotifyLaserDamage(int penalty)
    {
        if (!IsServer)
        {
            return;
        }

        NotifyLocalClientClientRpc("LASER HIT  -" + penalty, new Color(1f, 0.35f, 0.35f), 1.4f, BuildTargetParams());
    }

    public void NotifyLocalKingCoin()
    {
        if (!IsServer)
        {
            return;
        }

        NotifyLocalClientClientRpc("KING COIN +10", new Color(1f, 0.87f, 0.2f), 1.8f, BuildTargetParams());
    }

    public void NotifyTeleport()
    {
        if (!IsServer)
        {
            return;
        }

        NotifyLocalClientClientRpc("TELEPORTED", new Color(0.75f, 0.55f, 1f), 1.4f, BuildTargetParams());
    }

    public void ResetStatusState()
    {
        if (!IsServer)
        {
            return;
        }

        _speedBoostRemaining.Value = 0f;
        _shieldRemaining.Value = 0f;
        _slowRemaining.Value = 0f;
        _dashCooldownRemaining = 0f;
        _moveInput = Vector2.zero;
    }

    public bool HasShield()
    {
        return _shieldRemaining.Value > 0.01f;
    }

    public bool IsSpeedBoosted()
    {
        return _speedBoostRemaining.Value > 0.01f;
    }

    public bool IsSlowed()
    {
        return _slowRemaining.Value > 0.01f;
    }

    public float GetShieldRemaining()
    {
        return _shieldRemaining.Value;
    }

    public float GetSpeedBoostRemaining()
    {
        return _speedBoostRemaining.Value;
    }

    public float GetSlowRemaining()
    {
        return _slowRemaining.Value;
    }

    public float GetDashCooldownRemainingLocal()
    {
        return Mathf.Max(0f, _dashCooldownRemaining);
    }

    public float GetDashCooldownDuration()
    {
        return dashCooldown;
    }

    private ClientRpcParams BuildTargetParams()
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };
    }

    [ClientRpc]
    private void NotifyLocalClientClientRpc(string message, Color color, float duration, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner)
        {
            return;
        }

        UiEventFeed.Push(message, color, duration);
    }

    private void CacheLocalPlayerLabel()
    {
        string label = "Player " + (OwnerClientId + 1);
        _cachedPlayerLabels[OwnerClientId] = label;
    }

    public static string GetPlayerLabel(ulong ownerClientId)
    {
        if (_cachedPlayerLabels.TryGetValue(ownerClientId, out string cached))
        {
            return cached;
        }

        return "Player " + (ownerClientId + 1);
    }
}
