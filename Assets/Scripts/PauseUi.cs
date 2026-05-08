using UnityEngine;
using Unity.Netcode;

public class PauseMenuUI : MonoBehaviour
{
    private GameManager gameManager;

    void Awake()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        if (GameManager.gameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsListening) return;
        if (gameManager == null) return;

        bool newPauseState = !GameManager.gamePaused;
        gameManager.SetPauseServerRpc(newPauseState);
    }

    void OnGUI()
    {
        if (!GameManager.gamePaused) return;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 50;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        float boxWidth = 420f;
        float boxHeight = 280f;
        float x = (Screen.width - boxWidth) / 2f;
        float y = (Screen.height - boxHeight) / 2f;

        GUI.Box(new Rect(x, y, boxWidth, boxHeight), "");

        GUI.Label(new Rect(x, y + 25, boxWidth, 70), "Paused", titleStyle);

        if (GUI.Button(new Rect(x + 90, y + 115, 240, 55), "Resume", buttonStyle))
        {
            if (gameManager != null)
            {
                gameManager.SetPauseServerRpc(false);
            }
        }

        if (GUI.Button(new Rect(x + 90, y + 185, 240, 55), "Exit Game", buttonStyle))
        {
            QuitGame();
        }
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
        GameManager.gamePaused = false;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void LockCursor()
    {
        if (GameManager.gameOver) return;
        if (GameManager.gamePaused) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}