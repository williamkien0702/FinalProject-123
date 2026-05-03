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

    public static bool laserWarningActive = false;
    public static bool laserFiringActive = false;

    private readonly List<GameObject> _activeLaserLines = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(LaserLoop());
        }
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

        FineMarbleSfx.Instance?.PlayLaserWarning();

        SpawnLaserGrid();
        yield return new WaitForSeconds(warningDuration);

        laserWarningActive = false;
        laserFiringActive = true;
        UpdateLaserStatusClientRpc(false, true);

        FineMarbleSfx.Instance?.PlayLaserHit();

        DamagePlayersOnGridLines();
        yield return new WaitForSeconds(laserDuration);

        laserFiringActive = false;
        UpdateLaserStatusClientRpc(false, false);
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

        _activeLaserLines.Add(line);
    }

    private void DamagePlayersOnGridLines()
    {
        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerNetwork playerNetwork = players[i];
            if (playerNetwork == null || !playerNetwork.IsSpawned)
            {
                continue;
            }

            Vector3 playerPos = playerNetwork.transform.position;
            if (!IsOnGridLine(playerPos))
            {
                continue;
            }

            playerNetwork.score.Value = Mathf.Max(0, playerNetwork.score.Value - pointPenalty);

            PlayerMovement movement = playerNetwork.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.NotifyLaserDamage(pointPenalty);
            }

            if (playerNetwork.IsOwner)
            {
                FineMarbleSfx.Instance?.PlayLaserHit();
            }
        }
    }

    private bool IsOnGridLine(Vector3 playerPos)
    {
        for (float x = arenaMinX; x <= arenaMaxX; x += gridSpacing)
        {
            if (Mathf.Abs(playerPos.x - x) <= laserLineWidth)
            {
                return true;
            }
        }

        for (float z = arenaMinZ; z <= arenaMaxZ; z += gridSpacing)
        {
            if (Mathf.Abs(playerPos.z - z) <= laserLineWidth)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearLaserGrid()
    {
        for (int i = 0; i < _activeLaserLines.Count; i++)
        {
            GameObject line = _activeLaserLines[i];
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

        _activeLaserLines.Clear();
    }

    [ClientRpc]
    private void UpdateLaserStatusClientRpc(bool warning, bool firing)
    {
        laserWarningActive = warning;
        laserFiringActive = firing;

        if (warning)
        {
            UiEventFeed.Push("LASER WARNING", new Color(1f, 0.85f, 0.2f), 1.1f);
        }
        else if (firing)
        {
            UiEventFeed.Push("LASER FIRING", new Color(1f, 0.35f, 0.35f), 0.9f);
        }
    }
}