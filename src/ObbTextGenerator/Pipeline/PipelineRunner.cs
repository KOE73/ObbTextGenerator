using SkiaSharp;
using Spectre.Console;

namespace ObbTextGenerator;

/// <summary>
/// Executes the rendering pipeline multiple times to generate a synthetic dataset.
/// </summary>
public sealed class PipelineRunner(GenerationSettings generationSettings, List<IPipelineStage> pipeline, int maxParallelism)
{
    private readonly GenerationSettings _generationSettings = generationSettings;
    private readonly List<IPipelineStage> _pipeline = pipeline;
    private readonly int _maxParallelism = maxParallelism;

    /// <summary>
    /// Starts the generation process.
    /// </summary>
    public void Run()
    {
        var resolvedParallelism = ResolveParallelism();
        InitializeStages(resolvedParallelism);

        AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                    [
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new RemainingTimeColumn(),
                        new SpinnerColumn(),
                    ])
                    .Start(progressContext =>
                    {
                        var task = progressContext.AddTask("[green]Generating images[/]", maxValue: _generationSettings.SampleCount);
                        var progressLock = new object();
                        var parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = resolvedParallelism
                        };

                        Parallel.For(0, _generationSettings.SampleCount, parallelOptions, index =>
                        {
                            GenerateOne(index);

                            lock (progressLock)
                            {
                                task.Description = $"[green]Generating images ({resolvedParallelism} threads)[/]";
                                task.Increment(1);
                            }
                        });
                    });

        AnsiConsole.MarkupLine("[bold green]Generation completed successfully![/]");
    }

    private void InitializeStages(int resolvedParallelism)
    {
        var initializationContext = new PipelineStageInitializationContext
        {
            GenerationSettings = _generationSettings,
            MaxParallelism = resolvedParallelism
        };

        foreach (var stage in _pipeline)
        {
            if (stage is not IPipelineStageInitializer initializer)
            {
                continue;
            }

            initializer.Initialize(initializationContext);
        }
    }

    private int ResolveParallelism()
    {
        if (_maxParallelism > 0)
        {
            return _maxParallelism;
        }

        if (Environment.ProcessorCount <= 2)
        {
            return 1;
        }

        return Math.Max(1, Environment.ProcessorCount - 1);
    }

    private void GenerateOne(int index)
    {
        // 1. Determine the set name based on the split
        var trainCount = (int)(_generationSettings.SampleCount * _generationSettings.TrainSplit);
        var setName = index < trainCount ? "train" : "val";

        // 2. Prepare sample-specific settings and context
        var renderSettings = new RenderSettings(_generationSettings.Width, _generationSettings.Height, Random.Shared.Next());
        var session = new RenderSession();

        using var bitmap = new SKBitmap(_generationSettings.Width, _generationSettings.Height);
        using var canvas = new SKCanvas(bitmap);

        var context = session.CreateRootContext(renderSettings, bitmap, canvas, index, setName);

        // 3. Execute all pipeline stages
        foreach (var stage in _pipeline)
        {
            stage.Apply(context);
        }
    }
}
