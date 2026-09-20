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

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void CancellationOnConvergingPairThrowsInsteadOfReturningThresholdMet(
        SolverKind solver)
    {
        using var source = new CancellationTokenSource();
        var observer = new CancelAtPhase(
            source,
            0,
            TracePhase.AfterSource);
        var problem = new TransportProblem(
            [0.5, 0.5],
            [0.5, 0.5],
            new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => SinkhornSolver.Solve(
                problem,
                1.0,
                solver,
                observer: observer,
                cancellationToken: source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(observer.Cancelled);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void CancellationOnLastBudgetedPairThrowsInsteadOfReturningIterationLimit(
        SolverKind solver)
    {
        using var source = new CancellationTokenSource();
        var observer = new CancelAtPhase(
            source,
            0,
            TracePhase.AfterSource);

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => SinkhornSolver.Solve(
                RectangularProblem(),
                0.3,
                solver,
                new SolverOptions(MaxIterations: 1, Threshold: double.Epsilon),
                observer,
                source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(observer.Cancelled);
    }

    [Theory]
    [InlineData(TracePhase.AfterSource)]
    [InlineData(TracePhase.Restored)]
    public void CancellationOnBasicRestoringPairThrowsInsteadOfReturningBreakdown(
        TracePhase cancellationPhase)
    {
        using var source = new CancellationTokenSource();
        var observer = new CancelAtPhase(
            source,
            0,
            cancellationPhase);
        var problem = new TransportProblem(
            [1.0, 0.0],
            [0.0, 1.0],
            new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => SinkhornSolver.Solve(
                problem,
                1.0,
                SolverKind.Basic,
                observer: observer,
                cancellationToken: source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(observer.Cancelled);
        Assert.True(observer.SawRestoration);
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

    private sealed class CancelAtPhase(
        CancellationTokenSource source,
        int index,
        TracePhase phase) : ITraceObserver
    {
        public bool Cancelled { get; private set; }

        public bool SawRestoration { get; private set; }

        public void Observe(TraceFrame frame)
        {
            if (frame.Phase == TracePhase.Restored)
            {
                SawRestoration = true;
            }

            if (frame.Index == index && frame.Phase == phase)
            {
                Cancelled = true;
                source.Cancel();
            }
        }
    }
}
