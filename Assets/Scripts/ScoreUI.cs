using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public enum HudNoticeType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Danger = 3,
        Objective = 4
    }

    private sealed class HudNotice
    {
        public string Title;
        public string Subtitle;
        public HudNoticeType Type;
        public float ExpiresAt;
        public int Order;
    }

    private static readonly List<HudNotice> Notices = new List<HudNotice>();
    private static int nextNoticeOrder;

    private bool lastKingCoinActive;
    private bool lastLaserWarning;
    private bool lastLaserFiring;
    private bool lastGameOver;

    public static void QueueLocalNotification(string title, string subtitle, HudNoticeType type, float duration = 1.8f)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        float expiresAt = Time.realtimeSinceStartup + Mathf.Max(0.5f, duration);

        for (int i = 0; i < Notices.Count; i++)
        {
            HudNotice existing = Notices[i];
            if (existing.Title == title && existing.Subtitle == subtitle && existing.Type == type)
            {
                existing.ExpiresAt = expiresAt;
                existing.Order = ++nextNoticeOrder;
                return;
            }
        }

        Notices.Add(new HudNotice
        {
            Title = title,
            Subtitle = subtitle,
            Type = type,
            ExpiresAt = expiresAt,
            Order = ++nextNoticeOrder
        });

        if (Notices.Count > 12)
        {
            Notices.Sort((a, b) => a.Order.CompareTo(b.Order));
            Notices.RemoveAt(0);
        }
    }

    public static void ClearLocalNotifications()
    {
        Notices.Clear();
    }

    private void Update()
    {
        if (GameManager.kingCoinActive && !lastKingCoinActive)
        {
            QueueLocalNotification("KING COIN ACTIVE", "+10 points. Follow the arrow.", HudNoticeType.Objective, 2.2f);
        }

        if (LaserGridManager.laserWarningActive && !lastLaserWarning)
        {
            QueueLocalNotification("LASER WARNING", "Stand inside a square before it fires.", HudNoticeType.Danger, 2.1f);
        }

        if (LaserGridManager.laserFiringActive && !lastLaserFiring)
        {
            QueueLocalNotification("LASER FIRING", "Stay off the red lines.", HudNoticeType.Danger, 1.1f);
        }

        if (GameManager.gameOver && !lastGameOver)
        {
            QueueLocalNotification("MATCH OVER", GameManager.winnerText, HudNoticeType.Objective, 3f);
        }

        if (!GameManager.gameOver && lastGameOver)
        {
            ClearLocalNotifications();
        }

        lastKingCoinActive = GameManager.kingCoinActive;
        lastLaserWarning = LaserGridManager.laserWarningActive;
        lastLaserFiring = LaserGridManager.laserFiringActive;
        lastGameOver = GameManager.gameOver;
    }

    private void OnGUI()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            return;
        }

        SimpleUiTheme.Ensure();
        RemoveExpiredNotifications();

        DrawTimer();
        DrawScorePanel();
        DrawStatusPanel();
        DrawCenterAlerts();
    }

    private void DrawTimer()
    {
        float width = SimpleUiTheme.Px(320f);
        float height = SimpleUiTheme.Px(72f);
        Rect panelRect = new Rect((Screen.width - width) * 0.5f, SimpleUiTheme.Px(20f), width, height);

        SimpleUiTheme.DrawPanel(panelRect, SimpleUiTheme.PanelDarkColor, SimpleUiTheme.PanelBorder);
        SimpleUiTheme.DrawShadowLabel(panelRect, "TIME  " + SimpleUiTheme.FormatTime(GameManager.timeRemaining), SimpleUiTheme.TimerStyle, SimpleUiTheme.Accent);
    }

    private void DrawScorePanel()
    {
        List<PlayerNetwork> players = GetPlayers();
        int rowCount = Mathf.Max(2, players.Count);

        float width = SimpleUiTheme.Px(340f);
        float height = SimpleUiTheme.Px(128f) + rowCount * SimpleUiTheme.Px(42f);
        Rect panelRect = new Rect(SimpleUiTheme.Px(20f), SimpleUiTheme.Px(20f), width, height);
        SimpleUiTheme.DrawPanel(panelRect, SimpleUiTheme.PanelColor, SimpleUiTheme.PanelBorder);

        Rect contentRect = SimpleUiTheme.Pad(panelRect, SimpleUiTheme.Px(14f));
        GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, SimpleUiTheme.Px(28f)), "SCORES", SimpleUiTheme.HeadingStyle);
        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(30f), contentRect.width, SimpleUiTheme.Px(22f)), "Highest score wins when the timer ends.", SimpleUiTheme.MutedStyle);

        float rowY = contentRect.y + SimpleUiTheme.Px(62f);
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        if (players.Count == 0)
        {
            GUI.Label(new Rect(contentRect.x, rowY, contentRect.width, SimpleUiTheme.Px(28f)), "Waiting for players...", SimpleUiTheme.BodyStyle);
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];
            Rect rowRect = new Rect(contentRect.x, rowY + i * SimpleUiTheme.Px(42f), contentRect.width, SimpleUiTheme.Px(34f));
            bool isLocal = player.OwnerClientId == localClientId;
            Color rowColor = isLocal ? SimpleUiTheme.LocalPlayerRow : new Color(0.12f, 0.15f, 0.20f, 0.92f);
            SimpleUiTheme.DrawPanel(rowRect, rowColor, SimpleUiTheme.PanelBorder, SimpleUiTheme.Px(1.25f));

            string prefix = i == 0 ? "#1  " : string.Empty;
            string playerLabel = prefix + "Player " + (player.OwnerClientId + 1);
            GUI.Label(new Rect(rowRect.x + SimpleUiTheme.Px(10f), rowRect.y, rowRect.width * 0.65f, rowRect.height), playerLabel, SimpleUiTheme.ScoreStyle);

            GUIStyle scoreStyle = new GUIStyle(SimpleUiTheme.ScoreStyle);
            scoreStyle.alignment = TextAnchor.MiddleRight;
            scoreStyle.normal.textColor = i == 0 ? SimpleUiTheme.Accent : SimpleUiTheme.Text;
            GUI.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - SimpleUiTheme.Px(10f), rowRect.height), player.score.Value.ToString(), scoreStyle);
        }
    }

    private void DrawStatusPanel()
    {
        PlayerMovement localPlayer = GetLocalPlayerMovement();

        float width = SimpleUiTheme.Px(340f);
        float height = SimpleUiTheme.Px(250f);
        Rect panelRect = new Rect(Screen.width - width - SimpleUiTheme.Px(20f), SimpleUiTheme.Px(220f), width, height);
        SimpleUiTheme.DrawPanel(panelRect, SimpleUiTheme.PanelColor, SimpleUiTheme.PanelBorder);

        Rect contentRect = SimpleUiTheme.Pad(panelRect, SimpleUiTheme.Px(14f));
        GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, SimpleUiTheme.Px(28f)), "STATUS", SimpleUiTheme.HeadingStyle);

        string kingCoinText = GameManager.kingCoinActive ? "LIVE  (+10)" : "RESPAWNING";
        Color kingCoinColor = GameManager.kingCoinActive ? SimpleUiTheme.Accent : SimpleUiTheme.MutedText;
        GUIStyle kingStyle = new GUIStyle(SimpleUiTheme.SubHeadingStyle);
        kingStyle.normal.textColor = kingCoinColor;
        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(34f), contentRect.width, SimpleUiTheme.Px(22f)), "King Coin: " + kingCoinText, kingStyle);

        float firstBarY = contentRect.y + SimpleUiTheme.Px(72f);
        DrawEffectBar(new Rect(contentRect.x, firstBarY, contentRect.width, SimpleUiTheme.Px(26f)), "Speed Up", localPlayer != null ? localPlayer.GetSpeedBoostNormalized() : 0f, SimpleUiTheme.Info, localPlayer != null && localPlayer.IsSpeedBoosted() ? localPlayer.GetSpeedBoostRemaining().ToString("0.0") + "s" : "inactive");
        DrawEffectBar(new Rect(contentRect.x, firstBarY + SimpleUiTheme.Px(42f), contentRect.width, SimpleUiTheme.Px(26f)), "Shield", localPlayer != null ? localPlayer.GetShieldNormalized() : 0f, SimpleUiTheme.Success, localPlayer != null && localPlayer.HasShield() ? localPlayer.GetShieldRemaining().ToString("0.0") + "s" : "inactive");
        DrawEffectBar(new Rect(contentRect.x, firstBarY + SimpleUiTheme.Px(84f), contentRect.width, SimpleUiTheme.Px(26f)), "Slow", localPlayer != null ? localPlayer.GetSlowNormalized() : 0f, SimpleUiTheme.Warning, localPlayer != null && localPlayer.IsSlowed() ? localPlayer.GetSlowRemaining().ToString("0.0") + "s" : "inactive");

        GUI.Label(new Rect(contentRect.x, firstBarY + SimpleUiTheme.Px(130f), contentRect.width, SimpleUiTheme.Px(22f)), "Bombs and lasers remove 5 points.", SimpleUiTheme.MutedStyle);
        GUI.Label(new Rect(contentRect.x, firstBarY + SimpleUiTheme.Px(154f), contentRect.width, SimpleUiTheme.Px(22f)), "Shield blocks bomb damage only.", SimpleUiTheme.MutedStyle);
    }

    private void DrawEffectBar(Rect rect, string label, float fill, Color color, string valueText)
    {
        SimpleUiTheme.DrawProgressBar(rect, fill, color, label, valueText);
    }

    private void DrawCenterAlerts()
    {
        float scale = SimpleUiTheme.Scale;

        if (LaserGridManager.laserWarningActive)
        {
            DrawBanner(new Rect((Screen.width - SimpleUiTheme.Px(900f)) * 0.5f, SimpleUiTheme.Px(118f), SimpleUiTheme.Px(900f), SimpleUiTheme.Px(118f)), "LASER GRID WARNING", "Stand inside a square before it fires.", new Color(0.50f, 0.07f, 0.07f, 0.94f), SimpleUiTheme.Danger);
        }
        else if (LaserGridManager.laserFiringActive)
        {
            DrawBanner(new Rect((Screen.width - SimpleUiTheme.Px(900f)) * 0.5f, SimpleUiTheme.Px(118f), SimpleUiTheme.Px(900f), SimpleUiTheme.Px(108f)), "LASER GRID FIRING", "Stay off the bright red lines.", new Color(0.42f, 0.04f, 0.04f, 0.95f), SimpleUiTheme.Danger);
        }

        List<HudNotice> activeNotices = GetActiveNotices();
        if (activeNotices.Count == 0)
        {
            return;
        }

        float width = SimpleUiTheme.Px(520f);
        float height = SimpleUiTheme.Px(74f);
        float top = LaserGridManager.laserWarningActive || LaserGridManager.laserFiringActive ? SimpleUiTheme.Px(250f) : SimpleUiTheme.Px(140f);
        int shownCount = Mathf.Min(3, activeNotices.Count);

        for (int i = 0; i < shownCount; i++)
        {
            HudNotice notice = activeNotices[i];
            Rect rect = new Rect((Screen.width - width) * 0.5f, top + i * (height + SimpleUiTheme.Px(10f)), width, height);
            Color background = GetNoticeBackground(notice.Type);
            Color accent = GetNoticeAccent(notice.Type);
            SimpleUiTheme.DrawPanel(rect, background, accent);

            Rect contentRect = SimpleUiTheme.Pad(rect, SimpleUiTheme.Px(10f));
            GUIStyle titleStyle = new GUIStyle(SimpleUiTheme.CenterStyle);
            titleStyle.normal.textColor = accent;
            GUI.Label(new Rect(contentRect.x, contentRect.y - SimpleUiTheme.Px(2f), contentRect.width, SimpleUiTheme.Px(28f)), notice.Title, titleStyle);
            GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(26f), contentRect.width, SimpleUiTheme.Px(22f)), notice.Subtitle, SimpleUiTheme.SmallCenterStyle);
        }
    }

    private void DrawBanner(Rect rect, string title, string subtitle, Color background, Color border)
    {
        SimpleUiTheme.DrawPanel(rect, background, border, SimpleUiTheme.Px(2f));
        Rect contentRect = SimpleUiTheme.Pad(rect, SimpleUiTheme.Px(8f));
        SimpleUiTheme.DrawShadowLabel(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(4f), contentRect.width, SimpleUiTheme.Px(46f)), title, SimpleUiTheme.AlertTitleStyle, border);
        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(52f), contentRect.width, SimpleUiTheme.Px(28f)), subtitle, SimpleUiTheme.AlertBodyStyle);
    }

    private static void RemoveExpiredNotifications()
    {
        float now = Time.realtimeSinceStartup;
        for (int i = Notices.Count - 1; i >= 0; i--)
        {
            if (Notices[i].ExpiresAt <= now)
            {
                Notices.RemoveAt(i);
            }
        }
    }

    private static List<HudNotice> GetActiveNotices()
    {
        List<HudNotice> results = new List<HudNotice>(Notices);
        results.Sort((a, b) => b.Order.CompareTo(a.Order));
        return results;
    }

    private static Color GetNoticeBackground(HudNoticeType type)
    {
        switch (type)
        {
            case HudNoticeType.Success:
                return new Color(0.08f, 0.24f, 0.14f, 0.93f);
            case HudNoticeType.Warning:
                return new Color(0.27f, 0.18f, 0.05f, 0.93f);
            case HudNoticeType.Danger:
                return new Color(0.30f, 0.07f, 0.07f, 0.94f);
            case HudNoticeType.Objective:
                return new Color(0.26f, 0.21f, 0.05f, 0.94f);
            default:
                return new Color(0.06f, 0.16f, 0.25f, 0.93f);
        }
    }

    private static Color GetNoticeAccent(HudNoticeType type)
    {
        switch (type)
        {
            case HudNoticeType.Success:
                return SimpleUiTheme.Success;
            case HudNoticeType.Warning:
                return SimpleUiTheme.Warning;
            case HudNoticeType.Danger:
                return SimpleUiTheme.Danger;
            case HudNoticeType.Objective:
                return SimpleUiTheme.Accent;
            default:
                return SimpleUiTheme.Info;
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

    private static PlayerMovement GetLocalPlayerMovement()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.LocalClient == null || networkManager.LocalClient.PlayerObject == null)
        {
            return null;
        }

        return networkManager.LocalClient.PlayerObject.GetComponent<PlayerMovement>();
    }
}
