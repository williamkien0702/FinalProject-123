using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject coinPrefab;
    public GameObject fakeCoinPrefab;
    public GameObject bombPrefab;
    public GameObject speedPowerUpPrefab;
    public GameObject shieldPowerUpPrefab;
    public GameObject kingCoinPrefab;
    public GameObject wallPrefab;
    public GameObject blackHolePrefab;

    [Header("Round Settings")]
    [SerializeField] private float roundDuration = 90f;

    [Header("Spawn Counts")]
    [SerializeField] private int totalCoins = 80;
    [SerializeField] private int totalFakeCoins = 15;
    [SerializeField] private int totalBombs = 8;
    [SerializeField] private int totalSpeedPowerUps = 5;
    [SerializeField] private int totalShieldPowerUps = 5;
    [SerializeField] private int totalWalls = 25;
    [SerializeField] private int totalBlackHoles = 6;

    public static bool gameOver = false;
    public static string winnerText = "";
    public static float timeRemaining = 90f;
    public static Vector3 kingCoinPosition;
    public static bool kingCoinActive = false;

    private bool timerRunning = false;

    private readonly NetworkVariable<float> syncedTimeRemaining =
        new NetworkVariable<float>(90f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> syncedKingCoinActive =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> syncedKingCoinPosition =
        new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> syncedGameOver =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        syncedTimeRemaining.OnValueChanged += OnTimeChanged;
        syncedKingCoinActive.OnValueChanged += OnKingCoinActiveChanged;
        syncedKingCoinPosition.OnValueChanged += OnKingCoinPositionChanged;
        syncedGameOver.OnValueChanged += OnGameOverChanged;

        timeRemaining = syncedTimeRemaining.Value;
        kingCoinActive = syncedKingCoinActive.Value;
        kingCoinPosition = syncedKingCoinPosition.Value;
        gameOver = syncedGameOver.Value;

        if (IsServer)
        {
            StartGame();
        }
    }

    public override void OnNetworkDespawn()
    {
        syncedTimeRemaining.OnValueChanged -= OnTimeChanged;
        syncedKingCoinActive.OnValueChanged -= OnKingCoinActiveChanged;
        syncedKingCoinPosition.OnValueChanged -= OnKingCoinPositionChanged;
        syncedGameOver.OnValueChanged -= OnGameOverChanged;

        base.OnNetworkDespawn();
    }

    private void OnTimeChanged(float previous, float current)
    {
        timeRemaining = current;
    }

    private void OnKingCoinActiveChanged(bool previous, bool current)
    {
        kingCoinActive = current;
    }

    private void OnKingCoinPositionChanged(Vector3 previous, Vector3 current)
    {
        kingCoinPosition = current;
    }

    private void OnGameOverChanged(bool previous, bool current)
    {
        gameOver = current;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!IsNetworkReady()) return;
        if (!timerRunning) return;
        if (gameOver) return;

        syncedTimeRemaining.Value -= Time.deltaTime;
        if (syncedTimeRemaining.Value <= 0f)
        {
            syncedTimeRemaining.Value = 0f;
            EndGame();
        }

        timeRemaining = syncedTimeRemaining.Value;
    }

    private bool IsNetworkReady()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               IsSpawned;
    }

    private void StartGame()
    {
        if (!IsServer) return;
        if (!IsNetworkReady()) return;

        gameOver = false;
        winnerText = "";
        timerRunning = true;

        syncedGameOver.Value = false;
        syncedTimeRemaining.Value = roundDuration;
        syncedKingCoinActive.Value = false;
        syncedKingCoinPosition.Value = Vector3.zero;

        timeRemaining = roundDuration;
        kingCoinActive = false;
        kingCoinPosition = Vector3.zero;

        SpawnWalls();
        SpawnObjects(coinPrefab, totalCoins, 0.5f);
        SpawnObjects(fakeCoinPrefab, totalFakeCoins, 0.5f);
        SpawnObjects(bombPrefab, totalBombs, 0.5f);
        SpawnObjects(speedPowerUpPrefab, totalSpeedPowerUps, 0.5f);
        SpawnObjects(shieldPowerUpPrefab, totalShieldPowerUps, 0.5f);
        SpawnObjects(blackHolePrefab, totalBlackHoles, 0.8f);

        StartCoroutine(SpawnKingCoinAfterDelay(5f));
    }

    private void SpawnWalls()
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

            Vector3 scale = horizontal
                ? new Vector3(randomWallLength, wallHeight, wallThickness)
                : new Vector3(wallThickness, wallHeight, randomWallLength);

            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
            wall.transform.localScale = scale;

            NetworkObject netObj = wall.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            wallPositions.Add(pos);
            spawned++;
        }
    }

    private void SpawnObjects(GameObject prefab, int amount, float yPosition)
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
                pos = new Vector3(Random.Range(-30f, 30f), yPosition, Random.Range(-30f, 30f));

                foreach (Vector3 p in positions)
                {
                    if (Vector3.Distance(p, pos) < 1.5f)
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
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }
        }
    }

    private IEnumerator SpawnKingCoinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsServer) yield break;
        if (!IsNetworkReady()) yield break;
        if (gameOver) yield break;
        if (syncedKingCoinActive.Value) yield break;

        SpawnKingCoin();
    }

    private void SpawnKingCoin()
    {
        if (kingCoinPrefab == null) return;
        if (!IsServer) return;
        if (!IsNetworkReady()) return;

        Vector3 pos = new Vector3(Random.Range(-28f, 28f), 0.8f, Random.Range(-28f, 28f));
        GameObject kingCoin = Instantiate(kingCoinPrefab, pos, Quaternion.identity);

        NetworkObject netObj = kingCoin.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn();
        }

        syncedKingCoinPosition.Value = pos;
        syncedKingCoinActive.Value = true;

        kingCoinPosition = pos;
        kingCoinActive = true;
    }

    public void KingCoinCollected()
    {
        if (!IsServer) return;
        if (!IsNetworkReady()) return;

        syncedKingCoinActive.Value = false;
        syncedKingCoinPosition.Value = Vector3.zero;

        kingCoinActive = false;
        kingCoinPosition = Vector3.zero;

        StartCoroutine(SpawnKingCoinAfterDelay(10f));
    }

    public void CoinCollected()
    {
        if (!IsServer) return;
        // Timer-based game end; normal coins do not end the round.
    }

    private void EndGame()
    {
        if (gameOver) return;
        if (!IsServer) return;
        if (!IsNetworkReady()) return;

        gameOver = true;
        timerRunning = false;
        syncedGameOver.Value = true;

        int bestScore = int.MinValue;
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

        winnerText = tie ? "It's a tie!" : "Player " + winnerId + " wins!";
        ShowEndScreenClientRpc(winnerText);
    }

    [ClientRpc]
    private void ShowEndScreenClientRpc(string text)
    {
        gameOver = true;
        winnerText = text;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RestartGameServerRpc()
    {
        if (!IsServer) return;
        if (!IsNetworkReady()) return;

        StopAllCoroutines();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn != null)
            {
                pn.score.Value = 0;
            }

            var pm = playerObj.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.ResetStatusState();
            }
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
    private void RestartGameClientRpc()
    {
        gameOver = false;
        winnerText = "";
        timeRemaining = roundDuration;
        kingCoinActive = false;
        kingCoinPosition = Vector3.zero;
    }
}