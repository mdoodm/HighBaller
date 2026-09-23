using UnityEngine;

namespace HighBaller;

internal static class ImguiCompat
{
	private static GUIStyle _style;

	internal static void DrawTexture(Rect position, Texture2D image)
	{
		if (!(image == null) && Event.current != null && Event.current.type == EventType.Repaint)
		{
			if (_style == null)
			{
				_style = new GUIStyle();
			}
			_style.normal.background = image;
			Color backgroundColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.white;
			_style.Draw(position, false, false, false, false);
			GUI.backgroundColor = backgroundColor;
		}
	}
}
