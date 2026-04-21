using UnityEngine;
using Unity.Netcode;

public class EndGameUI : MonoBehaviour
{
    private GameManager gameManager;

    void Awake()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
    }

    void OnGUI()
    {
        if (!GameManager.gameOver) return;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 40;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.yellow;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 25;

        float boxWidth = 450;
        float boxHeight = 240;
        float x = (Screen.width - boxWidth) / 2;
        float y = (Screen.height - boxHeight) / 2;

        GUI.Box(new Rect(x, y, boxWidth, boxHeight), "");

        GUI.Label(new Rect(x, y + 25, boxWidth, 60), GameManager.winnerText, labelStyle);

        if (GUI.Button(new Rect(x + 60, y + 120, 140, 50), "Play Again", buttonStyle))
        {
            if (NetworkManager.Singleton != null && gameManager != null)
            {
                gameManager.RestartGameServerRpc();
            }
        }

        if (GUI.Button(new Rect(x + 250, y + 120, 140, 50), "Exit Game", buttonStyle))
        {
            QuitGame();
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