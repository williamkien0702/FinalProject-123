using UnityEngine;
using Unity.Netcode;

public class TopDownFollowOwner : NetworkBehaviour
{
    [Header("First Person Settings")]
    public float mouseSensitivity = 2f;
    public float cameraHeight = 1.7f;       // Eye height above player pivot
    public float minPitch = -80f;           // Look down limit
    public float maxPitch = 80f;            // Look up limit

    private Camera cam;
    private float pitch = 0f;              // Vertical angle (camera only)
    private float yaw = 0f;               // Horizontal angle (whole player body)

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        cam = Camera.main;
        if (cam == null) return;

        // Lock and hide cursor for FPS look
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PositionCamera();
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        if (cam == null) return;
        if (GameManager.gameOver) return;

        // Read mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Rotate the player body horizontally on the server via the movement script
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetYaw(yaw);
        }

        PositionCamera();
    }

    void PositionCamera()
    {
        // Place camera at eye level, rotated by pitch + yaw
        cam.transform.position = transform.position + Vector3.up * cameraHeight;
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // Called by UI scripts that need to unlock the cursor (e.g. end screen)
    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}