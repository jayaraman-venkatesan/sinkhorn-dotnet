namespace Sinkhorn.Tests;

using System.Text.Json;

public sealed class BasicSolverTests
{
    [Fact]
    public void BasicMatchesSymmetricReference()
    {
        var problem = new TransportProblem(
            [0.5, 0.5],
            [0.5, 0.5],
            new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

        SolverResult result = SinkhornSolver.Solve(
            problem,
            1.0,
            SolverKind.Basic,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.InRange(result.Plan[0, 0], 0.365529289314, 0.365529289317);
        Assert.Equal(0, result.LastAttemptedIndex);
        Assert.True(result.Checks.Usable);
    }

    [Fact]
    public void BasicReturnsRejectedZeroSupportResult()
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
        Assert.Equal(1, result.AttemptedPairs);
        Assert.Equal(0, result.AcceptedPairs);
        Assert.False(result.Checks.Usable);
    }

    [Fact]
    public void BasicMatchesPinnedPythonFixturesPerField()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "basic.json");
        FixtureRoot fixture = JsonSerializer.Deserialize<FixtureRoot>(
            File.ReadAllText(path),
            JsonOptions) ?? throw new InvalidOperationException("The Basic fixture was empty.");

        Assert.Equal("0.9.6.post1", fixture.Provenance.PotVersion);
        Assert.Equal("3.12.3", fixture.Provenance.PythonVersion);
        Assert.Equal("2.2.6", fixture.Provenance.NumpyVersion);
        Assert.Equal("1.15.3", fixture.Provenance.ScipyVersion);
        Assert.Equal("85113e9a380f5fcf684c50c73c1ff6a164a7366e", fixture.Provenance.PotCommit);
        Assert.Equal("cf5efadfc0f33300899d9b8a20f762e5f96a2759", fixture.Provenance.SourceGitBlob);
        Assert.Equal(
            "671958232afa87b8908d183c935475746f3d02c34827e78354d7505200e2dab2",
            fixture.Provenance.SourceSha256);
        Assert.Equal(64, fixture.Provenance.GeneratorSha256.Length);
        Assert.Equal(15, fixture.Cases.Length);

        foreach (FixtureCase item in fixture.Cases)
        {
            var problem = new TransportProblem(
                item.Source,
                item.Target,
                FixtureConversion.ToRectangular(item.Costs));
            var options = new SolverOptions(
                item.MaxIterations,
                item.Threshold,
                item.WarmStart is null
                    ? null
                    : new WarmStart(
                        item.WarmStart.SourceLogScaling,
                        item.WarmStart.TargetLogScaling));

            SolverResult actual = SinkhornSolver.Solve(
                problem,
                item.Regularization,
                SolverKind.Basic,
                options,
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(Enum.Parse<TerminationReason>(item.Expected.Termination), actual.Termination);
            Assert.Equal(item.Expected.LastAttemptedIndex, actual.LastAttemptedIndex);
            Assert.Equal(item.Expected.AttemptedPairs, actual.AttemptedPairs);
            Assert.Equal(item.Expected.AcceptedPairs, actual.AcceptedPairs);
            Assert.Equal(item.Expected.Warnings, actual.Warnings);
            Assert.Equal(item.Expected.Checks.Usable, actual.Checks.Usable);

            if (item.Extreme)
            {
                Assert.Equal(TerminationReason.NumericalBreakdown, actual.Termination);
                Assert.Equal(actual.AttemptedPairs - 1, actual.AcceptedPairs);
                continue;
            }

            Assert.Equal(item.Expected.Errors.Length, actual.Errors.Length);
            for (int i = 0; i < actual.Errors.Length; i++)
            {
                Assert.Equal(item.Expected.Errors[i].Index, actual.Errors[i].Index);
                AssertClose(item.Name, item.Expected.Errors[i].TargetL2, actual.Errors[i].TargetL2);
            }

            AssertMatrixClose(item.Name, item.Expected.Plan, actual.Plan);
            AssertVectorClose(item.Name, item.Expected.SourceScaling, actual.Scaling.Source);
            AssertVectorClose(item.Name, item.Expected.TargetScaling, actual.Scaling.Target);
            Assert.Equal(item.Expected.ScalingIsLog, actual.Scaling.IsLog);
            Assert.Equal(item.Expected.Checks.Finite, actual.Checks.Finite);
            Assert.Equal(item.Expected.Checks.Nonnegative, actual.Checks.Nonnegative);
            AssertClose(item.Name, item.Expected.Checks.SourceL1, actual.Checks.SourceL1);
            AssertClose(item.Name, item.Expected.Checks.TargetL1, actual.Checks.TargetL1);
            AssertClose(item.Name, item.Expected.Checks.TotalMass, actual.Checks.TotalMass);
            AssertClose(item.Name, item.Expected.TransportCost, actual.TransportCost);
        }
    }

