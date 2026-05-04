using UnityEngine;
using Unity.Netcode;

public class FakeCoinTrap : NetworkBehaviour
{
    public float slowSpeed = 2f;
    public float slowDuration = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        player.ApplySlow(slowSpeed, slowDuration);

        // Play fake coin sound on the player who hit it
        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        if (playerNetwork != null)
        {
            NotifyFakeCoinClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { playerNetwork.OwnerClientId }
                }
            });
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }

    [ClientRpc]
    void NotifyFakeCoinClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;
        localPlayer.GetComponent<PlayerNetwork>()?.PlayFakeCoinSound();
    }
}