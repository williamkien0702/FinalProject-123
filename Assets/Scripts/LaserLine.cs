using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Attach this to your laser line prefab alongside a BoxCollider set to Is Trigger.
// LaserGridManager toggles canDamage on/off to prevent damage during the warning phase.
public class LaserLine : NetworkBehaviour
{
    public int pointPenalty = 5;
    public float damageCooldown = 1f;   // Seconds between damage ticks per player

    // Toggled by LaserGridManager — false during warning, true when firing
    [HideInInspector] public bool canDamage = false;

    // Tracks the last time each player was damaged so they can't be hit every frame
    private Dictionary<ulong, float> lastDamageTime = new Dictionary<ulong, float>();

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

        playerNetwork.score.Value -= pointPenalty;
        if (playerNetwork.score.Value < 0) playerNetwork.score.Value = 0;
    }
}