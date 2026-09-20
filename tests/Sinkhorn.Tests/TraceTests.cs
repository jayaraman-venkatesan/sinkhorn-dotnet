namespace Sinkhorn.Tests;

using System.Text.Json;

public sealed class TraceTests
{
    [Fact]
    public void BasicFailureEmitsRestoration()
    {
        var collector = new Collector();
        var problem = new TransportProblem(
            [1.0, 0.0],
            [0.0, 1.0],
            new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

        SolverResult result = SinkhornSolver.Solve(
            problem,
            1.0,
            SolverKind.Basic,
            observer: collector,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                (-1, TracePhase.Initial),
                (0, TracePhase.AfterDestination),
                (0, TracePhase.AfterSource),
                (0, TracePhase.Restored),
            ],
            collector.Frames.Select(frame => (frame.Index, frame.Phase)));
        Assert.All(collector.Frames, frame => Assert.False(frame.Rejected));
        Assert.Equal(0, result.AcceptedPairs);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void ObservationLeavesEveryResultFieldUnchanged(SolverKind solver)
    {
        var problem = RectangularProblem();
        var options = new SolverOptions(MaxIterations: 31, Threshold: 1e-9);

        SolverResult unobserved = SinkhornSolver.Solve(
            problem,
            0.3,
            solver,
            options,
            cancellationToken: TestContext.Current.CancellationToken);
        SolverResult observed = SinkhornSolver.Solve(
            problem,
            0.3,
            solver,
            options,
            new Collector(),
            TestContext.Current.CancellationToken);

        AssertResultsExactlyEqual(unobserved, observed);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void MutatingObservedScalingsCannotChangeLiveSolverState(SolverKind solver)
    {
        var problem = RectangularProblem();
        var options = new SolverOptions(MaxIterations: 31, Threshold: 1e-9);
        SolverResult expected = SinkhornSolver.Solve(
            problem,
            0.3,
            solver,
            options,
            cancellationToken: TestContext.Current.CancellationToken);

        SolverResult actual = SinkhornSolver.Solve(
            problem,
            0.3,
            solver,
            options,
            new MutatingObserver(),
            TestContext.Current.CancellationToken);

        AssertResultsExactlyEqual(expected, actual);
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void SuccessfulPairsEmitPhasesInUpdateOrder(SolverKind solver)
    {
        var collector = new Collector();
        var options = new SolverOptions(MaxIterations: 2, Threshold: double.Epsilon);

        SolverResult result = SinkhornSolver.Solve(
            RectangularProblem(),
            0.3,
            solver,
            options,
            collector,
            TestContext.Current.CancellationToken);

        Assert.Equal(TerminationReason.IterationLimit, result.Termination);
        Assert.Equal(
            [
                (-1, TracePhase.Initial),
                (0, TracePhase.AfterDestination),
                (0, TracePhase.AfterSource),
                (1, TracePhase.AfterDestination),
                (1, TracePhase.AfterSource),
            ],
            collector.Frames.Select(frame => (frame.Index, frame.Phase)));
    }

    [Theory]
    [InlineData(SolverKind.Basic)]
    [InlineData(SolverKind.LogDomain)]
    public void CallerObserverExceptionsPropagate(SolverKind solver)
    {
        var expected = new InvalidOperationException("observer failed");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(
            () => SinkhornSolver.Solve(
                RectangularProblem(),
                0.3,
                solver,
                observer: new ThrowingObserver(expected),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void PhasesMatchPinnedPythonObservation()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "phase-traces.json");
        PhaseFixtureRoot fixture = JsonSerializer.Deserialize<PhaseFixtureRoot>(
            File.ReadAllText(path),
            JsonOptions) ?? throw new InvalidOperationException("The phase fixture was empty.");

        Assert.Equal("0.9.6.post1", fixture.Provenance.PotVersion);
        Assert.Equal("85113e9a380f5fcf684c50c73c1ff6a164a7366e", fixture.Provenance.PotCommit);
        Assert.Equal("cf5efadfc0f33300899d9b8a20f762e5f96a2759", fixture.Provenance.SourceGitBlob);
        Assert.Equal(5, fixture.Cases.Length);

        foreach (PhaseFixtureCase item in fixture.Cases)
        {
            Assert.True(item.ObservationParity);
            SolverKind solver = item.Solver switch
            {
                "basic" => SolverKind.Basic,
                "log-domain" => SolverKind.LogDomain,
                _ => throw new InvalidOperationException($"Unknown fixture solver '{item.Solver}'."),
            };
            var collector = new Collector();
            SolverResult result = SinkhornSolver.Solve(
                new TransportProblem(item.Source, item.Target, Rectangular(item.Costs)),
                item.Regularization,
                solver,
                new SolverOptions(item.MaxIterations, item.Threshold),
                collector,
                TestContext.Current.CancellationToken);
            int[] restoredIndices = collector.Frames
                .Where(frame => frame.Phase == TracePhase.Restored)
                .Select(frame => frame.Index)
                .ToArray();
            TraceFrame[] retained = collector.Frames
                .Where(frame => frame.Phase != TracePhase.Initial)
                .Where(frame => frame.Index == 0
                    || frame.Index == result.LastAttemptedIndex
                    || restoredIndices.Contains(frame.Index))
                .ToArray();

            Assert.Equal(item.Snapshots.Length, retained.Length);
            for (int index = 0; index < retained.Length; index++)
            {
                PhaseSnapshot expected = item.Snapshots[index];
                TraceFrame actual = retained[index];
                bool rejected = actual.Phase is TracePhase.AfterDestination or TracePhase.AfterSource
                    && restoredIndices.Contains(actual.Index);
                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(Enum.Parse<TracePhase>(expected.Phase), actual.Phase);
                Assert.Equal(expected.Rejected, rejected);
                AssertVectorClose(item.Name, expected.SourceScaling, actual.SourceScaling);
                AssertVectorClose(item.Name, expected.TargetScaling, actual.TargetScaling);
                AssertMatrixClose(item.Name, expected.Plan, Materialize(item, actual));
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static TransportProblem RectangularProblem() => new(
        [20.0, 30.0, 50.0],
        [40.0, 60.0],
        new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 }, { 0.5, 0.2 } });

    private static double[,] Rectangular(double[][] values)
    {
        var result = new double[values.Length, values[0].Length];
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values[i].Length; j++)
            {
                result[i, j] = values[i][j];
            }
        }

        return result;
    }

    private static double[,] Materialize(PhaseFixtureCase item, TraceFrame frame)
    {
        var result = new double[item.Source.Length, item.Target.Length];
        for (int i = 0; i < item.Source.Length; i++)
        {
            for (int j = 0; j < item.Target.Length; j++)
            {
                double exponent = -item.Costs[i][j] / item.Regularization;
                result[i, j] = frame.Solver == SolverKind.Basic
                    ? (frame.SourceScaling[i] * Math.Exp(exponent)) * frame.TargetScaling[j]
                    : Math.Exp(exponent + frame.SourceScaling[i] + frame.TargetScaling[j]);
            }
        }

        return result;
    }

    private static void AssertResultsExactlyEqual(SolverResult expected, SolverResult actual)
    {
        Assert.Equal(expected.Solver, actual.Solver);
        AssertMatrixExactlyEqual(expected.Plan, actual.Plan);
        Assert.Equal(expected.Termination, actual.Termination);
        Assert.Equal(expected.LastAttemptedIndex, actual.LastAttemptedIndex);
        Assert.Equal(expected.AttemptedPairs, actual.AttemptedPairs);
        Assert.Equal(expected.AcceptedPairs, actual.AcceptedPairs);
        Assert.Equal(expected.Errors.Length, actual.Errors.Length);
        for (int i = 0; i < expected.Errors.Length; i++)
        {
            Assert.Equal(expected.Errors[i].Index, actual.Errors[i].Index);
            AssertDoubleExactlyEqual(expected.Errors[i].TargetL2, actual.Errors[i].TargetL2);
        }

        AssertVectorExactlyEqual(expected.Scaling.Source, actual.Scaling.Source);
        AssertVectorExactlyEqual(expected.Scaling.Target, actual.Scaling.Target);
        Assert.Equal(expected.Scaling.IsLog, actual.Scaling.IsLog);
        Assert.Equal(expected.Checks.Finite, actual.Checks.Finite);
        Assert.Equal(expected.Checks.Nonnegative, actual.Checks.Nonnegative);
        AssertDoubleExactlyEqual(expected.Checks.SourceL1, actual.Checks.SourceL1);
        AssertDoubleExactlyEqual(expected.Checks.TargetL1, actual.Checks.TargetL1);
        AssertDoubleExactlyEqual(expected.Checks.TotalMass, actual.Checks.TotalMass);
        Assert.Equal(expected.Checks.Usable, actual.Checks.Usable);
        AssertDoubleExactlyEqual(expected.TransportCost, actual.TransportCost);
        Assert.Equal(expected.Warnings, actual.Warnings);
    }

    private static void AssertMatrixExactlyEqual(double[,] expected, double[,] actual)
    {
        Assert.Equal(expected.GetLength(0), actual.GetLength(0));
        Assert.Equal(expected.GetLength(1), actual.GetLength(1));
        for (int i = 0; i < expected.GetLength(0); i++)
        {
            for (int j = 0; j < expected.GetLength(1); j++)
            {
                AssertDoubleExactlyEqual(expected[i, j], actual[i, j]);
            }
        }
    }

    private static void AssertVectorExactlyEqual(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertDoubleExactlyEqual(expected[i], actual[i]);
        }
    }

    private static void AssertDoubleExactlyEqual(double expected, double actual) =>
        Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));

