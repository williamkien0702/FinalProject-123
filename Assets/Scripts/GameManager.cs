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
    public GameObject gunPickupPrefab;
    public GameObject monsterPrefab;

    [Header("Decorative Grass")]
    public GameObject[] grassPrefabs;
    public int totalGrass = 300;
    public float grassYPosition = 0.02f;
    public Vector2 grassScaleRange = new Vector2(0.7f, 1.4f);
    public float grassArenaHalfSize = 46f;
    public float grassWallClearance = 1.25f;
    public float grassPatchScale = 0.08f;
    [Range(0f, 1f)] public float grassPatchiness = 0.35f;

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
    private int totalGunPickups = 4;

    private int totalWalls = 50;
    private int totalBlackHoles = 6;
    private bool timerRunning = false;
    private int currentGrassSeed = 0;
    private GameObject grassRoot;
    private Coroutine grassSpawnRoutine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        StartGame();
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Wait one frame for the player object to be fully spawned
        StartCoroutine(MovePlayerToSafeSpawn(clientId));
        StartCoroutine(SpawnGrassForClientAfterConnect(clientId));
    }

    IEnumerator MovePlayerToSafeSpawn(ulong clientId)
    {
        yield return null;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) yield break;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj == null) yield break;

        Vector3 safePos = GetSafeSpawnPosition(1f);
        playerObj.transform.position = safePos;
    }

    IEnumerator SpawnGrassForClientAfterConnect(ulong clientId)
    {
        yield return null;
        yield return null;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) yield break;

        SpawnDecorativeGrassClientRpc(currentGrassSeed, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
    }

    Vector3 GetSafeSpawnPosition(float yPosition)
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(-46f, 46f),
                yPosition,
                Random.Range(-46f, 46f)
            );

            if (!Physics.CheckSphere(candidate, 1.5f, LayerMask.GetMask("Wall")))
                return candidate;
        }

        // Fallback to center if no safe position found
        return new Vector3(0f, yPosition, 0f);
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
        currentGrassSeed = Random.Range(0, int.MaxValue);
        SpawnDecorativeGrassClientRpc(currentGrassSeed);

        SpawnObjects(coinPrefab, totalCoins, 0.5f);
        SpawnObjects(fakeCoinPrefab, totalFakeCoins, 0.5f);
        SpawnObjects(bombPrefab, totalBombs, 0.7f);
        SpawnObjects(speedPowerUpPrefab, totalSpeedPowerUps, 1.5f);
        SpawnObjects(shieldPowerUpPrefab, totalShieldPowerUps, 1.5f);
        SpawnObjects(gunPickupPrefab, totalGunPickups, 1.5f);
        SpawnObjects(blackHolePrefab, totalBlackHoles, 0.8f);
        SpawnObjects(monsterPrefab, 2, 1f);
        StartCoroutine(SpawnKingCoinAfterDelay(5f));

        UpdateTimerClientRpc(timeRemaining);
    }

    void SpawnWalls()
    {
        if (wallPrefab == null) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 600;          // More attempts needed to fill larger area

        float minWallDistance = 14f;    // Scaled up from 9 to keep walls from clumping

        List<Vector3> wallPositions = new List<Vector3>();

        while (spawned < totalWalls && attempts < maxAttempts)
        {
            attempts++;

            Vector3 pos = new Vector3(
                Random.Range(-46f, 46f),    // ~100 unit arena, leaving 4u border
                1f,
                Random.Range(-46f, 46f)
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

            float randomWallLength = Random.Range(8f, 18f);  // Longer walls to fill space
            float wallThickness = 1f;
            float wallHeight = 34f;

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
            int attempts = 0;

            do
            {
                valid = true;
                pos = new Vector3(Random.Range(-46, 46), yPosition, Random.Range(-46, 46));

                // Reject if inside a wall
                if (Physics.CheckSphere(pos, 1.5f, LayerMask.GetMask("Wall")))
                {
                    valid = false;
                    attempts++;
                    continue;
                }

                // Reject if too close to another spawned object
                foreach (var p in positions)
                {
                    if (Vector3.Distance(p, pos) < 1.5f)
                    {
                        valid = false;
                        break;
                    }
                }

                attempts++;

            } while (!valid && attempts < 100);

            if (!valid) continue; // Skip if no valid position found after 100 attempts

            positions.Add(pos);

            GameObject obj = Instantiate(prefab, pos, prefab.transform.rotation);
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }

    [ClientRpc]
    void SpawnDecorativeGrassClientRpc(int seed, ClientRpcParams clientRpcParams = default)
    {
        ClearDecorativeGrass();
        grassSpawnRoutine = StartCoroutine(SpawnDecorativeGrassAfterWalls(seed));
    }

    IEnumerator SpawnDecorativeGrassAfterWalls(int seed)
    {
        // Give networked walls a moment to appear on clients before checking collisions.
        yield return null;
        yield return null;

        SpawnDecorativeGrass(seed);
        grassSpawnRoutine = null;
    }

    void SpawnDecorativeGrass(int seed)
    {
        if (grassPrefabs == null || grassPrefabs.Length == 0) return;
        if (totalGrass <= 0) return;

        grassRoot = new GameObject("Generated Grass");

        Random.State previousRandomState = Random.state;
        Random.InitState(seed);

        Vector2 noiseOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));
        float minimumPatchNoise = Mathf.Lerp(0f, 0.65f, grassPatchiness);
        int wallLayerMask = LayerMask.GetMask("Wall");

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = totalGrass * 20;

        while (spawned < totalGrass && attempts < maxAttempts)
        {
            attempts++;

            GameObject prefab = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
            if (prefab == null) continue;

            Vector3 pos = new Vector3(
                Random.Range(-grassArenaHalfSize, grassArenaHalfSize),
                grassYPosition,
                Random.Range(-grassArenaHalfSize, grassArenaHalfSize));

            float patchNoise = Mathf.PerlinNoise(
                pos.x * grassPatchScale + noiseOffset.x,
                pos.z * grassPatchScale + noiseOffset.y);

            if (patchNoise < minimumPatchNoise) continue;

            if (wallLayerMask != 0 &&
                Physics.CheckSphere(pos + Vector3.up * 0.5f, grassWallClearance, wallLayerMask))
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject grass = Instantiate(prefab, pos, rotation, grassRoot.transform);

            float minScale = Mathf.Min(grassScaleRange.x, grassScaleRange.y);
            float maxScale = Mathf.Max(grassScaleRange.x, grassScaleRange.y);
            float scale = Random.Range(minScale, maxScale);
            grass.transform.localScale = Vector3.Scale(grass.transform.localScale, new Vector3(scale, scale, scale));

            spawned++;
        }

        Random.state = previousRandomState;
    }

    void ClearDecorativeGrass()
    {
        if (grassSpawnRoutine != null)
        {
            StopCoroutine(grassSpawnRoutine);
            grassSpawnRoutine = null;
        }

        if (grassRoot != null)
        {
            Destroy(grassRoot);
            grassRoot = null;
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

        Vector3 pos = new Vector3(Random.Range(-28, 28), 1.5f, Random.Range(-28, 28));

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
                netObj.CompareTag("Wall") ||
                netObj.CompareTag("GunPickup") ||
                netObj.CompareTag("Bullet") ||
                netObj.CompareTag("Monster"))
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
