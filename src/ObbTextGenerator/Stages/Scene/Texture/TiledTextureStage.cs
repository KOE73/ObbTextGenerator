using SkiaSharp;

namespace ObbTextGenerator;

public sealed class TiledTextureStage(TiledTextureStageSettings settings) : RenderStageBase(settings)
{
    private readonly TiledTextureStageSettings _settings = settings;

    public override string Name => "Background/TiledTexture";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;
        int tileSize = _settings.TileSize.SampleInt(random);
        float alpha = _settings.Alpha.Sample(random);

        using var bitmap = new SKBitmap(tileSize, tileSize);
        using (var tileCanvas = new SKCanvas(bitmap))
        {
            tileCanvas.Clear(SKColors.White);
            
            using var paint = new SKPaint 
            { 
                Color = SKColors.Gray.WithAlpha((byte)(alpha * 255)),
                Style = SKPaintStyle.Fill
            };

            // Simple woven pattern: vertical half and horizontal half
            tileCanvas.DrawRect(0, 0, tileSize / 2, tileSize, paint);
            
            paint.Color = SKColors.DarkGray.WithAlpha((byte)(alpha * 128));
            tileCanvas.DrawRect(0, 0, tileSize, tileSize / 2, paint);
        }

        using var paintSack = new SKPaint();
        paintSack.Shader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

        context.Canvas.Save();
        context.Canvas.RotateDegrees(_settings.Rotation.Sample(random), window.Center.X, window.Center.Y);
        var margin = Math.Max(window.Width, window.Height);
        context.Canvas.DrawRect(window.Left - margin, window.Top - margin, window.Width + margin * 2, window.Height + margin * 2, paintSack);
        context.Canvas.Restore();
    }
}

public sealed class TiledTextureStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(TiledTextureStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new TiledTextureStage((TiledTextureStageSettings)settings);
    }
}
