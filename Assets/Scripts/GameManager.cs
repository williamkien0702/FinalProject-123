using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public GameObject coinPrefab;
    public GameObject fakeCoinPrefab;
    public GameObject bombPrefab;
    public GameObject speedPowerUpPrefab;
    public GameObject shieldPowerUpPrefab;
    public GameObject kingCoinPrefab;
    public GameObject wallPrefab;
    public GameObject blackHolePrefab;

    public static bool gameOver = false;
    public static string winnerText = string.Empty;
    public static float timeRemaining = 90f;
    public static Vector3 kingCoinPosition;
    public static bool kingCoinActive = false;

    private const int TotalCoins = 80;
    private const int TotalFakeCoins = 15;
    private const int TotalBombs = 8;
    private const int TotalSpeedPowerUps = 5;
    private const int TotalShieldPowerUps = 5;
    private const int TotalWalls = 25;
    private const int TotalBlackHoles = 6;

    private bool _timerRunning;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartGame();
        }
    }

    private void Update()
    {
        if (!IsServer || !_timerRunning || gameOver)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame();
        }

        UpdateTimerClientRpc(timeRemaining);
    }

    private void StartGame()
    {
        gameOver = false;
        winnerText = string.Empty;
        timeRemaining = 90f;
        _timerRunning = true;
        kingCoinActive = false;

        SpawnWalls();
        SpawnObjects(coinPrefab, TotalCoins, 0.5f);
        SpawnObjects(fakeCoinPrefab, TotalFakeCoins, 0.5f);
        SpawnObjects(bombPrefab, TotalBombs, 0.5f);
        SpawnObjects(speedPowerUpPrefab, TotalSpeedPowerUps, 0.5f);
        SpawnObjects(shieldPowerUpPrefab, TotalShieldPowerUps, 0.5f);
        SpawnObjects(blackHolePrefab, TotalBlackHoles, 0.8f);
        StartCoroutine(SpawnKingCoinAfterDelay(5f));

        UpdateTimerClientRpc(timeRemaining);
        UpdateKingCoinClientRpc(Vector3.zero, false);
        RestartGameClientRpc();
    }

    private void SpawnWalls()
    {
        if (wallPrefab == null)
        {
            return;
        }

        int spawned = 0;
        int attempts = 0;
        const int maxAttempts = 300;
        const float minWallDistance = 9f;
        List<Vector3> wallPositions = new List<Vector3>();

        while (spawned < TotalWalls && attempts < maxAttempts)
        {
            attempts++;
            Vector3 pos = new Vector3(Random.Range(-26f, 26f), 1f, Random.Range(-26f, 26f));

            bool tooClose = false;
            for (int i = 0; i < wallPositions.Count; i++)
            {
                if (Vector3.Distance(pos, wallPositions[i]) < minWallDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            bool horizontal = Random.value > 0.5f;
            float randomWallLength = Random.Range(5f, 10f);
            float wallThickness = 1f;
            float wallHeight = 2f;
            Vector3 scale = horizontal
                ? new Vector3(randomWallLength, wallHeight, wallThickness)
                : new Vector3(wallThickness, wallHeight, randomWallLength);

            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
            wall.transform.localScale = scale;
            NetworkObject netObj = wall.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            wallPositions.Add(pos);
            spawned++;
        }
    }

    private void SpawnObjects(GameObject prefab, int amount, float yPosition)
    {
        if (prefab == null)
        {
            return;
        }

        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < amount; i++)
        {
            Vector3 pos;
            bool valid;

            do
            {
                valid = true;
                pos = new Vector3(Random.Range(-30f, 30f), yPosition, Random.Range(-30f, 30f));
                for (int p = 0; p < positions.Count; p++)
                {
                    if (Vector3.Distance(positions[p], pos) < 1.5f)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            while (!valid);

            positions.Add(pos);
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
    }

    private IEnumerator SpawnKingCoinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsServer || gameOver || kingCoinActive)
        {
            yield break;
        }

        SpawnKingCoin();
    }

    private void SpawnKingCoin()
    {
        if (kingCoinPrefab == null)
        {
            return;
        }

        Vector3 pos = new Vector3(Random.Range(-28f, 28f), 0.8f, Random.Range(-28f, 28f));
        GameObject kingCoin = Instantiate(kingCoinPrefab, pos, Quaternion.identity);
        NetworkObject netObj = kingCoin.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        kingCoinPosition = pos;
        kingCoinActive = true;
        UpdateKingCoinClientRpc(pos, true);
        BroadcastUiToastClientRpc("KING COIN ACTIVE", new Color(1f, 0.87f, 0.2f), 1.8f);
    }

    public void KingCoinCollected()
    {
        if (!IsServer)
        {
            return;
        }

        kingCoinActive = false;
        UpdateKingCoinClientRpc(Vector3.zero, false);
        StartCoroutine(SpawnKingCoinAfterDelay(10f));
    }

    public void CoinCollected()
    {
        if (!IsServer)
        {
            return;
        }
    }

    private void EndGame()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        _timerRunning = false;

        int bestScore = int.MinValue;
        int winnerId = -1;
        bool tie = false;

        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        foreach (PlayerNetwork pn in players)
        {
            if (pn == null || !pn.IsSpawned)
            {
                continue;
            }

            int score = pn.score.Value;
            if (score > bestScore)
            {
                bestScore = score;
                winnerId = (int)pn.OwnerClientId + 1;
                tie = false;
            }
            else if (score == bestScore)
            {
                tie = true;
            }
        }

        winnerText = tie ? "It's a tie!" : "Player " + winnerId + " wins!";
        ShowEndScreenClientRpc(winnerText);
    }

    [ClientRpc]
    private void ShowEndScreenClientRpc(string text)
    {
        gameOver = true;
        winnerText = text;
        UiEventFeed.ClearAll();
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(float time)
    {
        timeRemaining = time;
    }

    [ClientRpc]
    private void UpdateKingCoinClientRpc(Vector3 pos, bool active)
    {
        kingCoinPosition = pos;
        kingCoinActive = active;
    }

    [ClientRpc]
    private void BroadcastUiToastClientRpc(string message, Color color, float duration)
    {
        UiEventFeed.Push(message, color, duration);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RestartGameServerRpc()
    {
        StopAllCoroutines();

        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        foreach (PlayerNetwork pn in players)
        {
            if (pn == null)
            {
                continue;
            }

            pn.score.Value = 0;

            PlayerMovement movement = pn.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.ResetStatusState();
            }
        }

        NetworkObject[] netObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (NetworkObject netObj in netObjects)
        {
            if (netObj == null || !netObj.IsSpawned)
            {
                continue;
            }

            if (netObj.CompareTag("Coin") ||
                netObj.CompareTag("Trap") ||
                netObj.CompareTag("PowerUp") ||
                netObj.CompareTag("KingCoin") ||
                netObj.CompareTag("Wall"))
            {
                netObj.Despawn(true);
            }
        }

        StartGame();
    }

    [ClientRpc]
    private void RestartGameClientRpc()
    {
        gameOver = false;
        winnerText = string.Empty;
        timeRemaining = 90f;
        kingCoinActive = false;
        UiEventFeed.ClearAll();
    }
}
