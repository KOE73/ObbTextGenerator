using SkiaSharp;

namespace ObbTextGenerator;

public static class RenderWindowResolver
{
    public static List<RenderWindow> Resolve(RenderWindowSettings settings, RenderContext context)
    {
        var parentWindow = context.GetCurrentRenderWindow();
        var random = context.Settings.Random;

        return settings.Mode switch
        {
            RenderWindowMode.SingleRect => ResolveSingleRect(settings, parentWindow, random),
            RenderWindowMode.ScatteredRects => ResolveScatteredRects(settings, parentWindow, random),
            _ => [parentWindow]
        };
    }

    private static List<RenderWindow> ResolveSingleRect(RenderWindowSettings settings, RenderWindow parentWindow, Random random)
    {
        var x = settings.X.Sample(random, parentWindow.Width);
        var y = settings.Y.Sample(random, parentWindow.Height);
        var width = settings.Width.Sample(random, parentWindow.Width);
        var height = settings.Height.Sample(random, parentWindow.Height);

        width = Math.Max(width, 1f);
        height = Math.Max(height, 1f);

        var left = ResolveSingleRectLeft(settings, parentWindow, x, width);
        var top = ResolveSingleRectTop(settings, parentWindow, y, height);

        if (!settings.AllowOutOfBounds)
        {
            width = Math.Min(width, parentWindow.Width);
            height = Math.Min(height, parentWindow.Height);

            left = Math.Clamp(left, parentWindow.Left, parentWindow.Right - width);
            top = Math.Clamp(top, parentWindow.Top, parentWindow.Bottom - height);
        }

        var bounds = SKRect.Create(left, top, width, height);
        if (!IsVisibleEnough(bounds, parentWindow.Bounds, settings.MinVisibleAreaPercent))
        {
            bounds = CreateFallbackBounds(parentWindow, width, height);
        }

        return [new RenderWindow(bounds)];
    }

    private static float ResolveSingleRectLeft(RenderWindowSettings settings, RenderWindow parentWindow, float x, float width)
    {
        if (settings.PositionMode == RenderWindowPositionMode.TopLeft)
        {
            return parentWindow.Left + x;
        }

        var centerX = parentWindow.Left + x;
        return centerX - width / 2f;
    }

    private static float ResolveSingleRectTop(RenderWindowSettings settings, RenderWindow parentWindow, float y, float height)
    {
        if (settings.PositionMode == RenderWindowPositionMode.TopLeft)
        {
            return parentWindow.Top + y;
        }

        var centerY = parentWindow.Top + y;
        return centerY - height / 2f;
    }

    private static List<RenderWindow> ResolveScatteredRects(RenderWindowSettings settings, RenderWindow parentWindow, Random random)
    {
        var windows = new List<RenderWindow>();
        var count = settings.Count.SampleInt(random);

        for (int index = 0; index < count; index++)
        {
            var bounds = ResolveScatteredRectBounds(
                parentWindow,
                settings.Width,
                settings.Height,
                settings.AllowOutOfBounds,
                settings.MinVisibleAreaPercent,
                settings.AllowOverlap,
                windows,
                random);
            windows.Add(new RenderWindow(bounds, index));
        }

        return windows;
    }

    private static SKRect ResolveScatteredRectBounds(
        RenderWindow parentWindow,
        SampledValueSpec widthSpec,
        SampledValueSpec heightSpec,
        bool allowOutOfBounds,
        float minVisibleAreaPercent,
        bool allowOverlap,
        List<RenderWindow> existingWindows,
        Random random)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            var width = Math.Clamp(widthSpec.Sample(random, parentWindow.Width), 1f, parentWindow.Width);
            var height = Math.Clamp(heightSpec.Sample(random, parentWindow.Height), 1f, parentWindow.Height);

            var left = ResolveLeft(parentWindow, width, allowOutOfBounds, random);
            var top = ResolveTop(parentWindow, height, allowOutOfBounds, random);
            var bounds = SKRect.Create(left, top, width, height);

            if (!IsVisibleEnough(bounds, parentWindow.Bounds, minVisibleAreaPercent))
            {
                continue;
            }

            if (allowOverlap || !IntersectsAny(bounds, existingWindows))
            {
                return bounds;
            }
        }

        var fallbackWidth = Math.Clamp(widthSpec.ResolveMaximum(parentWindow.Width), 1f, parentWindow.Width);
        var fallbackHeight = Math.Clamp(heightSpec.ResolveMaximum(parentWindow.Height), 1f, parentWindow.Height);
        return CreateFallbackBounds(parentWindow, fallbackWidth, fallbackHeight);
    }

    private static bool IntersectsAny(SKRect candidate, List<RenderWindow> existingWindows)
    {
        foreach (var window in existingWindows)
        {
            if (candidate.IntersectsWith(window.Bounds))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVisibleEnough(SKRect candidate, SKRect parentBounds, float minVisibleAreaPercent)
    {
        var visibleBounds = SKRect.Intersect(candidate, parentBounds);
        if (visibleBounds.IsEmpty)
        {
            return false;
        }

        var candidateArea = candidate.Width * candidate.Height;
        if (candidateArea <= 0f)
        {
            return false;
        }

        var visibleArea = visibleBounds.Width * visibleBounds.Height;
        var visibleAreaPercent = visibleArea / candidateArea * 100f;
        return visibleAreaPercent >= minVisibleAreaPercent;
    }

    private static float ResolveLeft(RenderWindow parentWindow, float width, bool allowOutOfBounds, Random random)
    {
        if (!allowOutOfBounds)
        {
            var maxLeft = parentWindow.Right - width;
            return NextFloat(random, parentWindow.Left, maxLeft);
        }

        var minLeft = parentWindow.Left - width;
        var maxLeftWithOverflow = parentWindow.Right;
        return NextFloat(random, minLeft, maxLeftWithOverflow);
    }

    private static float ResolveTop(RenderWindow parentWindow, float height, bool allowOutOfBounds, Random random)
    {
        if (!allowOutOfBounds)
        {
            var maxTop = parentWindow.Bottom - height;
            return NextFloat(random, parentWindow.Top, maxTop);
        }

        var minTop = parentWindow.Top - height;
        var maxTopWithOverflow = parentWindow.Bottom;
        return NextFloat(random, minTop, maxTopWithOverflow);
    }

    private static SKRect CreateFallbackBounds(RenderWindow parentWindow, float width, float height)
    {
        var fallbackWidth = Math.Clamp(width, 1f, parentWindow.Width);
        var fallbackHeight = Math.Clamp(height, 1f, parentWindow.Height);
        var left = parentWindow.Left + (parentWindow.Width - fallbackWidth) / 2f;
        var top = parentWindow.Top + (parentWindow.Height - fallbackHeight) / 2f;
        return SKRect.Create(left, top, fallbackWidth, fallbackHeight);
    }

    private static float NextFloat(Random random, float minValue, float maxValue)
    {
        if (maxValue <= minValue)
        {
            return minValue;
        }

        return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
    }
}
