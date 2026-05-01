using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EndGameUI : MonoBehaviour
{
    private GameManager gameManager;

    private void Awake()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
    }

    private void OnGUI()
    {
        if (!GameManager.gameOver)
        {
            return;
        }

        SimpleUiTheme.Ensure();

        SimpleUiTheme.DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), SimpleUiTheme.Overlay);

        float width = SimpleUiTheme.Px(680f);
        float height = SimpleUiTheme.Px(520f);
        Rect panelRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        SimpleUiTheme.DrawPanel(panelRect, SimpleUiTheme.PanelDarkColor, SimpleUiTheme.Accent, SimpleUiTheme.Px(2f));

        Rect contentRect = SimpleUiTheme.Pad(panelRect, SimpleUiTheme.Px(18f));

        GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, SimpleUiTheme.Px(40f)), "MATCH COMPLETE", SimpleUiTheme.TitleStyle);

        GUIStyle winnerStyle = new GUIStyle(SimpleUiTheme.CenterStyle);
        winnerStyle.normal.textColor = SimpleUiTheme.Accent;
        winnerStyle.fontSize = SimpleUiTheme.PxInt(24);
        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(48f), contentRect.width, SimpleUiTheme.Px(34f)), GameManager.winnerText, winnerStyle);

        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(90f), contentRect.width, SimpleUiTheme.Px(24f)), "Final standings", SimpleUiTheme.SmallCenterStyle);

        DrawFinalScores(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(128f), contentRect.width, SimpleUiTheme.Px(220f)));

        float buttonWidth = SimpleUiTheme.Px(220f);
        float buttonHeight = SimpleUiTheme.Px(52f);
        float buttonY = panelRect.yMax - SimpleUiTheme.Px(90f);
        float spacing = SimpleUiTheme.Px(16f);
        float leftX = (Screen.width - (buttonWidth * 2f + spacing)) * 0.5f;

        if (GUI.Button(new Rect(leftX, buttonY, buttonWidth, buttonHeight), "Play Again", SimpleUiTheme.ButtonStyle))
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (gameManager == null)
                {
                    gameManager = Object.FindFirstObjectByType<GameManager>();
                }

                if (gameManager != null)
                {
                    gameManager.RestartGameServerRpc();
                }
            }
        }

        if (GUI.Button(new Rect(leftX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight), "Exit Game", SimpleUiTheme.ButtonStyle))
        {
            QuitGame();
        }
    }

    private void DrawFinalScores(Rect rect)
    {
        List<PlayerNetwork> players = GetPlayers();
        float rowHeight = SimpleUiTheme.Px(42f);

        if (players.Count == 0)
        {
            GUI.Label(rect, "No player results available.", SimpleUiTheme.CenterStyle);
            return;
        }

        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];
            Rect rowRect = new Rect(rect.x, rect.y + i * (rowHeight + SimpleUiTheme.Px(8f)), rect.width, rowHeight);
            bool isLocal = player.OwnerClientId == localClientId;

            Color background = isLocal ? SimpleUiTheme.LocalPlayerRow : new Color(0.12f, 0.15f, 0.20f, 0.94f);
            Color border = i == 0 ? SimpleUiTheme.Accent : SimpleUiTheme.PanelBorder;
            SimpleUiTheme.DrawPanel(rowRect, background, border, SimpleUiTheme.Px(1.5f));

            string placement = (i + 1).ToString() + ".";
            GUI.Label(new Rect(rowRect.x + SimpleUiTheme.Px(10f), rowRect.y, SimpleUiTheme.Px(40f), rowRect.height), placement, SimpleUiTheme.ScoreStyle);
            GUI.Label(new Rect(rowRect.x + SimpleUiTheme.Px(54f), rowRect.y, rowRect.width * 0.55f, rowRect.height), "Player " + (player.OwnerClientId + 1), SimpleUiTheme.ScoreStyle);

            GUIStyle scoreStyle = new GUIStyle(SimpleUiTheme.ScoreStyle);
            scoreStyle.alignment = TextAnchor.MiddleRight;
            scoreStyle.normal.textColor = i == 0 ? SimpleUiTheme.Accent : SimpleUiTheme.Text;
            GUI.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - SimpleUiTheme.Px(12f), rowRect.height), player.score.Value.ToString(), scoreStyle);
        }
    }

    private static List<PlayerNetwork> GetPlayers()
    {
        PlayerNetwork[] playerArray = Object.FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        List<PlayerNetwork> players = new List<PlayerNetwork>(playerArray);
        players.Sort((a, b) =>
        {
            int scoreCompare = b.score.Value.CompareTo(a.score.Value);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            return a.OwnerClientId.CompareTo(b.OwnerClientId);
        });
        return players;
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
