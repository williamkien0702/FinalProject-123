using UnityEngine;
using Unity.Netcode;

public class ScoreUI : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 40;
        labelStyle.normal.textColor = Color.white;

        GUIStyle timerStyle = new GUIStyle(GUI.skin.label);
        timerStyle.fontSize = 45;
        timerStyle.alignment = TextAnchor.MiddleCenter;
        timerStyle.normal.textColor = Color.yellow;

        int minutes = Mathf.FloorToInt(GameManager.timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(GameManager.timeRemaining % 60f);

        GUI.Label(
            new Rect((Screen.width - 300) / 2, 20, 300, 60),
            $"Time: {minutes:00}:{seconds:00}",
            timerStyle
        );

        // Big centered laser warning text
        GUIStyle laserStyle = new GUIStyle(GUI.skin.label);
        laserStyle.fontSize = 70;
        laserStyle.alignment = TextAnchor.MiddleCenter;
        laserStyle.normal.textColor = Color.red;
        laserStyle.fontStyle = FontStyle.Bold;

        if (LaserGridManager.laserWarningActive)
        {
            GUI.Box(
                new Rect((Screen.width - 1050) / 2, 120, 1050, 170),
                ""
            );

            GUI.Label(
                new Rect((Screen.width - 1000) / 2, 135, 1000, 140),
                "LASER GRID WARNING!\nSTAND INSIDE A SQUARE!",
                laserStyle
            );
        }

        if (LaserGridManager.laserFiringActive)
        {
            GUI.Box(
                new Rect((Screen.width - 1050) / 2, 120, 1050, 150),
                ""
            );

            GUI.Label(
                new Rect((Screen.width - 1000) / 2, 145, 1000, 100),
                "LASER GRID FIRING!",
                laserStyle
            );
        }

        GUILayout.BeginArea(new Rect(20, 20, 450, 300));

        GUILayout.Label("Scores:", labelStyle);

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn == null) continue;

            GUILayout.Label($"Player {client.ClientId + 1}: {pn.score.Value}", labelStyle);
        }

        if (GameManager.kingCoinActive)
        {
            GUILayout.Label("King Coin is active!", labelStyle);
        }
        else
        {
            GUILayout.Label("King Coin incoming...", labelStyle);
        }

        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            PlayerMovement localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerMovement>();

            if (localPlayer != null)
            {
                if (localPlayer.IsSpeedBoosted())
                {
                    GUILayout.Label("Speed Boost Active", labelStyle);
                }

                if (localPlayer.HasShield())
                {
                    GUILayout.Label("Shield Active", labelStyle);
                }
            }
        }

        GUILayout.EndArea();
    }
}