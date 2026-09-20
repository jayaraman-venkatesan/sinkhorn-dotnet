namespace Sinkhorn.Tests;

public sealed class CancellationTests
{
    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void CancellationBeforeFirstPairThrows(SolverKind solver)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var observer = new CountingObserver();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => SinkhornSolver.Solve(
                RectangularProblem(),
                0.3,
                solver,
                observer: observer,
                cancellationToken: source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(0, observer.Calls);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void CancellationBetweenPairsThrowsAtNextBoundary(SolverKind solver)
    {
        using var source = new CancellationTokenSource();
        var observer = new CancelAfterFirstPair(source);

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => SinkhornSolver.Solve(
                RectangularProblem(),
                0.3,
                solver,
                new SolverOptions(MaxIterations: 100, Threshold: double.Epsilon),
                observer,
                source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(observer.SawCompletedFirstPair);
    }

    private static TransportProblem RectangularProblem() => new(
        [20.0, 30.0, 50.0],
        [40.0, 60.0],
        new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 }, { 0.5, 0.2 } });

    private sealed class CancelAfterFirstPair(CancellationTokenSource source) : ITraceObserver
    {
        public bool SawCompletedFirstPair { get; private set; }

        public void Observe(TraceFrame frame)
        {
            if (frame.Index == 0 && frame.Phase == TracePhase.AfterSource)
            {
                SawCompletedFirstPair = true;
                source.Cancel();
            }
        }
    }

    private sealed class CountingObserver : ITraceObserver
    {
        public int Calls { get; private set; }

        public void Observe(TraceFrame frame) => Calls++;
    }
}
