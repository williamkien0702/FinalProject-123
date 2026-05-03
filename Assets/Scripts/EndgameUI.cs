using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EndGameUI : MonoBehaviour
{
    private GameManager _gameManager;
    private readonly List<PlayerNetwork> _players = new List<PlayerNetwork>();
    private bool _playedEndSound = false;

    private void Awake()
    {
        _gameManager = Object.FindFirstObjectByType<GameManager>();
    }

    private void OnGUI()
    {
        if (!GameManager.gameOver)
        {
            _playedEndSound = false;
            return;
        }

        if (!_playedEndSound)
        {
            FineMarbleSfx.Instance?.PlayRoundEnd();
            _playedEndSound = true;
        }

        Rect panel = new Rect((Screen.width - 520f) * 0.5f, (Screen.height - 360f) * 0.5f, 520f, 360f);
        SimpleUiTheme.DrawPanel(panel, new Color(0.03f, 0.05f, 0.08f, 0.94f));

        GUI.Label(new Rect(panel.x + 20f, panel.y + 16f, panel.width - 40f, 42f), GameManager.winnerText, SimpleUiTheme.Warning);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 64f, panel.width - 40f, 24f), "FINAL STANDINGS", SimpleUiTheme.Body);

        CollectPlayers();

        float y = panel.y + 102f;
        for (int i = 0; i < _players.Count; i++)
        {
            PlayerNetwork pn = _players[i];
            if (pn == null)
            {
                continue;
            }

            string text = (i + 1) + ". " + PlayerMovement.GetPlayerLabel(pn.OwnerClientId) + "  -  " + pn.score.Value;
            GUI.Label(new Rect(panel.x + 30f, y, panel.width - 60f, 28f), text, SimpleUiTheme.Body);
            y += 30f;
        }

        if (GUI.Button(new Rect(panel.x + 60f, panel.y + panel.height - 80f, 160f, 44f), "Play Again", SimpleUiTheme.Button))
        {
            FineMarbleSfx.Instance?.PlayUiClick();
            _playedEndSound = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && _gameManager != null)
            {
                _gameManager.RestartGameServerRpc();
            }
        }

        if (GUI.Button(new Rect(panel.x + panel.width - 220f, panel.y + panel.height - 80f, 160f, 44f), "Exit Game", SimpleUiTheme.Button))
        {
            FineMarbleSfx.Instance?.PlayUiClick();
            QuitGame();
        }
    }

    private void CollectPlayers()
    {
        _players.Clear();
        PlayerNetwork[] all = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].IsSpawned)
            {
                _players.Add(all[i]);
            }
        }

        _players.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int scoreCompare = b.score.Value.CompareTo(a.score.Value);
            return scoreCompare != 0 ? scoreCompare : a.OwnerClientId.CompareTo(b.OwnerClientId);
        });
    }

    private void QuitGame()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}