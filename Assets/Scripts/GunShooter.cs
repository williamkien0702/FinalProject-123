using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GunShooter : NetworkBehaviour
{
    [Header("Gun Settings")]
    public GameObject bulletPrefab;
    public int maxAmmo = 5;
    public Transform bulletSpawnPoint;

    [Header("SFX")]
    public AudioSource gunShotAudio;    // Drag the AudioSource here in Inspector

    // Synced so ScoreUI can show ammo count on the owner's screen
    public NetworkVariable<int> ammo = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> hasGun = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    void Update()
    {
        if (!IsOwner) return;
        if (!hasGun.Value) return;
        if (GameManager.gameOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            FireServerRpc();

            // Play sound locally so there's no network delay on the shot audio
            if (gunShotAudio != null) gunShotAudio.Play();
        }
    }

    // Called by GunPickup on the server
    public void PickUpGun()
    {
        if (!IsServer) return;

        ammo.Value = maxAmmo;
        hasGun.Value = true;
    }

    void LoseGun()
    {
        hasGun.Value = false;
        ammo.Value = 0;
    }

    [ServerRpc]
    void FireServerRpc()
    {
        if (!hasGun.Value) return;
        if (ammo.Value <= 0) return;

        ammo.Value--;

        // Spawn bullet at barrel position facing player's forward direction
        Vector3 spawnPos = bulletSpawnPoint != null
            ? bulletSpawnPoint.position
            : transform.position + transform.forward * 1f + Vector3.up * 1.2f;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, transform.rotation);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.SetOwner(OwnerClientId);

        bulletObj.GetComponent<NetworkObject>().Spawn();

        // Out of ammo — take the gun away
        if (ammo.Value <= 0)
            LoseGun();
    }
}