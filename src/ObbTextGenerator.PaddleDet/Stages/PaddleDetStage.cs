using NeuroModFlowNet.ONNX;
using OpenCvSharp;
using System.Diagnostics;

namespace ObbTextGenerator;

/// <summary>
/// PaddleDet integration stage.
/// The stage takes the rendered bitmap, converts it to Mat, runs PaddleDet ONNX inference,
/// stores the produced mask and polygons in the sample context, and writes OBBs into an annotation layer.
/// </summary>
public sealed class PaddleDetStage(PaddleDetStageSettings settings, string configDirectory) : IPipelineStage, IPipelineStageInitializer
{
    private readonly PaddleDetStageSettings _settings = settings;
    private readonly string _configDirectory = configDirectory;
    private IPaddleDetExecutionRuntime? _runtime;
    private string _resolvedModelPath = string.Empty;
    private InferenceBackend _resolvedBackend;

    public string Name => "Plugin/PaddleDet";

    public void Initialize(PipelineStageInitializationContext context)
    {
        if(string.IsNullOrWhiteSpace(_settings.ModelPath))
        {
            throw new InvalidOperationException("PaddleDet stage requires 'modelPath'.");
        }

        _resolvedModelPath = ResolveModelPath(_settings.ModelPath, _configDirectory);
        PaddleDetModelDownloader.EnsureModelFile(_resolvedModelPath);

        _resolvedBackend = ParseBackend(_settings.Backend);
        _runtime = CreateExecutionRuntime(
            _resolvedModelPath,
            _resolvedBackend,
            context.GenerationSettings.Width,
            context.GenerationSettings.Height,
            context.MaxParallelism,
            _settings.RunnerAccessMode);
    }

    public void Apply(RenderContext context)
    {
        var bitmapEnvelope = PaddleDetBitmapEnvelopeFactory.Create(context.Bitmap);
        using var inputMat = PaddleDetInputMatFactory.CreateBgrMat(bitmapEnvelope);

        //Cv2.ImShow("inputMat", inputMat);

        // СУТЬ: inference замеряем отдельно от постобработки, чтобы видеть реальную цену самой модели.
        // ЦЕЛЬ: честно сравнивать runtime детектора с ценой последующей геометрической обработки.
        var inferenceStopwatch = Stopwatch.StartNew();
        using var maskMat = _runtime!.Predict(inputMat);
        inferenceStopwatch.Stop();
        Debug.WriteLine(
            $"[PaddleDet] Inference: {inferenceStopwatch.Elapsed.TotalMilliseconds:F3} ms (input={inputMat.Width}x{inputMat.Height}, mask={maskMat.Width}x{maskMat.Height})");

        var polygons = PaddleDetOutputMatProcessor.ExtractObbPolygons(maskMat);

        context.SetSampleData(_settings.MaskDataKey, maskMat.Clone());
        context.SetSampleData(_settings.PolygonDataKey, polygons.Select(points => points.ToArray()).ToList());

        // This runtime layer is the contract between PaddleDet processing and downstream writers.
        var sourceLayer = context.GetOrCreateAnnotationLayer(_settings.OutputLayer);

        foreach(var polygon in polygons)
        {
            sourceLayer.Annotations.Add(new ShapeAnnotation
            {
                Points = polygon,
                ClassId = _settings.ClassId,
                Text = _settings.LabelText
            });
        }

        context.AddTrace(
            "paddledet",
            $"layer={_settings.OutputLayer} image={bitmapEnvelope.Width}x{bitmapEnvelope.Height} model={_resolvedModelPath} backend={_resolvedBackend} accessMode={_settings.RunnerAccessMode} mask={maskMat.Width}x{maskMat.Height} maskKey={_settings.MaskDataKey} polygonsKey={_settings.PolygonDataKey}");
    }

    private static string ResolveModelPath(string modelPath, string configDirectory)
    {
        if(Path.IsPathRooted(modelPath))
        {
            return modelPath;
        }

        var configRelativePath = Path.GetFullPath(modelPath, configDirectory);
        if(File.Exists(configRelativePath))
        {
            return configRelativePath;
        }

        var appBaseRelativePath = Path.GetFullPath(modelPath, AppContext.BaseDirectory);
        if(File.Exists(appBaseRelativePath))
        {
            return appBaseRelativePath;
        }

        return configRelativePath;
    }

    private static InferenceBackend ParseBackend(string backendText)
    {
        if(Enum.TryParse<InferenceBackend>(backendText, ignoreCase: true, out var backend))
        {
            return backend;
        }

        throw new InvalidOperationException(
            $"Unsupported PaddleDet backend '{backendText}'. Supported values: {string.Join(", ", Enum.GetNames<InferenceBackend>())}.");
    }

    private static IPaddleDetExecutionRuntime CreateExecutionRuntime(
        string modelPath,
        InferenceBackend backend,
        int imageWidth,
        int imageHeight,
        int maxParallelism,
        PaddleDetRunnerAccessMode runnerAccessMode)
    {
        if(runnerAccessMode == PaddleDetRunnerAccessMode.SharedLocked)
        {
            var session = PaddleDetSessionFactory.Create(modelPath, backend, imageWidth, imageHeight);
            return new SharedLockedPaddleDetExecutionRuntime(session);
        }

        var sessionCount = Math.Max(1, maxParallelism);
        var sessions = new List<PaddleDetSession>(sessionCount);

        for(var sessionIndex = 0; sessionIndex < sessionCount; sessionIndex++)
        {
            var session = PaddleDetSessionFactory.Create(modelPath, backend, imageWidth, imageHeight);
            sessions.Add(session);
        }

        return new PerThreadPaddleDetExecutionRuntime(sessions);
    }
}
