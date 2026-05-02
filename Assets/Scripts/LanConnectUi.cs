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

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 24;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;

        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = 22;
        textFieldStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;

        float boxWidth = 420f;
        float boxHeight = 220f;
        float boxX = (Screen.width - boxWidth) / 2f;
        float boxY = (Screen.height - boxHeight) / 2f;

        // before host/client starts
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");

            GUI.Label(new Rect(boxX + 20, boxY + 20, boxWidth - 40, 35), "Enter Host IP", labelStyle);

            hostIP = GUI.TextField(
                new Rect(boxX + 70, boxY + 65, 280, 35),
                hostIP,
                textFieldStyle
            );

            if (GUI.Button(new Rect(boxX + 60, boxY + 120, 130, 45), "Start Host", buttonStyle))
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
                NetworkManager.Singleton.StartHost();
            }

            if (GUI.Button(new Rect(boxX + 230, boxY + 120, 130, 45), "Start Client", buttonStyle))
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetConnectionData(hostIP, port);
                NetworkManager.Singleton.StartClient();
            }

            if (GUI.Button(new Rect(boxX + 145, boxY + 175, 130, 35), "Exit Game", buttonStyle))
            {
                QuitGame();
            }
        }
        else
        {
            // during gameplay
            if (GUI.Button(new Rect(Screen.width - 150, 20, 120, 40), "Exit Game", buttonStyle))
            {
                QuitGame();
            }
        }
    }

    void QuitGame()
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