    [Fact]
    public void DiagnosticOverflowDoesNotInvalidateFinitePlan()
    {
        var problem = new TransportProblem(
            [1.0, 1.0],
            [1.0, 1.0],
            new double[,]
            {
                { double.MaxValue, double.MaxValue },
                { double.MaxValue, double.MaxValue },
            });

        SolverResult result = SinkhornSolver.Solve(
            problem,
            double.MaxValue,
            SolverKind.Basic,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TerminationReason.ThresholdMet, result.Termination);
        Assert.True(result.Checks.Finite);
        Assert.True(result.Checks.Usable);
        Assert.Equal(double.PositiveInfinity, result.TransportCost);
        Assert.Equal(["diagnostic-overflow"], result.Warnings);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static void AssertMatrixClose(string name, double[][] expected, double[,] actual)
    {
        Assert.Equal(expected.Length, actual.GetLength(0));
        Assert.Equal(expected[0].Length, actual.GetLength(1));
        for (int i = 0; i < expected.Length; i++)
        {
            for (int j = 0; j < expected[i].Length; j++)
            {
                AssertClose($"{name} plan[{i},{j}]", expected[i][j], actual[i, j]);
            }
        }
    }

    private static void AssertVectorClose(string name, double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertClose($"{name} scaling[{i}]", expected[i], actual[i]);
        }
    }

    private static void AssertClose(string name, double expected, double actual)
    {
        double tolerance = 1e-12 + (1e-9 * Math.Abs(expected));
        Assert.True(
            Math.Abs(actual - expected) <= tolerance,
            $"{name}: expected {expected:R}, actual {actual:R}, tolerance {tolerance:R}.");
    }

    private sealed record FixtureRoot(FixtureProvenance Provenance, FixtureCase[] Cases);

    private sealed record FixtureProvenance(
        string PythonVersion,
        string PotVersion,
        string NumpyVersion,
        string ScipyVersion,
        string PotCommit,
        string SourceGitBlob,
        string SourceSha256,
        string GeneratorSha256);

    private sealed record FixtureCase(
        string Name,
        double[] Source,
        double[] Target,
        double[][] Costs,
        double Regularization,
        bool Extreme,
        double Threshold,
        int MaxIterations,
        FixtureWarmStart? WarmStart,
        FixtureExpected Expected);

    private sealed record FixtureWarmStart(
        double[] SourceLogScaling,
        double[] TargetLogScaling);

    private sealed record FixtureExpected(
        string Termination,
        int LastAttemptedIndex,
        int AttemptedPairs,
        int AcceptedPairs,
        FixtureError[] Errors,
        double[][] Plan,
        double[] SourceScaling,
        double[] TargetScaling,
        bool ScalingIsLog,
        FixtureChecks Checks,
        double TransportCost,
        string[] Warnings);

    private sealed record FixtureError(int Index, double TargetL2);

    private sealed record FixtureChecks(
        bool Finite,
        bool Nonnegative,
        double SourceL1,
        double TargetL1,
        double TotalMass,
        bool Usable);
}
