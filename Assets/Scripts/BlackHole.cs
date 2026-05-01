using Unity.Netcode;
using UnityEngine;

public class BlackHoleTrap : NetworkBehaviour
{
    public float triggerRadius = 2f;
    public float teleportMinDistance = 12f;
    public float teleportMaxDistance = 28f;
    public float arenaMinX = -30f;
    public float arenaMaxX = 30f;
    public float arenaMinZ = -30f;
    public float arenaMaxZ = 30f;

    private bool used;

    private void Update()
    {
        if (!IsServer || used || GameManager.gameOver)
        {
            return;
        }

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
            if (distance <= triggerRadius)
            {
                used = true;
                TeleportPlayer(client.PlayerObject.gameObject);
                break;
            }
        }
    }

    private void TeleportPlayer(GameObject playerObject)
    {
        Vector3 oldPosition = playerObject.transform.position;
        Vector3 newPosition = oldPosition;

        for (int i = 0; i < 20; i++)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(teleportMinDistance, teleportMaxDistance);
            Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 candidate = oldPosition + direction * distance;
            candidate.x = Mathf.Clamp(candidate.x, arenaMinX, arenaMaxX);
            candidate.z = Mathf.Clamp(candidate.z, arenaMinZ, arenaMaxZ);
            candidate.y = oldPosition.y;

            if (Vector3.Distance(oldPosition, candidate) >= teleportMinDistance)
            {
                newPosition = candidate;
                break;
            }
        }

        playerObject.transform.position = newPosition;

        PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.ShowOwnerNotification("TELEPORTED!", "Black hole warped you away.", ScoreUI.HudNoticeType.Info, 1.8f);
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
