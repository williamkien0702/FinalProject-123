using UnityEngine;
using Unity.Netcode;

public class KingCoin : NetworkBehaviour
{
    public int pointValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null)
        {
            return;
        }

        player.score.Value += pointValue;

        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.NotifyLocalKingCoin();
        }

        if (player.IsOwner)
        {
            FineMarbleSfx.Instance?.PlayKingCoinPickup();
        }

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.KingCoinCollected();
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}