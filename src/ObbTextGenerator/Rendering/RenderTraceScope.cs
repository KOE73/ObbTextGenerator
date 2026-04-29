namespace ObbTextGenerator;

public sealed class RenderTraceScope(RenderContext context) : IDisposable
{
    private readonly RenderContext _context = context;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _context.DecreaseTraceDepth();
    }
}
