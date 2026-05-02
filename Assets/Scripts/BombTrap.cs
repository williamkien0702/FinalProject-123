using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BombTrap : NetworkBehaviour
{
    public float activationRadius = 1.5f;
    public float explosionDelay = 2f;
    public float explosionRadius = 5f;
    public int pointPenalty = 5;

    private bool activated = false;

    void Update()
    {
        if (!IsServer) return;
        if (activated) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);

            if (distance <= activationRadius)
            {
                activated = true;
                StartCoroutine(ExplodeAfterDelay());
                break;
            }
        }
    }

    IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);

            if (distance <= explosionRadius)
            {
                PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
                PlayerMovement playerMovement = client.PlayerObject.GetComponent<PlayerMovement>();

                if (playerNetwork == null || playerMovement == null) continue;

                if (playerMovement.HasShield())
                {
                    continue;
                }

                playerNetwork.score.Value -= pointPenalty;

                if (playerNetwork.score.Value < 0)
                {
                    playerNetwork.score.Value = 0;
                }
            }
        }

        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}