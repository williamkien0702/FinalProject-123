using UnityEngine;
using Unity.Netcode;

public class PowerUp : NetworkBehaviour
{
    public enum PowerUpType
    {
        SpeedBoost,
        Shield
    }

    public PowerUpType powerUpType;
    public float boostedSpeed = 35f;
    public float duration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();

        if (powerUpType == PowerUpType.SpeedBoost)
        {
            player.ApplySpeedBoost(boostedSpeed, duration);

            if (playerNetwork != null && playerNetwork.IsOwner)
            {
                FineMarbleSfx.Instance?.PlaySpeedBoost();
            }
        }
        else if (powerUpType == PowerUpType.Shield)
        {
            player.GiveShield(duration);

            if (playerNetwork != null && playerNetwork.IsOwner)
            {
                FineMarbleSfx.Instance?.PlayShieldPickup();
            }
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}