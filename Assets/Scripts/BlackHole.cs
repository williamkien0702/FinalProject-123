using UnityEngine;
using Unity.Netcode;

public class BlackHoleTrap : NetworkBehaviour
{
    public float triggerRadius = 2f;
    public float teleportMinDistance = 12f;
    public float teleportMaxDistance = 28f;
    public float arenaMinX = -30f;
    public float arenaMaxX = 30f;
    public float arenaMinZ = -30f;
    public float arenaMaxZ = 30f;

    private bool _used;

    private void Update()
    {
        if (!IsServer || _used)
        {
            return;
        }

        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerNetwork player = players[i];
            if (player == null || !player.IsSpawned)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= triggerRadius)
            {
                _used = true;
                TeleportPlayer(player.gameObject);
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.NotifyTeleport();
                }

                NetworkObject netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
                break;
            }
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        Vector3 oldPos = player.transform.position;
        Vector3 newPos = oldPos;

        for (int i = 0; i < 20; i++)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(teleportMinDistance, teleportMaxDistance);
            Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 candidate = oldPos + direction * distance;

            candidate.x = Mathf.Clamp(candidate.x, arenaMinX, arenaMaxX);
            candidate.z = Mathf.Clamp(candidate.z, arenaMinZ, arenaMaxZ);
            candidate.y = oldPos.y;

            if (Vector3.Distance(oldPos, candidate) >= teleportMinDistance)
            {
                newPos = candidate;
                break;
            }
        }

        player.transform.position = newPos;
    }
}
