using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    public static bool gameOver;
    public static string winnerText = string.Empty;
    public static float timeRemaining = 90f;
    public static Vector3 kingCoinPosition;
    public static bool kingCoinActive;

    private readonly int totalCoins = 80;
    private readonly int totalFakeCoins = 15;
    private readonly int totalBombs = 8;
    private readonly int totalSpeedPowerUps = 5;
    private readonly int totalShieldPowerUps = 5;
    private readonly int totalWalls = 25;
    private readonly int totalBlackHoles = 6;

    private bool timerRunning;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        StartGame();
    }

    private void Update()
    {
        if (!IsServer || !timerRunning || gameOver)
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
        timerRunning = true;
        kingCoinActive = false;
        kingCoinPosition = Vector3.zero;

        ResetPlayersForRound();

        SpawnWalls();
        SpawnObjects(coinPrefab, totalCoins, 0.5f);
        SpawnObjects(fakeCoinPrefab, totalFakeCoins, 0.5f);
        SpawnObjects(bombPrefab, totalBombs, 0.5f);
        SpawnObjects(speedPowerUpPrefab, totalSpeedPowerUps, 0.5f);
        SpawnObjects(shieldPowerUpPrefab, totalShieldPowerUps, 0.5f);
        SpawnObjects(blackHolePrefab, totalBlackHoles, 0.8f);

        StartCoroutine(SpawnKingCoinAfterDelay(5f));
        UpdateTimerClientRpc(timeRemaining);
        UpdateKingCoinClientRpc(Vector3.zero, false);
    }

    private void ResetPlayersForRound()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
            if (playerNetwork != null)
            {
                playerNetwork.score.Value = 0;
            }

            PlayerMovement playerMovement = client.PlayerObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ResetRoundState();
            }
        }
    }

    private void SpawnWalls()
    {
        if (wallPrefab == null)
        {
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 300;
        float minWallDistance = 9f;
        List<Vector3> wallPositions = new List<Vector3>();

        while (spawned < totalWalls && attempts < maxAttempts)
        {
            attempts++;

            Vector3 position = new Vector3(Random.Range(-26f, 26f), 1f, Random.Range(-26f, 26f));
            bool tooClose = false;

            for (int i = 0; i < wallPositions.Count; i++)
            {
                if (Vector3.Distance(position, wallPositions[i]) < minWallDistance)
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

            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
            wall.transform.localScale = scale;
            wall.GetComponent<NetworkObject>().Spawn();
            wallPositions.Add(position);
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
            Vector3 position;
            bool valid;

            do
            {
                valid = true;
                position = new Vector3(Random.Range(-30f, 30f), yPosition, Random.Range(-30f, 30f));

                for (int j = 0; j < positions.Count; j++)
                {
                    if (Vector3.Distance(positions[j], position) < 1.5f)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            while (!valid);

            positions.Add(position);
            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn();
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

        Vector3 position = new Vector3(Random.Range(-28f, 28f), 0.8f, Random.Range(-28f, 28f));
        GameObject kingCoin = Instantiate(kingCoinPrefab, position, Quaternion.identity);
        kingCoin.GetComponent<NetworkObject>().Spawn();

        kingCoinPosition = position;
        kingCoinActive = true;
        UpdateKingCoinClientRpc(position, true);
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
        timerRunning = false;

        int bestScore = -1;
        int winnerId = -1;
        bool tie = false;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            PlayerNetwork playerNetwork = client.PlayerObject.GetComponent<PlayerNetwork>();
            if (playerNetwork == null)
            {
                continue;
            }

            if (playerNetwork.score.Value > bestScore)
            {
                bestScore = playerNetwork.score.Value;
                winnerId = (int)client.ClientId + 1;
                tie = false;
            }
            else if (playerNetwork.score.Value == bestScore)
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
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(float time)
    {
        timeRemaining = time;
    }

    [ClientRpc]
    private void UpdateKingCoinClientRpc(Vector3 position, bool active)
    {
        kingCoinPosition = position;
        kingCoinActive = active;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RestartGameServerRpc()
    {
        StopAllCoroutines();

        foreach (NetworkObject netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (!netObj.IsSpawned)
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

        LaserGridManager laserGridManager = Object.FindFirstObjectByType<LaserGridManager>();
        if (laserGridManager != null)
        {
            laserGridManager.ResetRoundState();
        }

        StartGame();
        RestartGameClientRpc();
    }

    [ClientRpc]
    private void RestartGameClientRpc()
    {
        gameOver = false;
        winnerText = string.Empty;
        timeRemaining = 90f;
        kingCoinActive = false;
        kingCoinPosition = Vector3.zero;
        ScoreUI.ClearLocalNotifications();
    }
}
