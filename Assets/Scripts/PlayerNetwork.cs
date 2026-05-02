using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Coin"))
        {
            return;
        }

        if (IsOwner)
        {
            PlayCoinSfxLocal();
        }

        if (!IsServer)
        {
            return;
        }

        NetworkObject coinNetObj = other.GetComponent<NetworkObject>();
        if (coinNetObj != null && coinNetObj.IsSpawned)
        {
            score.Value += 1;

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.CoinCollected();
            }

            coinNetObj.Despawn(true);
        }
    }

    private void PlayCoinSfxLocal()
    {
        if (sfxSource != null)
        {
            sfxSource.Play();
        }
    }
}
