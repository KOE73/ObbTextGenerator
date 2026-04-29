using SkiaSharp;

namespace ObbTextGenerator;

public sealed class GrayColorProvider(GrayColorProviderSettings settings) : IColorProvider
{
    private readonly GrayColorProviderSettings _settings = settings;

    public SKColor GetColor(RenderContext context)
    {
        var random = context.Settings.Random;
        byte intensity = GetValue(_settings.Intensity, 128, random);
        byte alpha = GetValue(_settings.Alpha, 255, random);
        return new SKColor(intensity, intensity, intensity, alpha);
    }

    private byte GetValue(object? input, byte defaultValue, Random random)
    {
        if (input == null) return defaultValue;

        if (input is int i) return (byte)Math.Clamp(i, 0, 255);
        if (input is long l) return (byte)Math.Clamp(l, 0, 255);
        
        if (input is List<object> list && list.Count >= 2)
        {
            int min = Convert.ToInt32(list[0]);
            int max = Convert.ToInt32(list[1]);
            return (byte)random.Next(Math.Clamp(min, 0, 255), Math.Clamp(max, 0, 255) + 1);
        }

        return defaultValue;
    }
}
