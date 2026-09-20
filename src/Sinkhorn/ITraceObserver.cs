namespace Sinkhorn;

public interface ITraceObserver
{
    void Observe(TraceFrame frame);
}
