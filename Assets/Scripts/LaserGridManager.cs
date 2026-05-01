using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    public static bool laserWarningActive;
    public static bool laserFiringActive;

    private readonly List<GameObject> activeLaserLines = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(LaserLoop());
        }
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();
        ResetLocalState();
    }

    public void ResetRoundState()
    {
        if (!IsServer)
        {
            return;
        }

        StopAllCoroutines();
        ClearLaserGrid();
        ResetAndBroadcastState();
        StartCoroutine(LaserLoop());
    }

    private IEnumerator LaserLoop()
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

    private IEnumerator RunLaserGridEvent()
    {
        laserWarningActive = true;
        laserFiringActive = false;
        UpdateLaserStatusClientRpc(true, false);

        SpawnLaserGrid();
        yield return new WaitForSeconds(warningDuration);

        if (GameManager.gameOver)
        {
            ClearLaserGrid();
            ResetAndBroadcastState();
            yield break;
        }

        laserWarningActive = false;
        laserFiringActive = true;
        UpdateLaserStatusClientRpc(false, true);

        DamagePlayersOnGridLines();
        yield return new WaitForSeconds(laserDuration);

        ResetAndBroadcastState();
        ClearLaserGrid();
    }

    private void SpawnLaserGrid()
    {
        for (float x = arenaMinX; x <= arenaMaxX; x += gridSpacing)
        {
            Vector3 position = new Vector3(x, 0.25f, 0f);
            Vector3 scale = new Vector3(laserLineWidth, 0.15f, arenaMaxZ - arenaMinZ);
            SpawnLaserLine(position, scale);
        }

        for (float z = arenaMinZ; z <= arenaMaxZ; z += gridSpacing)
        {
            Vector3 position = new Vector3(0f, 0.25f, z);
            Vector3 scale = new Vector3(arenaMaxX - arenaMinX, 0.15f, laserLineWidth);
            SpawnLaserLine(position, scale);
        }
    }

    private void SpawnLaserLine(Vector3 position, Vector3 scale)
    {
        if (laserLinePrefab == null)
        {
            return;
        }

        GameObject line = Instantiate(laserLinePrefab, position, Quaternion.identity);
        line.transform.localScale = scale;

        NetworkObject netObj = line.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        activeLaserLines.Add(line);
    }

    private void DamagePlayersOnGridLines()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            Vector3 playerPosition = client.PlayerObject.transform.position;
            if (!IsOnGridLine(playerPosition))
            {
                continue;
            }

            PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
            PlayerMovement playerMovement = client.PlayerObject.GetComponent<PlayerMovement>();
            if (playerNetwork == null)
            {
                continue;
            }

            playerNetwork.score.Value = Mathf.Max(0, playerNetwork.score.Value - pointPenalty);
            if (playerMovement != null)
            {
                playerMovement.ShowOwnerNotification("-5 POINTS", "Laser grid hit.", ScoreUI.HudNoticeType.Danger, 1.5f);
            }
        }
    }

    private bool IsOnGridLine(Vector3 playerPosition)
    {
        for (float x = arenaMinX; x <= arenaMaxX; x += gridSpacing)
        {
            if (Mathf.Abs(playerPosition.x - x) <= laserLineWidth)
            {
                return true;
            }
        }

        for (float z = arenaMinZ; z <= arenaMaxZ; z += gridSpacing)
        {
            if (Mathf.Abs(playerPosition.z - z) <= laserLineWidth)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearLaserGrid()
    {
        for (int i = 0; i < activeLaserLines.Count; i++)
        {
            GameObject line = activeLaserLines[i];
            if (line == null)
            {
                continue;
            }

            NetworkObject netObj = line.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }

        activeLaserLines.Clear();
    }

    private void ResetAndBroadcastState()
    {
        laserWarningActive = false;
        laserFiringActive = false;
        UpdateLaserStatusClientRpc(false, false);
    }

    private void ResetLocalState()
    {
        laserWarningActive = false;
        laserFiringActive = false;
        activeLaserLines.Clear();
    }

    [ClientRpc]
    private void UpdateLaserStatusClientRpc(bool warning, bool firing)
    {
        laserWarningActive = warning;
        laserFiringActive = firing;
    }
}
