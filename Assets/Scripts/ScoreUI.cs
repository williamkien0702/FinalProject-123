using UnityEngine;
using Unity.Netcode;

public class ScoreUI : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 50;  
        labelStyle.normal.textColor = Color.white;

        GUILayout.BeginArea(new Rect(20, 20, 400, 300));

        GUILayout.Label("Scores:", labelStyle);

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn == null) continue;

            // +1 so players show as 1,2 instead of 0,1
            GUILayout.Label($"Player {client.ClientId + 1}: {pn.score.Value}", labelStyle);
        }

        GUILayout.EndArea();
    }
}