using SkiaSharp;

namespace ObbTextGenerator;

public sealed class RandomColorProvider(RandomColorProviderSettings settings) : IColorProvider
{
    private readonly RandomColorProviderSettings _settings = settings;

    public SKColor GetColor(RenderContext context)
    {
        var random = context.Settings.Random;
        byte r, g, b, a;

        if (_settings.R != null || _settings.G != null || _settings.B != null)
        {
            r = GetValue(_settings.R, 128, random);
            g = GetValue(_settings.G, 128, random);
            b = GetValue(_settings.B, 128, random);
        }
        else
        {
            switch (_settings.Preset.ToLower())
            {
                case "dark":
                    r = (byte)random.Next(0, 101);
                    g = (byte)random.Next(0, 101);
                    b = (byte)random.Next(0, 101);
                    break;
                case "light":
                    r = (byte)random.Next(180, 256);
                    g = (byte)random.Next(180, 256);
                    b = (byte)random.Next(180, 256);
                    break;
                default:
                    r = (byte)random.Next(0, 256);
                    g = (byte)random.Next(0, 256);
                    b = (byte)random.Next(0, 256);
                    break;
            }
        }

        if (_settings.A != null)
        {
            a = GetValue(_settings.A, 255, random);
        }
        else
        {
            a = 255;
        }

        return new SKColor(r, g, b, a);
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
