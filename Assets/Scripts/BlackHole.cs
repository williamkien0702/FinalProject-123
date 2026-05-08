using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class BlackHoleTrap : NetworkBehaviour
{
    public float triggerRadius = 3.2f;
    public float teleportMinDistance = 20f;
    public float teleportMaxDistance = 50f;

    public float arenaMinX = -46f;
    public float arenaMaxX = 46f;
    public float arenaMinZ = -46f;
    public float arenaMaxZ = 46f;

    [Header("Bomb Teleport Logic")]
    public float teleportNearBombChance = 0.45f;
    public float bombTeleportDistance = 3f;

    [Header("Teleport Safety")]
    public float wallCheckRadius = 1.5f;
    public float escapeCheckDistance = 8f;
    public int minimumOpenDirections = 3;

    private bool used = false;

    void Update()
    {
        if (!IsServer) return;
        if (used) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float distance = Vector3.Distance(
                transform.position,
                client.PlayerObject.transform.position
            );

            if (distance <= triggerRadius)
            {
                used = true;

                TeleportPlayer(client.PlayerObject);

                NetworkObject netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);

                break;
            }
        }
    }

    void TeleportPlayer(NetworkObject playerNetObj)
    {
        Vector3 oldPos = playerNetObj.transform.position;
        Vector3 newPos;

        bool shouldTeleportNearBomb = Random.value < teleportNearBombChance;
        bool foundBombSpot = false;

        if (shouldTeleportNearBomb)
        {
            foundBombSpot = TryGetPositionNearBomb(oldPos, out newPos);
        }
        else
        {
            newPos = GetRandomSafeTeleportPosition(oldPos);
        }

        if (!foundBombSpot && shouldTeleportNearBomb)
        {
            newPos = GetRandomSafeTeleportPosition(oldPos);
        }

        playerNetObj.transform.position = newPos;

        NotifyTeleportClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerNetObj.OwnerClientId }
            }
        });
    }

    bool TryGetPositionNearBomb(Vector3 oldPos, out Vector3 result)
    {
        result = oldPos;

        BombTrap[] bombs = FindObjectsByType<BombTrap>(FindObjectsSortMode.None);

        if (bombs.Length == 0)
        {
            Debug.Log("BlackHole: No bombs found.");
            return false;
        }

        for (int i = 0; i < 30; i++)
        {
            BombTrap bomb = bombs[Random.Range(0, bombs.Length)];

            NetworkObject bombNetObj = bomb.GetComponent<NetworkObject>();
            if (bombNetObj == null || !bombNetObj.IsSpawned) continue;

            float angle = Random.Range(0f, 360f);

            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            Vector3 candidate = bomb.transform.position + direction * bombTeleportDistance;

            candidate.x = Mathf.Clamp(candidate.x, arenaMinX, arenaMaxX);
            candidate.z = Mathf.Clamp(candidate.z, arenaMinZ, arenaMaxZ);
            candidate.y = oldPos.y;

            if (!IsSafeTeleportSpot(candidate)) continue;

            result = candidate;
            Debug.Log("BlackHole: Teleported player near bomb.");
            return true;
        }

        Debug.Log("BlackHole: Could not find safe bomb teleport spot.");
        return false;
    }

    Vector3 GetRandomSafeTeleportPosition(Vector3 oldPos)
    {
        for (int i = 0; i < 50; i++)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(teleportMinDistance, teleportMaxDistance);

            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            Vector3 candidate = oldPos + direction * distance;

            candidate.x = Mathf.Clamp(candidate.x, arenaMinX, arenaMaxX);
            candidate.z = Mathf.Clamp(candidate.z, arenaMinZ, arenaMaxZ);
            candidate.y = oldPos.y;

            if (Vector3.Distance(oldPos, candidate) < teleportMinDistance) continue;

            if (!IsSafeTeleportSpot(candidate)) continue;

            return candidate;
        }

        // Final fallback: center area, usually safer than a random corner.
        Vector3 fallback = new Vector3(0f, oldPos.y, 0f);

        if (IsSafeTeleportSpot(fallback))
            return fallback;

        return oldPos;
    }

    bool IsSafeTeleportSpot(Vector3 position)
    {
        int wallMask = LayerMask.GetMask("Wall");

        // First check: do not teleport directly inside or too close to a wall.
        if (Physics.CheckSphere(position, wallCheckRadius, wallMask))
        {
            return false;
        }

        // Second check: make sure there are enough open escape directions.
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

        int openDirections = 0;

        foreach (Vector3 dir in directions)
        {
            Vector3 rayStart = position + Vector3.up * 0.5f;

            if (!Physics.Raycast(rayStart, dir, escapeCheckDistance, wallMask))
            {
                openDirections++;
            }
        }

        return openDirections >= minimumOpenDirections;
    }

    [ClientRpc]
    void NotifyTeleportClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        localPlayer.GetComponent<PlayerNetwork>()?.PlayTeleportSound();
    }
}