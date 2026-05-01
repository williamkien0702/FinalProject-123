using Unity.Netcode;
using UnityEngine;

public class KingCoin : NetworkBehaviour
{
    public int pointValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        if (playerNetwork == null)
        {
            return;
        }

        playerNetwork.score.Value += pointValue;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.ShowOwnerNotification("KING COIN +10", "Big score bonus collected.", ScoreUI.HudNoticeType.Objective, 2f);
        }

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.KingCoinCollected();
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
