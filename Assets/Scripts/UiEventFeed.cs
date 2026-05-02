using System.Collections.Generic;
using UnityEngine;

public class UiEventFeed : MonoBehaviour
{
    private struct UiEvent
    {
        public string text;
        public Color color;
        public float expiresAt;
        public float lifetime;
    }

    private static UiEventFeed _instance;
    private readonly List<UiEvent> _events = new List<UiEvent>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        GameObject obj = new GameObject("UiEventFeed");
        DontDestroyOnLoad(obj);
        _instance = obj.AddComponent<UiEventFeed>();
    }

    public static void Push(string text, Color color, float duration = 1.8f)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        EnsureInstance();
        _instance._events.Add(new UiEvent
        {
            text = text,
            color = color,
            lifetime = Mathf.Max(0.25f, duration),
            expiresAt = Time.unscaledTime + Mathf.Max(0.25f, duration)
        });
    }

    public static void ClearAll()
    {
        if (_instance != null)
        {
            _instance._events.Clear();
        }
    }

    private void Update()
    {
        for (int i = _events.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime >= _events[i].expiresAt)
            {
                _events.RemoveAt(i);
            }
        }
    }

    private void OnGUI()
    {
        if (_events.Count == 0 || GameManager.gameOver)
        {
            return;
        }

        float width = Mathf.Min(560f, Screen.width - 60f);
        float height = 56f;
        float startY = Screen.height * 0.22f;

        for (int i = 0; i < _events.Count; i++)
        {
            UiEvent item = _events[i];
            float timeLeft = Mathf.Max(0f, item.expiresAt - Time.unscaledTime);
            float alpha = Mathf.Clamp01(timeLeft / item.lifetime);
            Rect rect = new Rect((Screen.width - width) * 0.5f, startY + (i * (height + 10f)), width, height);

            SimpleUiTheme.DrawPanel(rect, new Color(0f, 0f, 0f, 0.58f * alpha));

            GUIStyle style = new GUIStyle(SimpleUiTheme.Title);
            style.fontSize = 24;
            style.normal.textColor = new Color(item.color.r, item.color.g, item.color.b, Mathf.Lerp(0.3f, 1f, alpha));
            GUI.Label(rect, item.text, style);
        }
    }
}
