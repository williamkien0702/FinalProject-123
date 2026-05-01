using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class LanConnectUI : MonoBehaviour
{
    public string hostIP = "127.0.0.1";
    public ushort port = 7777;

    private void OnGUI()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        SimpleUiTheme.Ensure();

        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            DrawConnectionMenu(networkManager);
        }
        else
        {
            DrawInMatchControls(networkManager);
        }
    }

    private void DrawConnectionMenu(NetworkManager networkManager)
    {
        float width = SimpleUiTheme.Px(760f);
        float height = SimpleUiTheme.Px(560f);
        Rect panelRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        SimpleUiTheme.DrawPanel(panelRect, SimpleUiTheme.PanelDarkColor, SimpleUiTheme.Accent, SimpleUiTheme.Px(2f));

        Rect contentRect = SimpleUiTheme.Pad(panelRect, SimpleUiTheme.Px(22f));

        GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, SimpleUiTheme.Px(40f)), "FINE MARBLE", SimpleUiTheme.TitleStyle);
        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(48f), contentRect.width, SimpleUiTheme.Px(26f)), "LAN Multiplayer Arena", SimpleUiTheme.SmallCenterStyle);
        GUI.Label(new Rect(contentRect.x, contentRect.y + SimpleUiTheme.Px(88f), contentRect.width, SimpleUiTheme.Px(44f)), "Collect coins, survive hazards, and finish with the highest score when time runs out.", SimpleUiTheme.CenterStyle);

        float formY = contentRect.y + SimpleUiTheme.Px(154f);
        GUI.Label(new Rect(contentRect.x, formY, contentRect.width, SimpleUiTheme.Px(24f)), "Host IP address", SimpleUiTheme.SubHeadingStyle);

        float fieldWidth = SimpleUiTheme.Px(360f);
        float fieldHeight = SimpleUiTheme.Px(42f);
        float fieldX = (Screen.width - fieldWidth) * 0.5f;
        hostIP = GUI.TextField(new Rect(fieldX, formY + SimpleUiTheme.Px(32f), fieldWidth, fieldHeight), hostIP, SimpleUiTheme.TextFieldStyle);

        GUI.Label(new Rect(contentRect.x, formY + SimpleUiTheme.Px(82f), contentRect.width, SimpleUiTheme.Px(22f)), "Use the host machine's local IP when joining from another computer.", SimpleUiTheme.SmallCenterStyle);

        float buttonWidth = SimpleUiTheme.Px(190f);
        float buttonHeight = SimpleUiTheme.Px(52f);
        float buttonY = formY + SimpleUiTheme.Px(126f);
        float startX = (Screen.width - (buttonWidth * 2f + SimpleUiTheme.Px(16f))) * 0.5f;

        if (GUI.Button(new Rect(startX, buttonY, buttonWidth, buttonHeight), "Start Host", SimpleUiTheme.ButtonStyle))
        {
            StartHost(networkManager);
        }

        if (GUI.Button(new Rect(startX + buttonWidth + SimpleUiTheme.Px(16f), buttonY, buttonWidth, buttonHeight), "Start Client", SimpleUiTheme.ButtonStyle))
        {
            StartClient(networkManager);
        }

        float rulesY = buttonY + SimpleUiTheme.Px(84f);
        Rect rulesRect = new Rect(contentRect.x + SimpleUiTheme.Px(42f), rulesY, contentRect.width - SimpleUiTheme.Px(84f), SimpleUiTheme.Px(170f));
        SimpleUiTheme.DrawPanel(rulesRect, new Color(0.10f, 0.13f, 0.18f, 0.92f), SimpleUiTheme.PanelBorder, SimpleUiTheme.Px(1.5f));

        Rect rulesContent = SimpleUiTheme.Pad(rulesRect, SimpleUiTheme.Px(14f));
        GUI.Label(new Rect(rulesContent.x, rulesContent.y, rulesContent.width, SimpleUiTheme.Px(24f)), "Quick rules", SimpleUiTheme.HeadingStyle);
        GUI.Label(new Rect(rulesContent.x, rulesContent.y + SimpleUiTheme.Px(30f), rulesContent.width, SimpleUiTheme.Px(22f)), "Coins: +1 point", SimpleUiTheme.BodyStyle);
        GUI.Label(new Rect(rulesContent.x, rulesContent.y + SimpleUiTheme.Px(56f), rulesContent.width, SimpleUiTheme.Px(22f)), "King Coin: +10 points", SimpleUiTheme.BodyStyle);
        GUI.Label(new Rect(rulesContent.x, rulesContent.y + SimpleUiTheme.Px(82f), rulesContent.width, SimpleUiTheme.Px(22f)), "Bombs and laser grid: -5 points", SimpleUiTheme.BodyStyle);
        GUI.Label(new Rect(rulesContent.x, rulesContent.y + SimpleUiTheme.Px(108f), rulesContent.width, SimpleUiTheme.Px(22f)), "Shield blocks bomb damage, fake coins slow you down.", SimpleUiTheme.BodyStyle);

        float exitWidth = SimpleUiTheme.Px(180f);
        if (GUI.Button(new Rect((Screen.width - exitWidth) * 0.5f, panelRect.yMax - SimpleUiTheme.Px(68f), exitWidth, SimpleUiTheme.Px(44f)), "Exit Game", SimpleUiTheme.ButtonStyle))
        {
            QuitGame();
        }
    }

    private void DrawInMatchControls(NetworkManager networkManager)
    {
        float panelWidth = SimpleUiTheme.Px(220f);
        float panelHeight = SimpleUiTheme.Px(88f);
        Rect panelRect = new Rect(Screen.width - panelWidth - SimpleUiTheme.Px(20f), Screen.height - panelHeight - SimpleUiTheme.Px(20f), panelWidth, panelHeight);
        SimpleUiTheme.DrawPanel(panelRect, SimpleUiTheme.PanelDarkColor, SimpleUiTheme.PanelBorder);

        string mode = networkManager.IsHost ? "Host" : networkManager.IsServer ? "Server" : "Client";
        GUI.Label(new Rect(panelRect.x, panelRect.y + SimpleUiTheme.Px(8f), panelRect.width, SimpleUiTheme.Px(22f)), mode + "  |  Port " + port, SimpleUiTheme.SmallCenterStyle);

        if (GUI.Button(new Rect(panelRect.x + SimpleUiTheme.Px(18f), panelRect.y + SimpleUiTheme.Px(36f), panelRect.width - SimpleUiTheme.Px(36f), SimpleUiTheme.Px(34f)), "Leave Match", SimpleUiTheme.ButtonStyle))
        {
            QuitGame();
        }
    }

    private void StartHost(NetworkManager networkManager)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        }

        networkManager.StartHost();
    }

    private void StartClient(NetworkManager networkManager)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(hostIP, port);
        }

        networkManager.StartClient();
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
