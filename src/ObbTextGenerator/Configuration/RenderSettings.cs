namespace ObbTextGenerator;

public sealed class RenderSettings
{
    public RenderSettings(int width, int height, int? randomSeed = null)
    {
        Width = width;
        Height = height;
        Random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random(Random.Shared.Next());
    }

    public RenderSettings(int width, int height, Random random)
    {
        Width = width;
        Height = height;
        Random = random;
    }

    /// <summary>
    /// Final output image width in pixels.
    /// This is the target viewport width for all stages.
    /// </summary>
    public  int Width { get;  }

    /// <summary>
    /// Final output image height in pixels.
    /// This is the target viewport height for all stages.
    /// </summary>
    public  int Height { get;  }

    /// <summary>
    /// Enables extra debug data and optional debug overlays.
    /// </summary>
    public bool DebugEnabled { get;  }

    public  Random Random { get;  }
}
