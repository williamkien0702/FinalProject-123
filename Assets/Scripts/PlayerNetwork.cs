using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    [Header("SFX")]
    [SerializeField] AudioSource coinAudio;       // Existing coin sound
    public AudioSource hitAudio;                  // Hit by ghost, bullet, or bomb
    public AudioSource speedAudio;                // Speed pickup
    public AudioSource shieldAudio;               // Shield pickup
    public AudioSource fakeCoinAudio;             // Fake coin pickup
    public AudioSource teleportAudio;             // Black hole teleport

    void Awake()
    {
        if (coinAudio == null) coinAudio = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Coin")) return;

        if (IsOwner)
            PlayCoinSfxLocal();

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
        if (coinAudio != null && coinAudio.clip != null)
            coinAudio.PlayOneShot(coinAudio.clip);
    }

    public void PlayHitSound()
    {
        if (!IsOwner) return;
        if (hitAudio != null && hitAudio.clip != null)
            hitAudio.PlayOneShot(hitAudio.clip);
    }

    public void PlaySpeedSound()
    {
        if (!IsOwner) return;
        if (speedAudio != null && speedAudio.clip != null)
            speedAudio.PlayOneShot(speedAudio.clip);
    }

    public void PlayShieldSound()
    {
        if (!IsOwner) return;
        if (shieldAudio != null && shieldAudio.clip != null)
            shieldAudio.PlayOneShot(shieldAudio.clip);
    }

    public void PlayFakeCoinSound()
    {
        if (!IsOwner) return;
        if (fakeCoinAudio != null && fakeCoinAudio.clip != null)
            fakeCoinAudio.PlayOneShot(fakeCoinAudio.clip);
    }

    public void PlayTeleportSound()
    {
        if (!IsOwner) return;
        if (teleportAudio != null && teleportAudio.clip != null)
            teleportAudio.PlayOneShot(teleportAudio.clip);
    }
}