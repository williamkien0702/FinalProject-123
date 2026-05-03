using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 20f;
    public float dashForce = 12f;
    public float dashCooldown = 3f;

    [Header("Effect Defaults")]
    [SerializeField] private float defaultSlowSpeed = 6f;
    [SerializeField] private float defaultBoostSpeed = 35f;
    [SerializeField] private float defaultSlowDuration = 2f;
    [SerializeField] private float defaultBoostDuration = 5f;
    [SerializeField] private float defaultShieldDuration = 5f;

    private float _slowSpeed;
    private float _boostSpeed;

    private float _dashCooldownRemainingLocal;
    private Vector2 _moveInput = Vector2.zero;
    private Vector3 _lastMoveDirection = Vector3.forward;

    private float _currentYaw = 0f;

    private readonly NetworkVariable<float> _speedBoostRemaining =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _shieldRemaining =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _slowRemaining =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> _syncedYaw =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private static readonly Dictionary<ulong, string> _cachedPlayerLabels = new Dictionary<ulong, string>();

    public float CurrentYaw => _currentYaw;

    private void Awake()
    {
        _slowSpeed = defaultSlowSpeed;
        _boostSpeed = defaultBoostSpeed;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _currentYaw = _syncedYaw.Value;
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);

        if (IsOwner)
        {
            CacheLocalPlayerLabel();
        }
    }

    private void Update()
    {
        if (IsServer && !GameManager.gameOver)
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

        if (_dashCooldownRemainingLocal > 0f)
        {
            _dashCooldownRemainingLocal -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && _dashCooldownRemainingLocal <= 0f)
        {
            _dashCooldownRemainingLocal = dashCooldown;
            DashServerRpc();

            if (HasUiEventFeed())
            {
                UiEventFeed.Push("DASH", new Color(0.78f, 0.96f, 1f), 0.7f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        Vector3 localMove = new Vector3(_moveInput.x, 0f, _moveInput.y);

        if (localMove.sqrMagnitude > 1f)
        {
            localMove.Normalize();
        }

        Vector3 worldMove = Quaternion.Euler(0f, _currentYaw, 0f) * localMove;

        if (worldMove.sqrMagnitude > 0.0001f)
        {
            _lastMoveDirection = worldMove.normalized;
        }

        Vector3 nextPosition = transform.position + worldMove * GetCurrentSpeed() * Time.fixedDeltaTime;

        if (!Physics.CheckSphere(nextPosition, 0.6f, LayerMask.GetMask("Wall")))
        {
            transform.position = nextPosition;
        }

        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
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

        Vector3 localMove = new Vector3(h, 0f, v);
        if (localMove.sqrMagnitude > 1f)
        {
            localMove.Normalize();
        }

        Vector3 worldMove = Quaternion.Euler(0f, _currentYaw, 0f) * localMove;
        if (worldMove.sqrMagnitude > 0.0001f)
        {
            _lastMoveDirection = worldMove.normalized;
        }
    }

    [ServerRpc]
    private void DashServerRpc()
    {
        if (GameManager.gameOver)
        {
            return;
        }

        Vector3 direction = _lastMoveDirection.sqrMagnitude > 0.0001f
            ? _lastMoveDirection.normalized
            : Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.forward;

        Vector3 targetPosition = transform.position + direction * dashForce;

        if (!Physics.CheckSphere(targetPosition, 0.6f, LayerMask.GetMask("Wall")))
        {
            transform.position = targetPosition;
        }
    }

    public void ApplySlow()
    {
        ApplySlow(defaultSlowSpeed, defaultSlowDuration);
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

    public void ApplySpeedBoost()
    {
        ApplySpeedBoost(defaultBoostSpeed, defaultBoostDuration);
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

    public void GiveShield()
    {
        GiveShield(defaultShieldDuration);
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

        NotifyLocalClientClientRpc("LASER HIT -" + penalty, new Color(1f, 0.35f, 0.35f), 1.4f, BuildTargetParams());
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
        _moveInput = Vector2.zero;
        _lastMoveDirection = Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.forward;
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
        return Mathf.Max(0f, _dashCooldownRemainingLocal);
    }

    public float GetDashCooldownDuration()
    {
        return dashCooldown;
    }

    public void SetYaw(float yawDegrees)
    {
        if (float.IsNaN(yawDegrees) || float.IsInfinity(yawDegrees))
        {
            return;
        }

        if (IsServer)
        {
            ApplyYaw(yawDegrees);
        }
        else if (IsOwner)
        {
            SetYawServerRpc(yawDegrees);
        }
    }

    [ServerRpc]
    private void SetYawServerRpc(float yawDegrees)
    {
        ApplyYaw(yawDegrees);
    }

    private void ApplyYaw(float yawDegrees)
    {
        _currentYaw = yawDegrees;
        _syncedYaw.Value = yawDegrees;

        Vector3 forward = Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.forward;
        _lastMoveDirection = forward.normalized;
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
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

        if (HasUiEventFeed())
        {
            UiEventFeed.Push(message, color, duration);
        }
    }

    private static bool HasUiEventFeed()
    {
        return typeof(UiEventFeed) != null;
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