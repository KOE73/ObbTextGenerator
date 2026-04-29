using SkiaSharp;
using System.Reflection;

namespace ObbTextGenerator;  

public static class SkColorHelper
{
    public static bool TryParseNamedOrHex(string value, out SKColor color)
    {
        color = default;

        if(string.IsNullOrWhiteSpace(value))
            return false;

        // Сначала пробуем штатный hex-парсер SkiaSharp.
        if(SKColor.TryParse(value, out color))
            return true;

        // Потом ищем имя среди SKColors: Red, Blue, LightGray и т.д.
        var field = typeof(SKColors).GetField(
            value,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if(field is not null && field.FieldType == typeof(SKColor))
        {
            color = (SKColor)field.GetValue(null)!;
            return true;
        }

        return false;
    }
}