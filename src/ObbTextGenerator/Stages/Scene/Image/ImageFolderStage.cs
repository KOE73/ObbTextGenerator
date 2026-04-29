using SkiaSharp;

namespace ObbTextGenerator;

public sealed class ImageFolderStage(
    ImageFolderStageSettings settings,
    List<string> imagePaths) : RenderStageBase(settings)
{
    private static readonly ImageFolderAugmentationSettings DefaultAugmentSettings = new();
    private readonly ImageFolderStageSettings _settings = settings;
    private readonly List<string> _imagePaths = imagePaths;

    public override string Name => "Background/ImageFolder";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        if (_imagePaths.Count == 0)
        {
            return;
        }

        var augment = GetAugmentSettings();
        var random = context.Settings.Random;
        var imagePath = _imagePaths[random.Next(_imagePaths.Count)];
        context.AddTrace($"image-folder: {Path.GetFileName(imagePath)}");

        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap == null)
        {
            return;
        }

        using var paint = CreatePaint(random);
        var destinationRect = ResolveDestinationRect(bitmap, window, random);
        var rotation = augment.Rotation.Sample(random);
        var flipScaleX = random.NextDouble() < augment.FlipXProbability ? -1f : 1f;
        var flipScaleY = random.NextDouble() < augment.FlipYProbability ? -1f : 1f;

        context.Canvas.Save();
        context.Canvas.Translate(window.Center.X, window.Center.Y);
        context.Canvas.RotateDegrees(rotation);
        context.Canvas.Scale(flipScaleX, flipScaleY);

        var localRect = SKRect.Create(
            destinationRect.Left - window.Center.X,
            destinationRect.Top - window.Center.Y,
            destinationRect.Width,
            destinationRect.Height);

        context.Canvas.DrawBitmap(bitmap, localRect, paint);
        context.Canvas.Restore();
    }

    private SKPaint CreatePaint(Random random)
    {
        var augment = GetAugmentSettings();
        var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.BlendMode = _settings.BlendMode;

        var alpha = _settings.Alpha.Sample(random);
        paint.Color = SKColors.White.WithAlpha((byte)(Math.Clamp(alpha, 0f, 1f) * 255));

        var colorFilter = CreateColorFilter(random);
        if (colorFilter != null)
        {
            paint.ColorFilter = colorFilter;
        }

        var blurSigma = augment.Blur.Sample(random);
        if (blurSigma > 0.01f)
        {
            paint.ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
        }

        return paint;
    }

    private SKColorFilter? CreateColorFilter(Random random)
    {
        var augment = GetAugmentSettings();
        var brightnessShift = augment.Brightness.Sample(random) ;//* 255f;
        var contrast = 1f + augment.Contrast.Sample(random);
        var saturation = 1f + augment.Saturation.Sample(random);

        contrast = Math.Max(0f, contrast);
        saturation = Math.Max(0f, saturation);

        //if (Math.Abs(brightnessShift) < 0.01f && Math.Abs(contrast - 1f) < 0.01f && Math.Abs(saturation - 1f) < 0.01f)
        //{
        //    return null;
        //}

        const float luminanceRed = 0.2126f;
        const float luminanceGreen = 0.7152f;
        const float luminanceBlue = 0.0722f;

        var inverseSaturation = 1f - saturation;
        var red = inverseSaturation * luminanceRed;
        var green = inverseSaturation * luminanceGreen;
        var blue = inverseSaturation * luminanceBlue;
        var translate = brightnessShift + (1f - contrast) * 0.5f; // * 128f;

        var matrix = new float[]
        {
            contrast * (red + saturation), contrast * green, contrast * blue, 0f, translate,
            contrast * red, contrast * (green + saturation), contrast * blue, 0f, translate,
            contrast * red, contrast * green, contrast * (blue + saturation), 0f, translate,
            0f, 0f, 0f, 1f, 0f
        };

        return SKColorFilter.CreateColorMatrix(matrix);
    }

    private SKRect ResolveDestinationRect(SKBitmap bitmap, RenderWindow window, Random random)
    {
        var augment = GetAugmentSettings();
        var windowWidth = window.Width;
        var windowHeight = window.Height;
        var scaleX = windowWidth / bitmap.Width;
        var scaleY = windowHeight / bitmap.Height;

        if (augment.FitMode == ImageFitMode.Stretch)
        {
            return SKRect.Create(window.Left, window.Top, windowWidth, windowHeight);
        }

        var fitScale = augment.FitMode == ImageFitMode.Cover
            ? Math.Max(scaleX, scaleY)
            : Math.Min(scaleX, scaleY);

        var extraScale = augment.Scale.Sample(random);
        var finalScale = fitScale * extraScale;
        var destinationWidth = bitmap.Width * finalScale;
        var destinationHeight = bitmap.Height * finalScale;

        var minLeft = Math.Min(window.Left, window.Right - destinationWidth);
        var maxLeft = Math.Max(window.Left, window.Right - destinationWidth);
        var minTop = Math.Min(window.Top, window.Bottom - destinationHeight);
        var maxTop = Math.Max(window.Top, window.Bottom - destinationHeight);

        var left = NextFloat(random, minLeft, maxLeft);
        var top = NextFloat(random, minTop, maxTop);

        return SKRect.Create(left, top, destinationWidth, destinationHeight);
    }

    private ImageFolderAugmentationSettings GetAugmentSettings()
    {
        return _settings.Augment ?? DefaultAugmentSettings;
    }

    private static float NextFloat(Random random, float minValue, float maxValue)
    {
        if (maxValue <= minValue)
        {
            return minValue;
        }

        return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "image-folder";
    }
}
