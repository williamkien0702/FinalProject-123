using Unity.Netcode;
using UnityEngine;

public class FakeCoinTrap : NetworkBehaviour
{
    public float slowSpeed = 6f;
    public float slowDuration = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || GameManager.gameOver)
        {
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        player.ApplySlow(slowSpeed, slowDuration);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
