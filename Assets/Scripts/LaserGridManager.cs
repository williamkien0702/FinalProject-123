using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class LaserGridManager : NetworkBehaviour
{
    public GameObject laserLinePrefab;

    public float eventInterval = 10f;
    public float warningDuration = 2f;
    public float laserDuration = 2f;

    public float arenaMinX = -50f;
    public float arenaMaxX = 50f;
    public float arenaMinZ = -50f;
    public float arenaMaxZ = 50f;

    public float gridSpacing = 20f;
    public float laserLineWidth = 0.8f;
    public float laserHeight = 1.2f;
    public float laserThickness = 0.4f;

    [Header("Warning Visual")]
    public float warningPreviewDuration = 0.5f;
    [Range(0f, 1f)] public float warningPreviewAlpha = 0.35f;

    public static bool laserWarningActive = false;
    public static bool laserFiringActive = false;

    [Header("SFX")]
    public AudioSource laserWarningAudio;   // Drag AudioSource here in Inspector

    private List<LaserLine> activeLaserLines = new List<LaserLine>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(LaserLoop());
    }

    IEnumerator LaserLoop()
    {
        yield return new WaitForSeconds(5f);

        while (true)
        {
            if (!GameManager.gameOver)
                yield return StartCoroutine(RunLaserGridEvent());

            yield return new WaitForSeconds(eventInterval);
        }
    }

    IEnumerator RunLaserGridEvent()
    {
        // Warning phase — lasers are visible but canDamage is false
        laserWarningActive = true;
        laserFiringActive = false;
        UpdateLaserStatusClientRpc(true, false);

        // Play warning sound on all clients
        if (laserWarningAudio != null && laserWarningAudio.clip != null)
            PlayWarningSoundClientRpc();

        SpawnLaserGrid();
        SetLasersDamaging(false);

        SetLaserVisuals(true, warningPreviewAlpha);
        float totalWarningDuration = Mathf.Max(0f, warningDuration);
        float previewDuration = Mathf.Min(Mathf.Max(0f, warningPreviewDuration), totalWarningDuration);
        yield return new WaitForSeconds(previewDuration);

        SetLaserVisuals(false, warningPreviewAlpha);
        yield return new WaitForSeconds(totalWarningDuration - previewDuration);

        // Firing phase — enable damage on all laser colliders
        laserWarningActive = false;
        laserFiringActive = true;
        UpdateLaserStatusClientRpc(false, true);

        SetLaserVisuals(true, 1f);
        SetLasersDamaging(true);

        yield return new WaitForSeconds(laserDuration);

        // Done — disable damage and despawn
        laserFiringActive = false;
        UpdateLaserStatusClientRpc(false, false);

        SetLasersDamaging(false);
        ClearLaserGrid();
    }

    void SpawnLaserGrid()
    {
        // Vertical lines (extend along Z axis)
        for (float x = arenaMinX; x <= arenaMaxX; x += gridSpacing)
        {
            Vector3 position = new Vector3(x, laserHeight, 0f);
            Vector3 scale = new Vector3(laserLineWidth, laserThickness, arenaMaxZ - arenaMinZ);
            SpawnLaserLine(position, scale);
        }

        // Horizontal lines (extend along X axis)
        for (float z = arenaMinZ; z <= arenaMaxZ; z += gridSpacing)
        {
            Vector3 position = new Vector3(0f, laserHeight, z);
            Vector3 scale = new Vector3(arenaMaxX - arenaMinX, laserThickness, laserLineWidth);
            SpawnLaserLine(position, scale);
        }
    }

    void SpawnLaserLine(Vector3 position, Vector3 scale)
    {
        if (laserLinePrefab == null) return;

        GameObject line = Instantiate(laserLinePrefab, position, Quaternion.identity);
        line.transform.localScale = scale;

        NetworkObject netObj = line.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        LaserLine laserLine = line.GetComponent<LaserLine>();
        if (laserLine != null)
            activeLaserLines.Add(laserLine);
    }

    void SetLasersDamaging(bool damaging)
    {
        foreach (LaserLine line in activeLaserLines)
        {
            if (line != null)
                line.canDamage = damaging;
        }
    }

    void SetLaserVisuals(bool visible, float alpha)
    {
        foreach (LaserLine line in activeLaserLines)
        {
            if (line != null)
                line.SetVisualClientRpc(visible, alpha);
        }
    }

    void ClearLaserGrid()
    {
        foreach (LaserLine line in activeLaserLines)
        {
            if (line == null) continue;

            NetworkObject netObj = line.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
        }

        activeLaserLines.Clear();
    }

    [ClientRpc]
    void PlayWarningSoundClientRpc()
    {
        if (laserWarningAudio != null && laserWarningAudio.clip != null)
            laserWarningAudio.PlayOneShot(laserWarningAudio.clip);
    }

    [ClientRpc]
    void UpdateLaserStatusClientRpc(bool warning, bool firing)
    {
        laserWarningActive = warning;
        laserFiringActive = firing;
    }
}
