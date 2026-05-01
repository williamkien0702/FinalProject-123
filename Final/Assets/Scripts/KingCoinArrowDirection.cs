using UnityEngine;
using Unity.Netcode;

public class KingCoinArrowUI : MonoBehaviour
{
    void OnGUI()
    {
        if (!GameManager.kingCoinActive) return;
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClient == null) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

        Transform player = NetworkManager.Singleton.LocalClient.PlayerObject.transform;

        Vector3 direction = GameManager.kingCoinPosition - player.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        GUIStyle arrowStyle = new GUIStyle(GUI.skin.label);
        arrowStyle.fontSize = 170;
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.normal.textColor = Color.yellow;
        arrowStyle.fontStyle = FontStyle.Bold;

        Rect arrowRect = new Rect(Screen.width - 250, 90, 220, 220);

        GUIUtility.RotateAroundPivot(angle, arrowRect.center);
        GUI.Label(arrowRect, "↑", arrowStyle);
        GUIUtility.RotateAroundPivot(-angle, arrowRect.center);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 22;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = Color.yellow;

        GUI.Label(new Rect(Screen.width - 270, 240, 240, 40), "King Coin", textStyle);
    }
}