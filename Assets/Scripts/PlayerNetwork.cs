using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    [SerializeField] AudioSource sfxSource;

    void Awake()
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Coin")) return;

        if (IsOwner)
        {
            PlayCoinSfxLocal();
        }

        if (!IsServer) return;

        var coinNetObj = other.GetComponent<NetworkObject>();
        if (coinNetObj != null && coinNetObj.IsSpawned)
        {
            score.Value += 1;

            Object.FindFirstObjectByType<GameManager>().CoinCollected();

            coinNetObj.Despawn(true);
        }
    }

    void PlayCoinSfxLocal()
    {
        if (sfxSource != null && sfxSource.clip != null)
        {
            // PlayOneShot layers sounds on top of each other so rapid
            // coin pickups don't cut off the previous sound
            sfxSource.PlayOneShot(sfxSource.clip);
        }
    }
}