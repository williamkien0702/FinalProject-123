using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BombTrap : NetworkBehaviour
{
    public float activationRadius = 1.5f;
    public float explosionDelay = 2f;
    public float explosionRadius = 5f;
    public int pointPenalty = 5;

    private bool activated;

    private void Update()
    {
        if (!IsServer || activated || GameManager.gameOver)
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
            if (distance <= activationRadius)
            {
                activated = true;
                WarnNearbyPlayers();
                StartCoroutine(ExplodeAfterDelay());
                break;
            }
        }
    }

    private void WarnNearbyPlayers()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
            if (distance <= explosionRadius + 1f)
            {
                PlayerMovement playerMovement = client.PlayerObject.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.ShowOwnerNotification("BOMB TRIGGERED!", "Move away from the blast.", ScoreUI.HudNoticeType.Danger, Mathf.Max(1.2f, explosionDelay));
                }
            }
        }
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);

        if (GameManager.gameOver)
        {
            DespawnSelf();
            yield break;
        }

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
            if (distance > explosionRadius)
            {
                continue;
            }

            PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
            PlayerMovement playerMovement = client.PlayerObject.GetComponent<PlayerMovement>();
            if (playerNetwork == null || playerMovement == null)
            {
                continue;
            }

            if (playerMovement.HasShield())
            {
                playerMovement.ShowOwnerNotification("SHIELD BLOCKED DAMAGE", "The bomb was absorbed.", ScoreUI.HudNoticeType.Success, 1.6f);
                continue;
            }

            playerNetwork.score.Value = Mathf.Max(0, playerNetwork.score.Value - pointPenalty);
            playerMovement.ShowOwnerNotification("-5 POINTS", "Bomb hit.", ScoreUI.HudNoticeType.Danger, 1.5f);
        }

        DespawnSelf();
    }

    private void DespawnSelf()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
