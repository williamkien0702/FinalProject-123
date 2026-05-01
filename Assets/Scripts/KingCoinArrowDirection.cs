using Unity.Netcode;
using UnityEngine;

public class KingCoinArrowUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (!GameManager.kingCoinActive)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.LocalClient == null || networkManager.LocalClient.PlayerObject == null)
        {
            return;
        }

        SimpleUiTheme.Ensure();

        Transform player = networkManager.LocalClient.PlayerObject.transform;
        Vector3 direction = GameManager.kingCoinPosition - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        float width = SimpleUiTheme.Px(180f);
        float height = SimpleUiTheme.Px(180f);
        Rect panelRect = new Rect(Screen.width - width - SimpleUiTheme.Px(20f), SimpleUiTheme.Px(20f), width, height);
        SimpleUiTheme.DrawPanel(panelRect, new Color(0.22f, 0.18f, 0.04f, 0.94f), SimpleUiTheme.Accent, SimpleUiTheme.Px(2f));

        GUI.Label(new Rect(panelRect.x, panelRect.y + SimpleUiTheme.Px(6f), panelRect.width, SimpleUiTheme.Px(24f)), "KING COIN", SimpleUiTheme.SmallCenterStyle);

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Rect arrowRect = new Rect(panelRect.x + SimpleUiTheme.Px(26f), panelRect.y + SimpleUiTheme.Px(24f), panelRect.width - SimpleUiTheme.Px(52f), panelRect.height - SimpleUiTheme.Px(72f));
        GUIUtility.RotateAroundPivot(angle, arrowRect.center);
        SimpleUiTheme.DrawShadowLabel(arrowRect, "▲", SimpleUiTheme.ArrowStyle, SimpleUiTheme.Accent);
        GUIUtility.RotateAroundPivot(-angle, arrowRect.center);

        float distance = direction.magnitude;
        GUI.Label(new Rect(panelRect.x, panelRect.yMax - SimpleUiTheme.Px(34f), panelRect.width, SimpleUiTheme.Px(20f)), distance.ToString("0") + "m away", SimpleUiTheme.SmallCenterStyle);
    }
}
