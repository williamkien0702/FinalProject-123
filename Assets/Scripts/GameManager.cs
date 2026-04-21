using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public GameObject coinPrefab;

    public static bool gameOver = false;
    public static string winnerText = "";

    private int totalCoins = 100;
    private int coinsRemaining = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartGame();
    }

    void StartGame()
    {
        gameOver = false;
        winnerText = "";
        coinsRemaining = totalCoins;

        SpawnCoins();
    }

    void SpawnCoins()
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < totalCoins; i++)
        {
            Vector3 pos;
            bool valid;

            do
            {
                valid = true;
                pos = new Vector3(Random.Range(-30, 30), 0.5f, Random.Range(-30, 30));

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

            GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity);
            coin.GetComponent<NetworkObject>().Spawn();
        }
    }

    public void CoinCollected()
    {
        if (!IsServer) return;

        coinsRemaining--;
        

        if (coinsRemaining <= 0)
        {   
            EndGame();
        }
    }   

    void EndGame()
    {
        gameOver = true;

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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RestartGameServerRpc()
    {
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
            if (netObj.CompareTag("Coin") && netObj.IsSpawned)
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
    }
}