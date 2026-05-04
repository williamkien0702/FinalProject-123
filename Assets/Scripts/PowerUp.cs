using UnityEngine;
using Unity.Netcode;

public class PowerUp : NetworkBehaviour
{
    public enum PowerUpType { SpeedBoost, Shield }

    public PowerUpType powerUpType;
    public float boostedSpeed = 35f;
    public float duration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        if (powerUpType == PowerUpType.SpeedBoost)
            player.ApplySpeedBoost(boostedSpeed, duration);
        else if (powerUpType == PowerUpType.Shield)
            player.GiveShield(duration);

        // Play the correct sound for this power-up type
        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        if (playerNetwork != null)
        {
            if (powerUpType == PowerUpType.SpeedBoost)
            {
                NotifySpeedClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { playerNetwork.OwnerClientId }
                    }
                });
            }
            else if (powerUpType == PowerUpType.Shield)
            {
                NotifyShieldClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { playerNetwork.OwnerClientId }
                    }
                });
            }
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }

    [ClientRpc]
    void NotifySpeedClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;
        localPlayer.GetComponent<PlayerNetwork>()?.PlaySpeedSound();
    }

    [ClientRpc]
    void NotifyShieldClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;
        localPlayer.GetComponent<PlayerNetwork>()?.PlayShieldSound();
    }
}