using UnityEngine;

namespace HighBaller.Helpers;

internal static class ImguiCompat
{
    private static GUIStyle _style;

    internal static void DrawTexture(Rect position, Texture2D image)
    {
        if (image == null || Event.current == null || Event.current.type != EventType.Repaint) return;
        _style ??= new GUIStyle();
        _style.normal.background = image;
        var backgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.white;
        _style.Draw(position, false, false, false, false);
        GUI.backgroundColor = backgroundColor;
    }
}