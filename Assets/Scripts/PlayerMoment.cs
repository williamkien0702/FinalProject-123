using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 100f;

    private Vector2 moveInput = Vector2.zero;

    void Update()
    {
        if (!IsOwner) return;
        if (GameManager.gameOver) return;

        float h = 0f;
        float v = 0f;

        // both players use WASD on their own PC
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h =  1f;
        if (Input.GetKey(KeyCode.W)) v =  1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;

        SubmitInputServerRpc(h, v);
    }
    // ServerRpc sends client movement input to the server
    [ServerRpc]
    void SubmitInputServerRpc(float h, float v)
    {
        moveInput = new Vector2(h, v);
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (GameManager.gameOver) return;

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        transform.position += move * speed * Time.fixedDeltaTime;
    }
}