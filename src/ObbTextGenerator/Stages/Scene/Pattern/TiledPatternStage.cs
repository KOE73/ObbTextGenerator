using SkiaSharp;

namespace ObbTextGenerator;

internal sealed class PatternLayerInstance(PatternLayerSettingsBase settings, IColorProvider colorProvider)
{
    public PatternLayerSettingsBase Settings { get; } = settings;
    public IColorProvider ColorProvider { get; } = colorProvider;
}

public sealed class TiledPatternStage(
    PatternStageSettings settings,
    FullConfig config,
    StageFactoryContext factoryContext) : RenderStageBase(settings)
{
    private readonly PatternStageSettings _settings = settings;
    private readonly FullConfig _config = config;
    private readonly StageFactoryContext _factoryContext = factoryContext;

    public override string Name => "Background/TiledPattern";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;
        NamedPatternSettings? selected = null;

        // 1. Resolve from library
        if (!string.IsNullOrEmpty(_settings.PatternName))
        {
            selected = _config.Patterns.FirstOrDefault(p => string.Equals(p.Name, _settings.PatternName, StringComparison.OrdinalIgnoreCase));
        }
        else if (!string.IsNullOrEmpty(_settings.Group))
        {
            var options = _config.Patterns.Where(p => string.Equals(p.Group, _settings.Group, StringComparison.OrdinalIgnoreCase)).ToList();
            if (options.Count > 0) selected = options[random.Next(options.Count)];
        }
        else
        {
            // Pick any random
            if (_config.Patterns != null && _config.Patterns.Count > 0)
                selected = _config.Patterns[random.Next(_config.Patterns.Count)];
        }

        if (selected == null) return;
        context.AddTrace($"tiled-pattern: {selected.Name}");

        // Store active pattern info in context for reporting/exporters (last applied pattern wins)
        context.ActivePattern = selected.Pattern;
        context.ActivePatternName = selected.Name;

        var patternConfig = selected.Pattern;
        var layers = ResolveLayers(patternConfig);
        // 2. Render to tile
        int baseSize = (int)Math.Max(window.Width, window.Height);
        int tileSize = patternConfig.TileSize.SampleInt(random, baseSize);
        tileSize = Math.Max(tileSize, 1);
        
        var imgInfo = new SKImageInfo(tileSize, tileSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(imgInfo);
        using (var tileCanvas = new SKCanvas(bitmap))
        {
            tileCanvas.Clear(SKColors.Transparent);
            foreach (var layer in layers)
            {
                DrawLayer(tileCanvas, layer, tileSize, context, random);
            }
        }

        // 3. Render tile to main canvas
        float alpha = _settings.Alpha.Sample(random);
        using var paintPattern = new SKPaint
        {
            BlendMode = _settings.BlendMode,
            Color = SKColors.White.WithAlpha((byte)(alpha * 255)),
            IsAntialias = true
        };
        
        paintPattern.Shader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

        float globalRotation = patternConfig.GlobalRotation.Sample(random);

        context.Canvas.Save();
        context.Canvas.RotateDegrees(globalRotation, window.Center.X, window.Center.Y);
        float margin = Math.Max(window.Width, window.Height);
        context.Canvas.DrawRect(window.Left - margin, window.Top - margin, window.Width + margin * 2, window.Height + margin * 2, paintPattern);
        context.Canvas.Restore();
    }

    private List<PatternLayerInstance> ResolveLayers(TiledPatternStageSettings settings)
    {
        var result = new List<PatternLayerInstance>();
        foreach (var layer in settings.Layers)
        {
            var colorSettings = layer.Color ?? new ConstantColorProviderSettings { Color = "#000000" };
            var colorProvider = ColorProviderFactory.Create(colorSettings, _factoryContext);
            result.Add(new PatternLayerInstance(layer, colorProvider));
        }
        return result;
    }

    private void DrawLayer(SKCanvas canvas, PatternLayerInstance layerInstance, int tileSize, RenderContext context, Random random)
    {
        var layer = layerInstance.Settings;
        var colorProvider = layerInstance.ColorProvider;
        
        int count = GetLayerCount(layer, random);

        for (int i = 0; i < count; i++)
        {
            var baseColor = colorProvider.GetColor(context);
            float layerAlpha = layer.Alpha.Sample(random);
            
            using var paint = new SKPaint
            {
                Color = baseColor.WithAlpha((byte)(baseColor.Alpha * layerAlpha)),
                IsAntialias = true,
                BlendMode = layer.BlendMode,
                Style = GetLayerStyle(layer)
            };

            SetLayerThickness(paint, layer, random);

            switch (layer)
            {
                case FillLayerSettings fill:
                    canvas.DrawRect(0, 0, tileSize, tileSize, paint);
                    break;
                case RectFixedLayerSettings fixedRect:
                    DrawFixedRect(canvas, fixedRect, tileSize, paint);
                    break;
                case RectRandomLayerSettings rndRect:
                    DrawRandomRect(canvas, rndRect, tileSize, paint, random);
                    break;
                case CircleRandomLayerSettings rndCircle:
                    DrawRandomCircle(canvas, rndCircle, tileSize, paint, random);
                    break;
                case LineRandomLayerSettings rndLine:
                    DrawRandomLine(canvas, tileSize, paint, random);
                    break;
                case ScatterRandomLayerSettings scatter:
                    DrawScatter(canvas, scatter, tileSize, paint, random);
                    break;
                case GridProceduralLayerSettings grid:
                    DrawGrid(canvas, grid, tileSize, paint, random);
                    break;
                case PerlinProceduralLayerSettings perlin:
                    DrawPerlin(canvas, perlin, tileSize, paint, random);
                    break;
            }
        }
    }

    private int GetLayerCount(PatternLayerSettingsBase layer, Random random)
    {
        return layer switch
        {
            RectRandomLayerSettings r => r.Count.SampleInt(random),
            CircleRandomLayerSettings r => r.Count.SampleInt(random),
            LineRandomLayerSettings r => r.Count.SampleInt(random),
            ScatterRandomLayerSettings r => r.Count.SampleInt(random),
            _ => 1
        };
    }

    private SKPaintStyle GetLayerStyle(PatternLayerSettingsBase layer)
    {
        return layer switch
        {
            LineRandomLayerSettings => SKPaintStyle.Stroke,
            GridProceduralLayerSettings => SKPaintStyle.Stroke,
            _ => SKPaintStyle.Fill
        };
    }

    private void SetLayerThickness(SKPaint paint, PatternLayerSettingsBase layer, Random random)
    {
        if (layer is LineRandomLayerSettings line)
        {
            paint.StrokeWidth = line.Thickness.Sample(random);
        }
        else if (layer is GridProceduralLayerSettings grid)
        {
            paint.StrokeWidth = grid.Thickness.Sample(random);
        }
    }

    private void DrawFixedRect(SKCanvas canvas, RectFixedLayerSettings layer, int tileSize, SKPaint paint)
    {
        float x = layer.Rect.Length > 0 ? layer.Rect[0] * tileSize : 0;
        float y = layer.Rect.Length > 1 ? layer.Rect[1] * tileSize : 0;
        float w = layer.Rect.Length > 2 ? layer.Rect[2] * tileSize : tileSize;
        float h = layer.Rect.Length > 3 ? layer.Rect[3] * tileSize : tileSize;
        canvas.DrawRect(x, y, w, h, paint);
    }

    private void DrawRandomLine(SKCanvas canvas, int tileSize, SKPaint paint, Random random)
    {
        float x1 = (float)random.NextDouble() * tileSize;
        float y1 = (float)random.NextDouble() * tileSize;
        float x2 = (float)random.NextDouble() * tileSize;
        float y2 = (float)random.NextDouble() * tileSize;
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    private void DrawRandomRect(SKCanvas canvas, RectRandomLayerSettings layer, int tileSize, SKPaint paint, Random random)
    {
        float w = layer.Width.Sample(random) * tileSize;
        float h = layer.Height.Sample(random) * tileSize;
        float x = (float)random.NextDouble() * (tileSize - w);
        float y = (float)random.NextDouble() * (tileSize - h);
        float rotation = layer.Rotation.Sample(random);

        canvas.Save();
        canvas.RotateDegrees(rotation, x + w / 2, y + h / 2);
        canvas.DrawRect(x, y, w, h, paint);
        canvas.Restore();
    }

    private void DrawRandomCircle(SKCanvas canvas, CircleRandomLayerSettings layer, int tileSize, SKPaint paint, Random random)
    {
        float r = layer.Width.Sample(random) * tileSize / 2f;
        float x = (float)(random.NextDouble() * (tileSize - r * 2) + r);
        float y = (float)(random.NextDouble() * (tileSize - r * 2) + r);
        canvas.DrawCircle(x, y, r, paint);
    }

    private void DrawGrid(SKCanvas canvas, GridProceduralLayerSettings layer, int tileSize, SKPaint paint, Random random)
    {
        float stepX = layer.SpacingX * tileSize;
        float stepY = layer.SpacingY * tileSize;
        if (stepX <= 0) stepX = tileSize;
        if (stepY <= 0) stepY = tileSize;

        float rotation = layer.Rotation.Sample(random);
        float jitterAmp = layer.PositionJitter * tileSize;

        canvas.Save();
        canvas.RotateDegrees(rotation, tileSize / 2f, tileSize / 2f);
        
        // Draw larger to cover area after rotation
        float margin = tileSize;
        if (jitterAmp <= 0f)
        {
            for (float x = -margin; x <= tileSize + margin; x += stepX) canvas.DrawLine(x, -margin, x, tileSize + margin, paint);
            for (float y = -margin; y <= tileSize + margin; y += stepY) canvas.DrawLine(-margin, y, tileSize + margin, y, paint);
        }
        else
        {
            // With jitter: we make small segments slightly offset
            using var path = new SKPath();
            for (float x = -margin; x <= tileSize + margin; x += stepX)
            {
                float varX = x + (float)(random.NextDouble() * 2 - 1) * jitterAmp;
                path.MoveTo(varX, -margin);
                for (float y = -margin; y <= tileSize + margin; y += stepY / 2f)
                {
                    path.LineTo(varX + (float)(random.NextDouble() * 2 - 1) * jitterAmp, y + (float)(random.NextDouble() * 2 - 1) * jitterAmp);
                }
            }
            for (float y = -margin; y <= tileSize + margin; y += stepY)
            {
                float varY = y + (float)(random.NextDouble() * 2 - 1) * jitterAmp;
                path.MoveTo(-margin, varY);
                for (float x = -margin; x <= tileSize + margin; x += stepX / 2f)
                {
                    path.LineTo(x + (float)(random.NextDouble() * 2 - 1) * jitterAmp, varY + (float)(random.NextDouble() * 2 - 1) * jitterAmp);
                }
            }
            canvas.DrawPath(path, paint);
        }
        
        canvas.Restore();
    }

    private void DrawScatter(SKCanvas canvas, ScatterRandomLayerSettings layer, int tileSize, SKPaint paint, Random random)
    {
        float w = layer.Width.Sample(random) * tileSize;
        float h = w; // scatter dots
        float x = (float)random.NextDouble() * (tileSize - w);
        float y = (float)random.NextDouble() * (tileSize - h);
        float rotation = layer.Rotation.Sample(random);

        canvas.Save();
        canvas.RotateDegrees(rotation, x + w / 2, y + h / 2);
        canvas.DrawLine(x, y, x + w, y + h, paint);
        canvas.Restore();
    }

    private void DrawPerlin(SKCanvas canvas, PerlinProceduralLayerSettings layer, int tileSize, SKPaint paint, Random random)
    {
        using var perlinPaint = new SKPaint
        {
            BlendMode = layer.BlendMode,
            IsAntialias = true
        };

        perlinPaint.Shader = SKShader.CreatePerlinNoiseFractalNoise(
            baseFrequencyX: layer.BaseFrequencyX,
            baseFrequencyY: layer.BaseFrequencyY,
            numOctaves: layer.Octaves,
            seed: random.Next());

        canvas.DrawRect(0, 0, tileSize, tileSize, perlinPaint);
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "tiled-pattern";
    }
}

public sealed class TiledPatternStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(PatternStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new TiledPatternStage((PatternStageSettings)settings, context.FullConfig, context);
    }
}
