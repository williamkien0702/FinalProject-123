using UnityEngine;
using Unity.Netcode;

public class BlackHoleTrap : NetworkBehaviour
{
    public float triggerRadius = 2f;
    public float teleportMinDistance = 20f;
    public float teleportMaxDistance = 50f;

    public float arenaMinX = -46f;
    public float arenaMaxX = 46f;
    public float arenaMinZ = -46f;
    public float arenaMaxZ = 46f;

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
        Vector3 newPos = oldPos;

        for (int i = 0; i < 20; i++)
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

            // Make sure we don't teleport inside a wall
            if (!Physics.CheckSphere(candidate, 1f, LayerMask.GetMask("Wall")) &&
                Vector3.Distance(oldPos, candidate) >= teleportMinDistance)
            {
                newPos = candidate;
                break;
            }
        }

        playerNetObj.transform.position = newPos;

        // Tell the teleported player to play their teleport sound
        NotifyTeleportClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerNetObj.OwnerClientId }
            }
        });
    }

    [ClientRpc]
    void NotifyTeleportClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;
        localPlayer.GetComponent<PlayerNetwork>()?.PlayTeleportSound();
    }
}