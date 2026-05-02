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

        if (powerUpType == PowerUpType.SpeedBoost)
        {
            player.ApplySpeedBoost(boostedSpeed, duration);
        }
        else if (powerUpType == PowerUpType.Shield)
        {
            player.GiveShield(duration);
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
