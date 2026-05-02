using UnityEngine;

public static class SimpleUiTheme
{
    private static GUIStyle _panel;
    private static GUIStyle _title;
    private static GUIStyle _body;
    private static GUIStyle _small;
    private static GUIStyle _warning;
    private static GUIStyle _danger;
    private static GUIStyle _button;
    private static GUIStyle _textField;
    private static Texture2D _whiteTexture;

    public static Texture2D WhiteTexture
    {
        get
        {
            if (_whiteTexture == null)
            {
                _whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _whiteTexture.SetPixel(0, 0, Color.white);
                _whiteTexture.Apply();
            }
            return _whiteTexture;
        }
    }

    public static GUIStyle Panel
    {
        get
        {
            if (_panel == null)
            {
                _panel = new GUIStyle(GUI.skin.box);
                _panel.normal.background = WhiteTexture;
                _panel.normal.textColor = Color.white;
                _panel.border = new RectOffset(8, 8, 8, 8);
                _panel.padding = new RectOffset(14, 14, 12, 12);
            }
            return _panel;
        }
    }

    public static GUIStyle Title
    {
        get
        {
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label);
                _title.fontSize = 28;
                _title.fontStyle = FontStyle.Bold;
                _title.normal.textColor = Color.white;
                _title.alignment = TextAnchor.MiddleCenter;
            }
            return _title;
        }
    }

    public static GUIStyle Body
    {
        get
        {
            if (_body == null)
            {
                _body = new GUIStyle(GUI.skin.label);
                _body.fontSize = 20;
                _body.normal.textColor = Color.white;
                _body.alignment = TextAnchor.MiddleLeft;
            }
            return _body;
        }
    }

    public static GUIStyle Small
    {
        get
        {
            if (_small == null)
            {
                _small = new GUIStyle(GUI.skin.label);
                _small.fontSize = 16;
                _small.wordWrap = true;
                _small.normal.textColor = new Color(0.88f, 0.92f, 0.98f, 1f);
            }
            return _small;
        }
    }

    public static GUIStyle Warning
    {
        get
        {
            if (_warning == null)
            {
                _warning = new GUIStyle(Title);
                _warning.fontSize = 40;
                _warning.normal.textColor = new Color(1f, 0.85f, 0.2f, 1f);
            }
            return _warning;
        }
    }

    public static GUIStyle Danger
    {
        get
        {
            if (_danger == null)
            {
                _danger = new GUIStyle(Title);
                _danger.fontSize = 42;
                _danger.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);
            }
            return _danger;
        }
    }

    public static GUIStyle Button
    {
        get
        {
            if (_button == null)
            {
                _button = new GUIStyle(GUI.skin.button);
                _button.fontSize = 20;
                _button.fontStyle = FontStyle.Bold;
                _button.alignment = TextAnchor.MiddleCenter;
            }
            return _button;
        }
    }

    public static GUIStyle TextField
    {
        get
        {
            if (_textField == null)
            {
                _textField = new GUIStyle(GUI.skin.textField);
                _textField.fontSize = 20;
                _textField.alignment = TextAnchor.MiddleCenter;
            }
            return _textField;
        }
    }

    public static void DrawPanel(Rect rect, Color background)
    {
        Color old = GUI.color;
        GUI.color = background;
        GUI.Box(rect, GUIContent.none, Panel);
        GUI.color = old;
    }

    public static void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color shadowColor, Vector2 offset)
    {
        Color old = style.normal.textColor;
        Color oldGui = GUI.color;

        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height), text, style);

        style.normal.textColor = old;
        GUI.color = oldGui;
        GUI.Label(rect, text, style);
    }

    public static void DrawBar(Rect rect, float normalized, Color fill, string label)
    {
        normalized = Mathf.Clamp01(normalized);
        DrawPanel(rect, new Color(0f, 0f, 0f, 0.55f));

        Rect fillRect = new Rect(rect.x + 3f, rect.y + 3f, (rect.width - 6f) * normalized, rect.height - 6f);
        Color old = GUI.color;
        GUI.color = fill;
        GUI.DrawTexture(fillRect, WhiteTexture);
        GUI.color = old;

        GUIStyle centered = new GUIStyle(Body);
        centered.alignment = TextAnchor.MiddleCenter;
        centered.fontSize = 16;
        GUI.Label(rect, label, centered);
    }
}
