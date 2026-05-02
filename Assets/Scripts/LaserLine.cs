using UnityEngine;
using Unity.Netcode;

// Attach this to your laser line prefab alongside a BoxCollider set to Is Trigger.
// LaserGridManager toggles canDamage on/off to prevent damage during the warning phase.
public class LaserLine : NetworkBehaviour
{
    public int pointPenalty = 5;

    // Toggled by LaserGridManager — false during warning, true when firing
    [HideInInspector] public bool canDamage = false;

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        if (!canDamage) return;

        PlayerNetwork playerNetwork = other.GetComponent<PlayerNetwork>();
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

        if (playerNetwork == null || playerMovement == null) return;

        if (playerMovement.HasShield()) return;

        playerNetwork.score.Value -= pointPenalty;
        if (playerNetwork.score.Value < 0) playerNetwork.score.Value = 0;
    }
}