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
        if (GameManager.gameOver || !other.CompareTag("Coin"))
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

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.CoinCollected();
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
