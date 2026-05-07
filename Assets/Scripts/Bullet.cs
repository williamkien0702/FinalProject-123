using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float speed = 30f;
    public int scorePenalty = 3;
    public float lifetime = 3f;        // Despawn if nothing is hit

    private ulong ownerClientId;

    public void SetOwner(ulong clientId)
    {
        ownerClientId = clientId;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Invoke(nameof(DespawnBullet), lifetime);
    }

    void Update()
    {
        // Move forward on all clients so it looks smooth
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Hit a monster — make it flee
        Monster monster = other.GetComponentInParent<Monster>();
        if (monster != null)
        {
            monster.OnHitByBullet(transform.forward);
            DespawnBullet();
            return;
        }

        // Hit a player
        PlayerNetwork playerNetwork = other.GetComponentInParent<PlayerNetwork>();
        if (playerNetwork == null) return;

        // Don't hit the player who fired it
        if (playerNetwork.OwnerClientId == ownerClientId) return;

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null && playerMovement.HasShield()) return;

        playerNetwork.TakeDamage(scorePenalty);

        // Tell the hit player to play their hit sound
        NotifyHitClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerNetwork.OwnerClientId }
            }
        });

        DespawnBullet();
    }

    [ClientRpc]
    void NotifyHitClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;
        localPlayer.GetComponent<PlayerNetwork>()?.PlayHitSound();
    }

    void DespawnBullet()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }
}
