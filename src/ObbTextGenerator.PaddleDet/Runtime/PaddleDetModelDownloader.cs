using System.Diagnostics;

namespace ObbTextGenerator;

/// <summary>
/// Ensures the default PaddleOCR detection ONNX model is available at the configured model path.
/// </summary>
public static class PaddleDetModelDownloader
{
    private const string DefaultModelUrl = "https://huggingface.co/monkt/paddleocr-onnx/resolve/main/detection/v5/det.onnx";

    public static void EnsureModelFile(string modelPath)
    {
        if(File.Exists(modelPath))
        {
            return;
        }

        DownloadModelFile(modelPath);
    }

    private static void DownloadModelFile(string modelPath)
    {
        var modelDirectory = Path.GetDirectoryName(modelPath);
        if(!string.IsNullOrWhiteSpace(modelDirectory))
        {
            Directory.CreateDirectory(modelDirectory);
        }

        var temporaryModelPath = modelPath + ".download";

        if(File.Exists(temporaryModelPath))
        {
            File.Delete(temporaryModelPath);
        }

        Debug.WriteLine($"[PaddleDet] Downloading model: {DefaultModelUrl} -> {modelPath}");

        using var httpClient = new HttpClient();
        using var response = httpClient.GetAsync(DefaultModelUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using(var responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
        using(var fileStream = File.Create(temporaryModelPath))
        {
            responseStream.CopyTo(fileStream);
        }

        File.Move(temporaryModelPath, modelPath, overwrite: false);
    }
}
