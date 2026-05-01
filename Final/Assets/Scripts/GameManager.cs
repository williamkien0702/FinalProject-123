using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

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
    public static string winnerText = "";

    public static float timeRemaining = 90f;
    public static Vector3 kingCoinPosition;
    public static bool kingCoinActive = false;

    private int totalCoins = 80;
    private int totalFakeCoins = 15;
    private int totalBombs = 8;
    private int totalSpeedPowerUps = 5;
    private int totalShieldPowerUps = 5;

    private int totalWalls = 25;
    private int totalBlackHoles = 6;
    private bool timerRunning = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartGame();
    }

    void Update()
    {
        if (!IsServer) return;
        if (!timerRunning) return;
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame();
        }

        UpdateTimerClientRpc(timeRemaining);
    }

    void StartGame()
    {
        gameOver = false;
        winnerText = "";
        timeRemaining = 90f;
        timerRunning = true;
        kingCoinActive = false;

        SpawnWalls();

        SpawnObjects(coinPrefab, totalCoins, 0.5f);
        SpawnObjects(fakeCoinPrefab, totalFakeCoins, 0.5f);
        SpawnObjects(bombPrefab, totalBombs, 0.5f);
        SpawnObjects(speedPowerUpPrefab, totalSpeedPowerUps, 0.5f);
        SpawnObjects(shieldPowerUpPrefab, totalShieldPowerUps, 0.5f);
        SpawnObjects(blackHolePrefab, totalBlackHoles, 0.8f);
        StartCoroutine(SpawnKingCoinAfterDelay(5f));

        UpdateTimerClientRpc(timeRemaining);
    }

    void SpawnWalls()
    {
        if (wallPrefab == null) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 300;

        float minWallDistance = 9f;

        List<Vector3> wallPositions = new List<Vector3>();

        while (spawned < totalWalls && attempts < maxAttempts)
        {
            attempts++;

            Vector3 pos = new Vector3(
                Random.Range(-26f, 26f),
                1f,
                Random.Range(-26f, 26f)
            );

            bool tooClose = false;

            foreach (Vector3 existingPos in wallPositions)
            {
                if (Vector3.Distance(pos, existingPos) < minWallDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            bool horizontal = Random.value > 0.5f;

            float randomWallLength = Random.Range(5f, 10f);
            float wallThickness = 1f;
            float wallHeight = 2f;

            Vector3 scale;

            if (horizontal)
            {
                // Horizontal wall, long on X axis
                scale = new Vector3(randomWallLength, wallHeight, wallThickness);
            }
            else
            {
                // Vertical wall, long on Z axis
                scale = new Vector3(wallThickness, wallHeight, randomWallLength);
            }

            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
            wall.transform.localScale = scale;
            wall.GetComponent<NetworkObject>().Spawn();

            wallPositions.Add(pos);
            spawned++;
        }
    }


    void SpawnObjects(GameObject prefab, int amount, float yPosition)
    {
        if (prefab == null) return;

        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < amount; i++)
        {
            Vector3 pos;
            bool valid;

            do
            {
                valid = true;
                pos = new Vector3(Random.Range(-30, 30), yPosition, Random.Range(-30, 30));

                foreach (var p in positions)
                {
                    if (Vector3.Distance(p, pos) < 1.5f)
                    {
                        valid = false;
                        break;
                    }
                }

            } while (!valid);

            positions.Add(pos);

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }

    IEnumerator SpawnKingCoinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsServer) yield break;
        if (gameOver) yield break;
        if (kingCoinActive) yield break;

        SpawnKingCoin();
    }

    void SpawnKingCoin()
    {
        if (kingCoinPrefab == null) return;

        Vector3 pos = new Vector3(Random.Range(-28, 28), 0.8f, Random.Range(-28, 28));

        GameObject kingCoin = Instantiate(kingCoinPrefab, pos, Quaternion.identity);
        kingCoin.GetComponent<NetworkObject>().Spawn();

        kingCoinPosition = pos;
        kingCoinActive = true;

        UpdateKingCoinClientRpc(pos, true);
    }

    public void KingCoinCollected()
    {
        if (!IsServer) return;

        kingCoinActive = false;
        UpdateKingCoinClientRpc(Vector3.zero, false);

        StartCoroutine(SpawnKingCoinAfterDelay(10f));
    }

    public void CoinCollected()
    {
        if (!IsServer) return;

        // Normal coins no longer end the game.
        // The timer decides when the game ends.
    }

    void EndGame()
    {
        if (gameOver) return;

        gameOver = true;
        timerRunning = false;

        int bestScore = -1;
        int winnerId = -1;
        bool tie = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn == null) continue;

            if (pn.score.Value > bestScore)
            {
                bestScore = pn.score.Value;
                winnerId = (int)client.ClientId + 1;
                tie = false;
            }
            else if (pn.score.Value == bestScore)
            {
                tie = true;
            }
        }

        if (tie)
            winnerText = "It's a tie!";
        else
            winnerText = "Player " + winnerId + " wins!";

        ShowEndScreenClientRpc(winnerText);
    }

    [ClientRpc]
    void ShowEndScreenClientRpc(string text)
    {
        gameOver = true;
        winnerText = text;
    }

    [ClientRpc]
    void UpdateTimerClientRpc(float time)
    {
        timeRemaining = time;
    }

    [ClientRpc]
    void UpdateKingCoinClientRpc(Vector3 pos, bool active)
    {
        kingCoinPosition = pos;
        kingCoinActive = active;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RestartGameServerRpc()
    {
        StopAllCoroutines();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn == null) continue;

            pn.score.Value = 0;
        }

        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (!netObj.IsSpawned) continue;

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
        RestartGameClientRpc();
    }

    [ClientRpc]
    void RestartGameClientRpc()
    {
        gameOver = false;
        winnerText = "";
        timeRemaining = 90f;
        kingCoinActive = false;
    }
}