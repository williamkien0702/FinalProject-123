using UnityEngine;
using Unity.Netcode;

public class KingCoinArrowUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (!GameManager.kingCoinActive || NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
        {
            return;
        }

        NetworkObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObj == null)
        {
            return;
        }

        Transform player = playerObj.transform;
        Vector3 direction = GameManager.kingCoinPosition - player.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float distance = direction.magnitude;

        Rect panel = new Rect(Screen.width - 200f, 270f, 170f, 120f);
        SimpleUiTheme.DrawPanel(panel, new Color(0.15f, 0.12f, 0.02f, 0.82f));

        GUIStyle arrowStyle = new GUIStyle(SimpleUiTheme.Title);
        arrowStyle.fontSize = 72;
        arrowStyle.normal.textColor = new Color(1f, 0.9f, 0.2f, 1f);

        Rect arrowRect = new Rect(panel.x + 36f, panel.y + 8f, 96f, 80f);
        GUIUtility.RotateAroundPivot(angle, arrowRect.center);
        GUI.Label(arrowRect, "↑", arrowStyle);
        GUIUtility.RotateAroundPivot(-angle, arrowRect.center);

        GUIStyle infoStyle = new GUIStyle(SimpleUiTheme.Small);
        infoStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(panel.x + 10f, panel.y + 78f, panel.width - 20f, 18f), "King Coin", infoStyle);
        GUI.Label(new Rect(panel.x + 10f, panel.y + 96f, panel.width - 20f, 18f), distance.ToString("0.0") + " m", infoStyle);
    }
}
