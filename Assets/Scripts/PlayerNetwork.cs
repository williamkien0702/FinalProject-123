using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Damage Flash")]
    public Color damageFlashColor = Color.red;
    [Range(0f, 1f)] public float damageFlashAmount = 0.75f;
    public float damageFlashDuration = 0.2f;

    private Coroutine damageFlashCoroutine;
    private List<Material> flashMaterials;
    private List<string> flashColorProperties;
    private List<Color> flashOriginalColors;

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

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;
        if (amount <= 0) return;

        score.Value -= amount;
        if (score.Value < 0) score.Value = 0;

        FlashDamageClientRpc();
    }

    [ClientRpc]
    void FlashDamageClientRpc()
    {
        if (damageFlashDuration <= 0f) return;

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
            RestoreDamageFlash();
        }

        damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    IEnumerator DamageFlashRoutine()
    {
        CaptureDamageFlashMaterials();

        for (int i = 0; i < flashMaterials.Count; i++)
        {
            Color flashColor = Color.Lerp(flashOriginalColors[i], damageFlashColor, damageFlashAmount);
            flashMaterials[i].SetColor(flashColorProperties[i], flashColor);
        }

        yield return new WaitForSeconds(damageFlashDuration);

        RestoreDamageFlash();
        damageFlashCoroutine = null;
    }

    void CaptureDamageFlashMaterials()
    {
        flashMaterials = new List<Material>();
        flashColorProperties = new List<string>();
        flashOriginalColors = new List<Color>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer playerRenderer in renderers)
        {
            foreach (Material material in playerRenderer.materials)
            {
                string colorProperty = GetColorProperty(material);
                if (string.IsNullOrEmpty(colorProperty)) continue;

                flashMaterials.Add(material);
                flashColorProperties.Add(colorProperty);
                flashOriginalColors.Add(material.GetColor(colorProperty));
            }
        }
    }

    void RestoreDamageFlash()
    {
        if (flashMaterials == null) return;

        for (int i = 0; i < flashMaterials.Count; i++)
        {
            if (flashMaterials[i] != null)
                flashMaterials[i].SetColor(flashColorProperties[i], flashOriginalColors[i]);
        }
    }

    string GetColorProperty(Material material)
    {
        if (material == null) return null;
        if (material.HasProperty("_BaseColor")) return "_BaseColor";
        if (material.HasProperty("_Color")) return "_Color";
        return null;
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
