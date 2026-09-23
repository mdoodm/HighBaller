using System;
using UnityEngine;
using HighBaller.Helpers;

namespace HighBaller.UI;

internal class PanelRenderer
{
    private const int MaxGuiScale = 10;

    private static GUIStyle _salesPanelStyle;

    private static GUIStyle _salesHeaderStyle;

    private static int _guiScale = 1;

    private Texture2D _cachedPanelBackground;

    private readonly PanelState _contactPanel;

    private GUIStyle _infoLabelStyle;

    private GUIStyle _infoValueHighlightStyle;

    private GUIStyle _infoValueStyle;

    private readonly PanelState _salesPanel;

    private GUIStyle _sectionHeaderStyle;

    internal PanelRenderer(PanelState salesPanel, PanelState contactPanel)
    {
        _salesPanel = salesPanel;
        _contactPanel = contactPanel;
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        _salesPanelStyle = new GUIStyle("box")
        {
            normal =
            {
                background = Texture2D.whiteTexture,
                textColor = Color.white
            },
            padding = new RectOffset(10, 10, 10, 10)
        };
        _salesHeaderStyle = new GUIStyle("label")
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = Color.white
            },
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 0, 0, 0)
        };
        _sectionHeaderStyle = new GUIStyle("label")
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = new Color(0.9f, 0.9f, 1f)
            },
            padding = new RectOffset(0, 0, 10, 5)
        };
        _infoLabelStyle = new GUIStyle("label")
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = new Color(0.8f, 0.8f, 0.8f)
            },
            padding = new RectOffset(10, 0, 2, 2)
        };
        _infoValueStyle = new GUIStyle("label")
        {
            fontSize = 14,
            normal =
            {
                textColor = new Color(1f, 0.95f, 0.8f)
            },
            padding = new RectOffset(10, 0, 2, 8)
        };
        _infoValueHighlightStyle = new GUIStyle("label")
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = new Color(0.3f, 1f, 0.4f)
            },
            padding = new RectOffset(10, 0, 2, 8)
        };
    }

    private static void HandlePanelDragging(PanelState panel, Rect dragRect, Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown when dragRect.Contains(currentEvent.mousePosition):
                panel.IsDragging = true;
                panel.DragOffset = currentEvent.mousePosition - panel.Position;
                currentEvent.Use();
                break;
            case EventType.MouseUp:
                panel.IsDragging = false;
                break;
        }

        if (!panel.IsDragging || currentEvent.type != EventType.MouseDrag) return;
        panel.Position = currentEvent.mousePosition - panel.DragOffset;
        currentEvent.Use();
    }

    internal void DrawPanel(PanelState panel, float width, float height)
    {
        if (!panel.IsVisible) return;

        var dragRect = new Rect(panel.Position.x, panel.Position.y, width - 90f, 40f);
        HandlePanelDragging(panel, dragRect, Event.current);
        panel.Position.x = Mathf.Clamp(panel.Position.x, 0f, Screen.width - width);
        panel.Position.y = Mathf.Clamp(panel.Position.y, 0f, Screen.height - height);
        GUI.backgroundColor = Color.black;

        DrawPanelBackground(panel, width, height);
        DrawPanelContent(panel, width, height);
        DrawPanelHeader(panel, width);
        DrawPanelButtons(panel, width);
    }

    private void DrawPanelBackground(PanelState panel, float width, float height)
    {
        if (_cachedPanelBackground == null)
        {
            _cachedPanelBackground = new Texture2D(1, 1);
            _cachedPanelBackground.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.2f, 0.95f));
            _cachedPanelBackground.Apply();
        }

        ImguiCompat.DrawTexture(new Rect(panel.Position.x, panel.Position.y, width, height),
            _cachedPanelBackground);
    }

    private void DrawPanelContent(PanelState panel, float width, float height)
    {
        var contentLines = panel.Content.Split(['\n'], StringSplitOptions.None);
        var contentTop = panel.Position.y + 40f;
        var contentWidth = width - 20f;
        panel.ScrollPosition = GUI.BeginScrollView(new Rect(panel.Position.x, contentTop, width, height - 40f),
            panel.ScrollPosition, new Rect(0f, 0f, contentWidth, contentLines.Length * 25));
        var lineY = 0f;
        foreach (var line in contentLines)
            lineY = DrawContentLine(line, contentWidth, lineY);

        GUI.EndScrollView();
    }

    private float DrawContentLine(string line, float contentWidth, float lineY)
    {
        if (line.StartsWith("<section>"))
        {
            var sectionTitle = line.Replace("<section>", "").Replace("</section>", "");
            GUI.Label(new Rect(10f, lineY, contentWidth, 25f), sectionTitle, _sectionHeaderStyle);
            return lineY + 30f;
        }

        if (line.Contains("<label>"))
        {
            var labelText = ExtractTaggedText(line, "<label>", "</label>");
            var isHighlight = line.Contains("<value-good>");
            var valueText = ExtractTaggedText(line,
                isHighlight ? "<value-good>" : "<value>",
                isHighlight ? "</value-good>" : "</value>");
            if (string.IsNullOrEmpty(labelText)) return lineY;

            GUI.Label(new Rect(10f, lineY, 140f, 20f), labelText, _infoLabelStyle);
            if (!string.IsNullOrEmpty(valueText))
            {
                GUI.Label(new Rect(160f, lineY, contentWidth - 160f, 20f), valueText,
                    isHighlight ? _infoValueHighlightStyle : _infoValueStyle);
            }

            return lineY + 25f;
        }

        if (line.StartsWith("<value>"))
        {
            var valueLine = line.Replace("<value>", "").Replace("</value>", "");
            GUI.Label(new Rect(20f, lineY, contentWidth - 20f, 20f), valueLine, _infoValueStyle);
            return lineY + 25f;
        }

        if (string.IsNullOrWhiteSpace(line)) return lineY;

        GUI.Label(new Rect(10f, lineY, contentWidth, 20f), line, _infoValueStyle);
        return lineY + 25f;
    }

    private static string ExtractTaggedText(string line, string openTag, string closeTag)
    {
        var openIndex = line.IndexOf(openTag, StringComparison.Ordinal);
        if (openIndex < 0) return "";

        var start = openIndex + openTag.Length;
        var end = line.IndexOf(closeTag, StringComparison.Ordinal);
        return end < start ? "" : line.Substring(start, end - start);
    }

    private static void DrawPanelHeader(PanelState panel, float width)
    {
        GUI.Box(new Rect(panel.Position.x, panel.Position.y, width, 40f), "", _salesPanelStyle);
        GUI.Label(new Rect(panel.Position.x, panel.Position.y, width, 40f), panel.Title + " (Drag to Move)",
            _salesHeaderStyle);
    }

    private void DrawPanelButtons(PanelState panel, float width)
    {
        if (GUI.Button(new Rect(panel.Position.x + width - 30f, panel.Position.y + 5f, 25f, 25f), "X"))
        {
            panel.IsVisible = false;
            panel.Position = new Vector2(20f, 20f);
            if (panel == _salesPanel)
                UIHandler.SelectedProduct = null;
            else if (panel == _contactPanel) UIHandler.SelectedCustomer = null;
        }

        if (GUI.Button(new Rect(panel.Position.x + width - 60f, panel.Position.y + 5f, 25f, 25f), "+"))
            ChangeGuiScale(1);

        if (GUI.Button(new Rect(panel.Position.x + width - 90f, panel.Position.y + 5f, 25f, 25f), "-"))
            ChangeGuiScale(-1);
    }

    private void ChangeGuiScale(int delta)
    {
        _guiScale = Mathf.Clamp(_guiScale + delta, 1, MaxGuiScale);
        ApplyGuiScale();
    }

    private void ApplyGuiScale()
    {
        _sectionHeaderStyle.fontSize = 16 + _guiScale;
        _sectionHeaderStyle.padding = new RectOffset(0, 0, 10 + _guiScale * 4, 5 + _guiScale * 2);
        _infoLabelStyle.fontSize = 14 + _guiScale;
        _infoLabelStyle.padding = new RectOffset(10, 0, 2 + _guiScale * 4, 2 + _guiScale * 4);
        _infoValueStyle.fontSize = 14 + _guiScale;
        _infoValueStyle.padding = new RectOffset(10 + _guiScale * 8, 0, 2 + _guiScale * 2, 8 + _guiScale * 4);
        _infoValueHighlightStyle.fontSize = 14 + _guiScale;
        _infoValueHighlightStyle.padding = new RectOffset(10 + _guiScale * 8, 0, 2 + _guiScale * 2,
            8 + _guiScale * 4);
    }
}

internal class PanelState(string title)
{
    public string Content = "";

    public Vector2 DragOffset = Vector2.zero;

    public bool IsDragging = false;
    public bool IsVisible = false;

    public Vector2 Position = new(20f, 20f);

    public Vector2 ScrollPosition = Vector2.zero;

    public readonly string Title = title;
}
