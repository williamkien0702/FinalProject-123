using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LanConnectUI : MonoBehaviour
{
    public string hostIP = "192.168.0.238";
    public ushort port = 7777;

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            DrawMainMenu();
        }
        else
        {
            DrawInGameExitButton();
        }
    }

    void DrawMainMenu()
    {
        float boxWidth = 560f;
        float boxHeight = 460f;
        float boxX = (Screen.width - boxWidth) / 2f;
        float boxY = (Screen.height - boxHeight) / 2f;

        // Background box
        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");

        // --- TITLE ---
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 58;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0f); // Gold

        GUI.Label(new Rect(boxX, boxY + 20f, boxWidth, 70f), "COIN RIOT", titleStyle);

        // --- SUBTITLE ---
        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 18;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = new Color(0.7f, 0.9f, 1f); // Light blue

        GUI.Label(new Rect(boxX, boxY + 90f, boxWidth, 30f), "LAN MULTIPLAYER ARENA", subtitleStyle);

        // --- DIVIDER ---
        GUI.Box(new Rect(boxX + 40f, boxY + 126f, boxWidth - 80f, 2f), "");

        // --- DESCRIPTION ---
        GUIStyle descStyle = new GUIStyle(GUI.skin.label);
        descStyle.fontSize = 16;
        descStyle.alignment = TextAnchor.MiddleCenter;
        descStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        descStyle.wordWrap = true;

        GUI.Label(
            new Rect(boxX + 30f, boxY + 136f, boxWidth - 60f, 80f),
            "Race to collect coins across a chaotic arena.\nDodge bombs, laser grids, and black holes.\nGrab power-ups to gain the edge.\nHighest score when the timer runs out wins!",
            descStyle
        );

        // --- IP LABEL ---
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 20;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(boxX, boxY + 228f, boxWidth, 30f), "Host IP Address", labelStyle);

        // --- IP INPUT ---
        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = 20;
        textFieldStyle.alignment = TextAnchor.MiddleCenter;

        hostIP = GUI.TextField(
            new Rect(boxX + 110f, boxY + 264f, 340f, 36f),
            hostIP,
            textFieldStyle
        );

        // --- BUTTONS ---
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;

        if (GUI.Button(new Rect(boxX + 50f, boxY + 320f, 160f, 50f), "Start Host", buttonStyle))
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            NetworkManager.Singleton.StartHost();
        }

        if (GUI.Button(new Rect(boxX + 350f, boxY + 320f, 160f, 50f), "Join Game", buttonStyle))
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(hostIP, port);
            NetworkManager.Singleton.StartClient();
        }

        GUIStyle exitStyle = new GUIStyle(GUI.skin.button);
        exitStyle.fontSize = 18;
        exitStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

        if (GUI.Button(new Rect(boxX + 190f, boxY + 390f, 180f, 38f), "Exit Game", exitStyle))
        {
            QuitGame();
        }
    }

    void DrawInGameExitButton()
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;

        if (GUI.Button(new Rect(Screen.width - 150f, 20f, 120f, 40f), "Exit Game", buttonStyle))
        {
            QuitGame();
        }
    }

    void QuitGame()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}