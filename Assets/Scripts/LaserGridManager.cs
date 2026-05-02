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

    public static bool laserWarningActive = false;
    public static bool laserFiringActive = false;

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

        SpawnLaserGrid();
        SetLasersDamaging(false);

        yield return new WaitForSeconds(warningDuration);

        // Firing phase — enable damage on all laser colliders
        laserWarningActive = false;
        laserFiringActive = true;
        UpdateLaserStatusClientRpc(false, true);

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
    void UpdateLaserStatusClientRpc(bool warning, bool firing)
    {
        laserWarningActive = warning;
        laserFiringActive = firing;
    }
}