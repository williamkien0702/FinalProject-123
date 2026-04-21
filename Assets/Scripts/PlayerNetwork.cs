using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{   
    //networkVariable used to sync each player's score across the network
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    [SerializeField] AudioSource sfxSource;


    void Awake()
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Coin")) return;

        var coinNetObj = other.GetComponent<NetworkObject>();
        if (coinNetObj != null && coinNetObj.IsSpawned)
        {
            score.Value += 1;

            PlayCoinSfxClientRpc();

            Object.FindFirstObjectByType<GameManager>().CoinCollected();
            
            coinNetObj.Despawn(true);
        }
    }
    //play the coin collect sound
    [ClientRpc]
    void PlayCoinSfxClientRpc()
    {
        if (sfxSource != null) sfxSource.Play();
    }
}