using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BombTrap : NetworkBehaviour
{
    public float activationRadius = 1.5f;
    public float explosionDelay = 2f;
    public float explosionRadius = 5f;
    public int pointPenalty = 5;

    [Header("VFX")]
    public ParticleSystem explosionPS;

    [Header("SFX")]
    public AudioSource explosionAudio;

    private bool activated = false;

    void Update()
    {
        if (!IsServer) return;
        if (activated) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);

            if (distance <= activationRadius)
            {
                activated = true;
                StartCoroutine(ExplodeAfterDelay());
                break;
            }
        }
    }

    IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);

            if (distance <= explosionRadius)
            {
                PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
                PlayerMovement playerMovement = client.PlayerObject.GetComponent<PlayerMovement>();

                if (playerNetwork == null || playerMovement == null) continue;
                if (playerMovement.HasShield()) continue;

                playerNetwork.TakeDamage(pointPenalty);

                // Tell the hit player to play their hit sound
                NotifyHitClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { client.ClientId }
                    }
                });
            }
        }

        PlayExplosionClientRpc();

        yield return new WaitForSeconds(0.05f);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }

    [ClientRpc]
    void PlayExplosionClientRpc()
    {
        if (explosionPS != null) explosionPS.Play();

        if (explosionAudio != null && explosionAudio.clip != null)
        {
            GameObject tempAudio = new GameObject("ExplosionAudio_Temp");
            tempAudio.transform.position = transform.position;
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = explosionAudio.clip;
            tempSource.volume = explosionAudio.volume;
            tempSource.spatialBlend = explosionAudio.spatialBlend;
            tempSource.Play();
            Destroy(tempAudio, explosionAudio.clip.length + 0.1f);
        }
    }

    [ClientRpc]
    void NotifyHitClientRpc(ClientRpcParams rpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;
        localPlayer.GetComponent<PlayerNetwork>()?.PlayHitSound();
    }
}
