using UnityEngine;
using Unity.Netcode;

public class GunPickup : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        if (playerNetwork == null) return;

        // Give the gun to this player via their GunShooter component
        GunShooter shooter = other.GetComponent<GunShooter>();
        if (shooter == null) return;

        shooter.PickUpGun();

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }
}