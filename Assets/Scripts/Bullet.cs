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

        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        if (playerNetwork == null) return;

        // Don't hit the player who fired it
        if (playerNetwork.OwnerClientId == ownerClientId) return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null && playerMovement.HasShield()) return;

        playerNetwork.score.Value -= scorePenalty;
        if (playerNetwork.score.Value < 0) playerNetwork.score.Value = 0;

        DespawnBullet();
    }

    void DespawnBullet()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }
}