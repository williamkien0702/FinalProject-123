using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LanConnectUI : MonoBehaviour
{
    public string hostIP = "127.0.0.1";
    public ushort port = 7777;

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            DrawConnectScreen();
        }
        else
        {
            DrawInGameButtons();
        }
    }

    private void DrawConnectScreen()
    {
        Rect panel = new Rect((Screen.width - 520f) * 0.5f, (Screen.height - 330f) * 0.5f, 520f, 330f);
        SimpleUiTheme.DrawPanel(panel, new Color(0.04f, 0.07f, 0.12f, 0.92f));

        GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 40f), "FINE MARBLE", SimpleUiTheme.Title);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 62f, panel.width - 40f, 24f), "LAN MULTIPLAYER ARENA", SimpleUiTheme.Body);

        GUI.Label(new Rect(panel.x + 40f, panel.y + 105f, panel.width - 80f, 22f), "Host IP", SimpleUiTheme.Body);
        hostIP = GUI.TextField(new Rect(panel.x + 40f, panel.y + 132f, panel.width - 80f, 34f), hostIP, SimpleUiTheme.TextField);

        if (GUI.Button(new Rect(panel.x + 40f, panel.y + 188f, 180f, 44f), "Start Host", SimpleUiTheme.Button))
        {
            FineMarbleSfx.Instance?.PlayUiClick();

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
            }
            NetworkManager.Singleton.StartHost();
        }

        if (GUI.Button(new Rect(panel.x + panel.width - 220f, panel.y + 188f, 180f, 44f), "Start Client", SimpleUiTheme.Button))
        {
            FineMarbleSfx.Instance?.PlayUiClick();

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(hostIP, port);
            }
            NetworkManager.Singleton.StartClient();
        }

        if (GUI.Button(new Rect(panel.x + (panel.width - 140f) * 0.5f, panel.y + 246f, 140f, 38f), "Exit Game", SimpleUiTheme.Button))
        {
            FineMarbleSfx.Instance?.PlayUiClick();
            QuitGame();
        }

        GUI.Label(
            new Rect(panel.x + 30f, panel.y + 286f, panel.width - 60f, 34f),
            "Collect coins, dodge bombs, lasers, walls, and black holes. Highest score wins when the timer ends.",
            SimpleUiTheme.Small);
    }

    private void DrawInGameButtons()
    {
        if (GUI.Button(new Rect(Screen.width - 150f, 220f, 120f, 38f), "Exit Game", SimpleUiTheme.Button))
        {
            FineMarbleSfx.Instance?.PlayUiClick();
            QuitGame();
        }
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