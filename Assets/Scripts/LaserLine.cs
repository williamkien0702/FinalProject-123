using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Rendering;

// Attach this to your laser line prefab alongside a BoxCollider set to Is Trigger.
// LaserGridManager toggles canDamage on/off to prevent damage during the warning phase.
public class LaserLine : NetworkBehaviour
{
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";

    public int pointPenalty = 5;
    public float damageCooldown = 1f;   // Seconds between damage ticks per player

    // Toggled by LaserGridManager — false during warning, true when firing
    [HideInInspector] public bool canDamage = false;

    // Tracks the last time each player was damaged so they can't be hit every frame
    private Dictionary<ulong, float> lastDamageTime = new Dictionary<ulong, float>();
    private Renderer laserRenderer;
    private Material laserMaterial;

    private void Awake()
    {
        laserRenderer = GetComponent<Renderer>();

        if (laserRenderer == null) return;

        laserMaterial = laserRenderer.material;
        MakeMaterialTransparent();
    }

    [ClientRpc]
    public void SetVisualClientRpc(bool visible, float alpha)
    {
        if (laserRenderer == null) return;

        laserRenderer.enabled = visible;
        SetAlpha(alpha);
    }

    private void OnDestroy()
    {
        if (laserMaterial != null)
            Destroy(laserMaterial);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        if (!canDamage) return;

        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

        if (playerNetwork == null || playerMovement == null) return;
        if (playerMovement.HasShield()) return;

        ulong clientId = playerNetwork.OwnerClientId;

        // Only damage if enough time has passed since the last hit
        float now = Time.time;
        if (lastDamageTime.TryGetValue(clientId, out float last) && now - last < damageCooldown)
            return;

        lastDamageTime[clientId] = now;

        playerNetwork.TakeDamage(pointPenalty);
    }

    private void MakeMaterialTransparent()
    {
        if (laserMaterial == null) return;

        if (laserMaterial.HasProperty("_Surface"))
            laserMaterial.SetFloat("_Surface", 1f);

        if (laserMaterial.HasProperty("_SrcBlend"))
            laserMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (laserMaterial.HasProperty("_DstBlend"))
            laserMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (laserMaterial.HasProperty("_ZWrite"))
            laserMaterial.SetFloat("_ZWrite", 0f);

        laserMaterial.SetOverrideTag("RenderType", "Transparent");
        laserMaterial.renderQueue = (int)RenderQueue.Transparent;
        laserMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private void SetAlpha(float alpha)
    {
        if (laserMaterial == null) return;

        alpha = Mathf.Clamp01(alpha);

        if (laserMaterial.HasProperty(BaseColorProperty))
        {
            Color color = laserMaterial.GetColor(BaseColorProperty);
            color.a = alpha;
            laserMaterial.SetColor(BaseColorProperty, color);
        }

        if (laserMaterial.HasProperty(ColorProperty))
        {
            Color color = laserMaterial.GetColor(ColorProperty);
            color.a = alpha;
            laserMaterial.SetColor(ColorProperty, color);
        }
    }
}
