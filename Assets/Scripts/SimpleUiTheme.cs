using UnityEngine;

public static class SimpleUiTheme
{
    private static bool initialized;
    private static int cachedWidth;
    private static int cachedHeight;

    public static float Scale { get; private set; }

    public static readonly Color PanelColor = new Color(0.08f, 0.10f, 0.14f, 0.90f);
    public static readonly Color PanelDarkColor = new Color(0.05f, 0.06f, 0.09f, 0.94f);
    public static readonly Color PanelBorder = new Color(0.36f, 0.41f, 0.50f, 1f);
    public static readonly Color Text = new Color(0.96f, 0.97f, 1f, 1f);
    public static readonly Color MutedText = new Color(0.76f, 0.80f, 0.86f, 1f);
    public static readonly Color Accent = new Color(1.00f, 0.85f, 0.22f, 1f);
    public static readonly Color Success = new Color(0.27f, 0.86f, 0.55f, 1f);
    public static readonly Color Warning = new Color(1.00f, 0.71f, 0.22f, 1f);
    public static readonly Color Danger = new Color(1.00f, 0.34f, 0.34f, 1f);
    public static readonly Color Info = new Color(0.36f, 0.78f, 1.00f, 1f);
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.55f);
    public static readonly Color LocalPlayerRow = new Color(0.20f, 0.28f, 0.38f, 0.95f);
    public static readonly Color NeutralBar = new Color(0.20f, 0.24f, 0.30f, 1f);

    public static GUIStyle TitleStyle { get; private set; }
    public static GUIStyle HeadingStyle { get; private set; }
    public static GUIStyle SubHeadingStyle { get; private set; }
    public static GUIStyle BodyStyle { get; private set; }
    public static GUIStyle MutedStyle { get; private set; }
    public static GUIStyle CenterStyle { get; private set; }
    public static GUIStyle SmallCenterStyle { get; private set; }
    public static GUIStyle TimerStyle { get; private set; }
    public static GUIStyle ButtonStyle { get; private set; }
    public static GUIStyle TextFieldStyle { get; private set; }
    public static GUIStyle AlertTitleStyle { get; private set; }
    public static GUIStyle AlertBodyStyle { get; private set; }
    public static GUIStyle ArrowStyle { get; private set; }
    public static GUIStyle ScoreStyle { get; private set; }
    public static GUIStyle TinyStyle { get; private set; }

    public static void Ensure()
    {
        if (initialized && cachedWidth == Screen.width && cachedHeight == Screen.height)
        {
            return;
        }

        initialized = true;
        cachedWidth = Screen.width;
        cachedHeight = Screen.height;

        Scale = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), 0.78f, 1.28f);

        TitleStyle = CreateLabelStyle(34, FontStyle.Bold, TextAnchor.MiddleCenter, Text, false);
        HeadingStyle = CreateLabelStyle(24, FontStyle.Bold, TextAnchor.UpperLeft, Text, false);
        SubHeadingStyle = CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, MutedText, false);
        BodyStyle = CreateLabelStyle(18, FontStyle.Normal, TextAnchor.UpperLeft, Text, true);
        MutedStyle = CreateLabelStyle(15, FontStyle.Normal, TextAnchor.UpperLeft, MutedText, true);
        CenterStyle = CreateLabelStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, Text, true);
        SmallCenterStyle = CreateLabelStyle(15, FontStyle.Normal, TextAnchor.MiddleCenter, MutedText, true);
        TimerStyle = CreateLabelStyle(30, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, false);
        AlertTitleStyle = CreateLabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, Text, true);
        AlertBodyStyle = CreateLabelStyle(18, FontStyle.Normal, TextAnchor.MiddleCenter, Text, true);
        ArrowStyle = CreateLabelStyle(88, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, false);
        ScoreStyle = CreateLabelStyle(19, FontStyle.Bold, TextAnchor.MiddleLeft, Text, false);
        TinyStyle = CreateLabelStyle(13, FontStyle.Normal, TextAnchor.MiddleLeft, MutedText, false);

        ButtonStyle = new GUIStyle(GUI.skin.button);
        ButtonStyle.fontSize = PxInt(18);
        ButtonStyle.fontStyle = FontStyle.Bold;
        ButtonStyle.alignment = TextAnchor.MiddleCenter;
        ButtonStyle.normal.textColor = Text;
        ButtonStyle.hover.textColor = Text;
        ButtonStyle.active.textColor = Text;
        ButtonStyle.padding = new RectOffset(PxInt(12), PxInt(12), PxInt(10), PxInt(10));
        ButtonStyle.margin = new RectOffset(0, 0, PxInt(4), PxInt(4));

        TextFieldStyle = new GUIStyle(GUI.skin.textField);
        TextFieldStyle.fontSize = PxInt(18);
        TextFieldStyle.alignment = TextAnchor.MiddleLeft;
        TextFieldStyle.padding = new RectOffset(PxInt(10), PxInt(10), PxInt(8), PxInt(8));
        TextFieldStyle.normal.textColor = Text;
        TextFieldStyle.focused.textColor = Text;
    }

    public static int PxInt(float value)
    {
        return Mathf.RoundToInt(value * Scale);
    }

    public static float Px(float value)
    {
        return value * Scale;
    }

    public static Rect Pad(Rect rect, float padding)
    {
        return new Rect(rect.x + padding, rect.y + padding, rect.width - (padding * 2f), rect.height - (padding * 2f));
    }

    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
    }

    public static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void DrawPanel(Rect rect, Color background, Color border, float borderSize = 2f)
    {
        DrawRect(rect, background);
        DrawRect(new Rect(rect.x, rect.y, rect.width, borderSize), border);
        DrawRect(new Rect(rect.x, rect.yMax - borderSize, rect.width, borderSize), border);
        DrawRect(new Rect(rect.x, rect.y, borderSize, rect.height), border);
        DrawRect(new Rect(rect.xMax - borderSize, rect.y, borderSize, rect.height), border);
    }

    public static void DrawShadowLabel(Rect rect, string text, GUIStyle baseStyle, Color color)
    {
        GUIStyle shadowStyle = new GUIStyle(baseStyle);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
        GUI.Label(new Rect(rect.x + Px(2f), rect.y + Px(2f), rect.width, rect.height), text, shadowStyle);

        GUIStyle mainStyle = new GUIStyle(baseStyle);
        mainStyle.normal.textColor = color;
        GUI.Label(rect, text, mainStyle);
    }

    public static void DrawProgressBar(Rect rect, float normalized, Color fillColor, string leftLabel, string rightLabel)
    {
        normalized = Mathf.Clamp01(normalized);

        DrawPanel(rect, new Color(0.07f, 0.08f, 0.11f, 0.92f), PanelBorder, Px(1.5f));

        Rect innerRect = Pad(rect, Px(2f));
        DrawRect(innerRect, NeutralBar);

        if (normalized > 0f)
        {
            Rect fillRect = new Rect(innerRect.x, innerRect.y, innerRect.width * normalized, innerRect.height);
            DrawRect(fillRect, fillColor);
        }

        GUI.Label(new Rect(rect.x + Px(8f), rect.y, rect.width - Px(16f), rect.height), leftLabel, TinyStyle);

        GUIStyle rightStyle = new GUIStyle(TinyStyle);
        rightStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(rect.x + Px(8f), rect.y, rect.width - Px(16f), rect.height), rightLabel, rightStyle);
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, bool wordWrap)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = PxInt(fontSize);
        style.fontStyle = fontStyle;
        style.alignment = alignment;
        style.wordWrap = wordWrap;
        style.richText = false;
        style.normal.textColor = color;
        return style;
    }
}
