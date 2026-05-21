using UnityEngine;
using UnityEngine.UI;

public static class UiFont
{
    private static Font _cachedHudFont;

    public static Font GetHudFont ()
    {
        if (_cachedHudFont != null)
        {
            return _cachedHudFont;
        }

        _cachedHudFont = Resources.Load<Font> ("Fonts/DejaVuSans");
        if (_cachedHudFont != null)
        {
            return _cachedHudFont;
        }

        try
        {
            _cachedHudFont = Font.CreateDynamicFontFromOSFont (
                new[] { "DejaVu Sans", "Liberation Sans", "Noto Sans", "FreeSans", "Arial" },
                32);
            if (_cachedHudFont != null)
            {
                return _cachedHudFont;
            }
        }
        catch
        {
            // Fall back to built-in font below.
        }

        _cachedHudFont = Resources.GetBuiltinResource<Font> ("LegacyRuntime.ttf");
        if (_cachedHudFont == null)
        {
            _cachedHudFont = Resources.GetBuiltinResource<Font> ("Arial.ttf");
        }

        return _cachedHudFont;
    }

    public static void ApplyTo (Text text)
    {
        if (text == null)
        {
            return;
        }

        Font font = GetHudFont ();
        if (font != null)
        {
            text.font = font;
        }
    }
}
