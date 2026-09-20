namespace Sinkhorn.Tests;

public sealed class MetadataTests
{
    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void CompletedResultReportsDefaultEffectiveRunMetadata(SolverKind solver)
    {
        SolverResult result = SinkhornSolver.Solve(
            OrdinaryProblem(),
            0.75,
            solver,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(solver, result.Metadata.Solver);
        Assert.Equal("0.9.6.post1", result.Metadata.ReferenceVersion);
        Assert.Equal("85113e9a380f5fcf684c50c73c1ff6a164a7366e", result.Metadata.ReferenceCommit);
        Assert.Equal(0.75, result.Metadata.Regularization);
        Assert.Equal(1000, result.Metadata.EffectiveOptions.MaxIterations);
        Assert.Equal(1e-9, result.Metadata.EffectiveOptions.Threshold);
        Assert.Null(result.Metadata.EffectiveOptions.WarmStart);
        Assert.Equal("none", result.Metadata.PreprocessingPolicy);
        Assert.Equal(1.0, result.Metadata.SourceTotal);
        Assert.Equal(1.0, result.Metadata.TargetTotal);
    }

    [Fact]
    public void NumericalFailureStillReportsRunMetadata()
    {
        var problem = new TransportProblem(
            [1.0, 0.0],
            [0.0, 1.0],
            new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

        SolverResult result = SinkhornSolver.Solve(
            problem,
            1.0,
            SolverKind.Basic,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TerminationReason.NumericalBreakdown, result.Termination);
        Assert.Equal(SolverKind.Basic, result.Metadata.Solver);
        Assert.Equal(1.0, result.Metadata.Regularization);
        Assert.Equal(1.0, result.Metadata.SourceTotal);
        Assert.Equal(1.0, result.Metadata.TargetTotal);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void AcceptedMassDiscrepancyReportsBothPreparedTotals(SolverKind solver)
    {
        var problem = new TransportProblem(
            [1.0],
            [1.0000000000005],
            new double[,] { { 0.0 } });

        SolverResult result = SinkhornSolver.Solve(
            problem,
            1.0,
            solver,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1.0, result.Metadata.SourceTotal);
        Assert.Equal(1.0000000000005, result.Metadata.TargetTotal);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void EmptyWeightsReportUniformFallbackTotals(SolverKind solver)
    {
        var problem = new TransportProblem(
            [],
            [],
            new double[,] { { 0.0, 1.0, 2.0 }, { 2.0, 1.0, 0.0 } });

        SolverResult result = SinkhornSolver.Solve(
            problem,
            1.0,
            solver,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1.0, result.Metadata.SourceTotal);
        Assert.Equal(1.0, result.Metadata.TargetTotal);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void MetadataOwnsCustomInputsBeforeObserverMutation(SolverKind solver)
    {
        double[] source = [0.5, 0.5];
        double[] target = [0.5, 0.5];
        double[] sourceWarmStart = [0.25, -0.5];
        double[] targetWarmStart = [-0.75, 0.5];
        var options = new SolverOptions(
            MaxIterations: 1,
            Threshold: double.Epsilon,
            WarmStart: new WarmStart(sourceWarmStart, targetWarmStart));
        var observer = new CallerInputMutatingObserver(
            source,
            target,
            sourceWarmStart,
            targetWarmStart);

        SolverResult result = SinkhornSolver.Solve(
            new TransportProblem(
                source,
                target,
                new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } }),
            0.5,
            solver,
            options,
            observer,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Metadata.EffectiveOptions.MaxIterations);
        Assert.Equal(double.Epsilon, result.Metadata.EffectiveOptions.Threshold);
        Assert.NotNull(result.Metadata.EffectiveOptions.WarmStart);
        Assert.NotSame(sourceWarmStart, result.Metadata.EffectiveOptions.WarmStart.SourceLogScaling);
        Assert.NotSame(targetWarmStart, result.Metadata.EffectiveOptions.WarmStart.TargetLogScaling);
        Assert.Equal([0.25, -0.5], result.Metadata.EffectiveOptions.WarmStart.SourceLogScaling);
        Assert.Equal([-0.75, 0.5], result.Metadata.EffectiveOptions.WarmStart.TargetLogScaling);
        Assert.Equal(1.0, result.Metadata.SourceTotal);
        Assert.Equal(1.0, result.Metadata.TargetTotal);
    }

    private static TransportProblem OrdinaryProblem() => new(
        [0.5, 0.5],
        [0.5, 0.5],
        new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

    private sealed class CallerInputMutatingObserver(params double[][] values) : ITraceObserver
    {
        public void Observe(TraceFrame frame)
        {
            foreach (double[] value in values)
            {
                Array.Fill(value, 42.0);
            }
        }
    }
}
