using UnityEngine;
using Unity.Netcode;

public class TopDownFollowOwner : NetworkBehaviour
{
    public Vector3 offset = new Vector3(0, 20, 0);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        var cam = Camera.main;
        if (cam == null) return;

        cam.transform.position = transform.position + offset;
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void LateUpdate()
    {
        if (!IsOwner) return;

        var cam = Camera.main;
        if (cam == null) return;

        cam.transform.position = transform.position + offset;
    }
}