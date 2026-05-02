using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BombTrap : NetworkBehaviour
{
    public float activationRadius = 1.5f;
    public float explosionDelay = 2f;
    public float explosionRadius = 5f;
    public int pointPenalty = 5;

    private bool _activated;

    private void Update()
    {
        if (!IsServer || _activated)
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
            if (distance <= activationRadius)
            {
                _activated = true;
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.NotifyBombTriggered();
                }
                StartCoroutine(ExplodeAfterDelay());
                break;
            }
        }
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);

        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerNetwork playerNetwork = players[i];
            if (playerNetwork == null || !playerNetwork.IsSpawned)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, playerNetwork.transform.position);
            if (distance > explosionRadius)
            {
                continue;
            }

            PlayerMovement playerMovement = playerNetwork.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                continue;
            }

            if (playerMovement.HasShield())
            {
                playerMovement.NotifyShieldBlockedDamage();
                continue;
            }

            playerNetwork.score.Value = Mathf.Max(0, playerNetwork.score.Value - pointPenalty);
            playerMovement.NotifyBombDamage(pointPenalty);
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
