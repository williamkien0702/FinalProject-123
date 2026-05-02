using UnityEngine;
using Unity.Netcode;

public class TopDownFollowOwner : NetworkBehaviour
{
    [Header("Third Person Settings")]
    public float mouseSensitivity = 2f;
    public float cameraDistance = 5f;    // How far behind the player
    public float cameraHeight = 2.5f;    // How high above the player
    public float minPitch = -20f;        // Look down limit
    public float maxPitch = 60f;         // Look up limit

    private Camera cam;
    private float pitch = 15f;
    private float yaw = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        cam = Camera.main;
        if (cam == null) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PositionCamera();
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        if (cam == null) return;
        if (GameManager.gameOver) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;  // Inverted fix: subtract so mouse up = look up
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetYaw(yaw);
        }

        PositionCamera();
    }

    void PositionCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Pull camera back and up, then raycast to prevent wall clipping
        Vector3 desiredOffset = rotation * new Vector3(0f, 0f, -cameraDistance);
        Vector3 targetPos = transform.position + Vector3.up * cameraHeight + desiredOffset;

        // Raycast from player center to desired camera position — if anything
        // is in the way, snap the camera in front of the obstruction
        Vector3 playerCenter = transform.position + Vector3.up * cameraHeight;
        Vector3 dir = targetPos - playerCenter;
        float dist = dir.magnitude;

        if (Physics.Raycast(playerCenter, dir.normalized, out RaycastHit hit, dist, LayerMask.GetMask("Wall")))
        {
            // Pull camera just in front of the wall hit point
            cam.transform.position = hit.point + dir.normalized * 0.2f;
        }
        else
        {
            cam.transform.position = targetPos;
        }

        cam.transform.LookAt(transform.position + Vector3.up * cameraHeight * 0.5f);
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}