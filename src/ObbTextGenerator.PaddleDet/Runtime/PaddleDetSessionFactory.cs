using NeuroModFlowNet.ONNX;
using OpenCvSharp;

namespace ObbTextGenerator;

/// <summary>
/// Creates fully initialized PaddleDet model sessions for a fixed input size.
/// </summary>
public static class PaddleDetSessionFactory
{
    public static PaddleDetSession Create(
        string modelPath,
        InferenceBackend backend,
        int imageWidth,
        int imageHeight)
    {
        var model = new OnnxModel(modelPath, backend, configure: null);
        EnsurePersistentValuesInitialized(model, imageWidth, imageHeight);

        //var runner = PaddleOCRDetFactory.CreateRunner<Mat, Mat>(model, MatType.CV_8UC1);
        var runner = PaddleOCRDetFactory.Single_FP32_32FC1_Unsafe(model);

        return new PaddleDetSession
        {
            Model = model,
            Runner = runner
        };
    }

    private static void EnsurePersistentValuesInitialized(OnnxModel model, int imageWidth, int imageHeight)
    {
        if(model.IsInputPersistentValueInitialized(model.PrimaryInputName))
        {
            return;
        }

        var inputShape = new long[] { 1, 3, imageWidth, imageHeight };
        var outputShape = new long[] { 1, 1, imageWidth, imageHeight };

        model.InitInputPersistentValue(model.PrimaryInputName, inputShape);
        model.InitOutputPersistentValue(model.PrimaryOutputName, outputShape);
    }
}
