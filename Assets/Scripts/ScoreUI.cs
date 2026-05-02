using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ScoreUI : MonoBehaviour
{
    private readonly List<PlayerNetwork> _players = new List<PlayerNetwork>();

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer))
        {
            return;
        }

        DrawScorePanel();
        DrawTimerPanel();
        DrawStatusPanel();
        DrawLaserBanner();
    }

    private void DrawScorePanel()
    {
        Rect panel = new Rect(20f, 20f, 280f, 220f);
        SimpleUiTheme.DrawPanel(panel, new Color(0.07f, 0.1f, 0.16f, 0.85f));
        GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 32f), "SCORES", SimpleUiTheme.Title);

        CollectPlayers();

        float y = panel.y + 48f;
        for (int i = 0; i < _players.Count; i++)
        {
            PlayerNetwork pn = _players[i];
            if (pn == null)
            {
                continue;
            }

            string label = PlayerMovement.GetPlayerLabel(pn.OwnerClientId);
            string line = label + ": " + pn.score.Value;
            GUI.Label(new Rect(panel.x + 16f, y, panel.width - 32f, 28f), line, SimpleUiTheme.Body);
            y += 28f;
        }

        GUIStyle foot = new GUIStyle(SimpleUiTheme.Small);
        foot.alignment = TextAnchor.LowerLeft;

        string kingCoinText = GameManager.kingCoinActive ? "King Coin is active" : "King Coin respawning";
        GUI.Label(new Rect(panel.x + 16f, panel.y + panel.height - 56f, panel.width - 32f, 20f), kingCoinText, foot);
        GUI.Label(new Rect(panel.x + 16f, panel.y + panel.height - 32f, panel.width - 32f, 20f), "Third-person HUD overlay enabled", foot);
    }

    private void DrawTimerPanel()
    {
        int minutes = Mathf.FloorToInt(GameManager.timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(GameManager.timeRemaining % 60f);

        Rect panel = new Rect((Screen.width - 220f) * 0.5f, 18f, 220f, 62f);
        SimpleUiTheme.DrawPanel(panel, new Color(0.08f, 0.09f, 0.12f, 0.85f));

        GUIStyle timerStyle = new GUIStyle(SimpleUiTheme.Title);
        timerStyle.fontSize = 30;
        timerStyle.normal.textColor = new Color(1f, 0.9f, 0.32f, 1f);
        GUI.Label(panel, minutes.ToString("00") + ":" + seconds.ToString("00"), timerStyle);
    }

    private void DrawStatusPanel()
    {
        NetworkObject localPlayerObject = NetworkManager.Singleton.LocalClient != null ? NetworkManager.Singleton.LocalClient.PlayerObject : null;
        if (localPlayerObject == null)
        {
            return;
        }

        PlayerMovement movement = localPlayerObject.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            return;
        }

        Rect panel = new Rect(Screen.width - 300f, 20f, 280f, 190f);
        SimpleUiTheme.DrawPanel(panel, new Color(0.07f, 0.1f, 0.16f, 0.85f));
        GUI.Label(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 28f), "STATUS", SimpleUiTheme.Title);

        float y = panel.y + 46f;
        DrawStatusRow(y, "Speed", movement.GetSpeedBoostRemaining(), 5f, new Color(0.3f, 1f, 0.65f));
        y += 42f;
        DrawStatusRow(y, "Shield", movement.GetShieldRemaining(), 5f, new Color(0.35f, 0.8f, 1f));
        y += 42f;
        DrawStatusRow(y, "Slow", movement.GetSlowRemaining(), 2f, new Color(1f, 0.65f, 0.25f));
        y += 42f;
        DrawDashRow(y, movement);
    }

    private void DrawStatusRow(float y, string label, float current, float max, Color color)
    {
        float normalized = max <= 0.0001f ? 0f : current / max;

        Rect labelRect = new Rect(Screen.width - 286f, y - 2f, 84f, 24f);
        GUI.Label(labelRect, label, SimpleUiTheme.Body);

        Rect barRect = new Rect(Screen.width - 200f, y, 166f, 22f);
        string timeText = current > 0.01f ? current.ToString("0.0") + "s" : "--";
        SimpleUiTheme.DrawBar(barRect, normalized, color, timeText);
    }

    private void DrawDashRow(float y, PlayerMovement movement)
    {
        float remaining = movement.GetDashCooldownRemainingLocal();
        float duration = Mathf.Max(0.01f, movement.GetDashCooldownDuration());
        float normalized = 1f - Mathf.Clamp01(remaining / duration);

        Rect labelRect = new Rect(Screen.width - 286f, y - 2f, 84f, 24f);
        GUI.Label(labelRect, "Dash", SimpleUiTheme.Body);

        Rect barRect = new Rect(Screen.width - 200f, y, 166f, 22f);
        string text = remaining <= 0.01f ? "READY" : remaining.ToString("0.0") + "s";
        SimpleUiTheme.DrawBar(barRect, normalized, new Color(0.85f, 0.85f, 0.95f), text);
    }

    private void DrawLaserBanner()
    {
        if (!LaserGridManager.laserWarningActive && !LaserGridManager.laserFiringActive)
        {
            return;
        }

        bool firing = LaserGridManager.laserFiringActive;
        Rect rect = new Rect((Screen.width - 620f) * 0.5f, 96f, 620f, 72f);
        SimpleUiTheme.DrawPanel(rect, firing ? new Color(0.35f, 0f, 0f, 0.88f) : new Color(0.35f, 0.2f, 0f, 0.88f));

        GUIStyle style = firing ? SimpleUiTheme.Danger : SimpleUiTheme.Warning;
        string text = firing ? "LASER GRID FIRING" : "LASER WARNING - MOVE INTO A SAFE SQUARE";
        GUI.Label(rect, text, style);
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
            return a.OwnerClientId.CompareTo(b.OwnerClientId);
        });
    }
}
