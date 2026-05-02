using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class LaserGridManager : NetworkBehaviour
{
    public GameObject laserLinePrefab;

    public float eventInterval = 10f;
    public float warningDuration = 2f;
    public float laserDuration = 1f;

    public int pointPenalty = 5;

    public float arenaMinX = -30f;
    public float arenaMaxX = 30f;
    public float arenaMinZ = -30f;
    public float arenaMaxZ = 30f;

    public float gridSpacing = 10f;
    public float laserLineWidth = 0.8f;

    // Laser beams now sit at roughly chest/eye height so they are
    // visible and meaningful in a first-person perspective.
    public float laserHeight = 1.2f;
    public float laserThickness = 0.4f;

    public static bool laserWarningActive = false;
    public static bool laserFiringActive = false;

    private List<GameObject> activeLaserLines = new List<GameObject>();

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
            {
                yield return StartCoroutine(RunLaserGridEvent());
            }

            yield return new WaitForSeconds(eventInterval);
        }
    }

    IEnumerator RunLaserGridEvent()
    {
        laserWarningActive = true;
        laserFiringActive = false;
        UpdateLaserStatusClientRpc(true, false);

        SpawnLaserGrid();

        yield return new WaitForSeconds(warningDuration);

        laserWarningActive = false;
        laserFiringActive = true;
        UpdateLaserStatusClientRpc(false, true);

        DamagePlayersOnGridLines();

        yield return new WaitForSeconds(laserDuration);

        laserFiringActive = false;
        UpdateLaserStatusClientRpc(false, false);

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

        activeLaserLines.Add(line);
    }

    void DamagePlayersOnGridLines()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            Vector3 playerPos = client.PlayerObject.transform.position;

            if (IsOnGridLine(playerPos))
            {
                PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
                if (playerNetwork == null) continue;

                playerNetwork.score.Value -= pointPenalty;
                if (playerNetwork.score.Value < 0) playerNetwork.score.Value = 0;
            }
        }
    }

    bool IsOnGridLine(Vector3 playerPos)
    {
        for (float x = arenaMinX; x <= arenaMaxX; x += gridSpacing)
        {
            if (Mathf.Abs(playerPos.x - x) <= laserLineWidth) return true;
        }

        for (float z = arenaMinZ; z <= arenaMaxZ; z += gridSpacing)
        {
            if (Mathf.Abs(playerPos.z - z) <= laserLineWidth) return true;
        }

        return false;
    }

    void ClearLaserGrid()
    {
        foreach (GameObject line in activeLaserLines)
        {
            if (line == null) continue;

            NetworkObject netObj = line.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
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