    private static void AssertMatrixClose(string name, JsonElement[][] expected, double[,] actual)
    {
        Assert.Equal(expected.Length, actual.GetLength(0));
        Assert.Equal(expected[0].Length, actual.GetLength(1));
        for (int i = 0; i < expected.Length; i++)
        {
            for (int j = 0; j < expected[i].Length; j++)
            {
                AssertClose($"{name} plan[{i},{j}]", Decode(expected[i][j]), actual[i, j]);
            }
        }
    }

    private static void AssertVectorClose(string name, JsonElement[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertClose($"{name} scaling[{i}]", Decode(expected[i]), actual[i]);
        }
    }

    private static double Decode(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        return value.GetProperty("nonFinite").GetString() switch
        {
            "NaN" => double.NaN,
            "PositiveInfinity" => double.PositiveInfinity,
            "NegativeInfinity" => double.NegativeInfinity,
            string tag => throw new InvalidOperationException($"Unknown nonfinite tag '{tag}'."),
            null => throw new InvalidOperationException("The nonfinite tag was null."),
        };
    }

    private static void AssertClose(string name, double expected, double actual)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{name}: expected NaN, actual {actual:R}.");
            return;
        }

        if (double.IsInfinity(expected))
        {
            Assert.Equal(expected, actual);
            return;
        }

        double tolerance = 1e-12 + (1e-9 * Math.Abs(expected));
        Assert.True(
            Math.Abs(actual - expected) <= tolerance,
            $"{name}: expected {expected:R}, actual {actual:R}, tolerance {tolerance:R}.");
    }

    private sealed class Collector : ITraceObserver
    {
        public List<TraceFrame> Frames { get; } = [];

        public void Observe(TraceFrame frame) => Frames.Add(frame);
    }

    private sealed class MutatingObserver : ITraceObserver
    {
        public void Observe(TraceFrame frame)
        {
            Array.Fill(frame.SourceScaling, double.NaN);
            Array.Fill(frame.TargetScaling, double.NaN);
        }
    }

    private sealed class ThrowingObserver(Exception exception) : ITraceObserver
    {
        public void Observe(TraceFrame frame) => throw exception;
    }

    private sealed record PhaseFixtureRoot(
        PhaseFixtureProvenance Provenance,
        PhaseFixtureCase[] Cases);

    private sealed record PhaseFixtureProvenance(
        string PotVersion,
        string PotCommit,
        string SourceGitBlob);

    private sealed record PhaseFixtureCase(
        string Name,
        string Solver,
        double[] Source,
        double[] Target,
        double[][] Costs,
        double Regularization,
        double Threshold,
        int MaxIterations,
        bool ObservationParity,
        PhaseSnapshot[] Snapshots);

    private sealed record PhaseSnapshot(
        int Index,
        string Phase,
        bool Rejected,
        JsonElement[] SourceScaling,
        JsonElement[] TargetScaling,
        JsonElement[][] Plan);
}
