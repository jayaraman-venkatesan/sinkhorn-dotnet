namespace Sinkhorn.Tests;

public sealed class ValidationTests
{
    private static readonly SolverOptions ValidOptions = new();

    [Fact]
    public void RejectsNullProblemAndOptions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProblemValidation.Prepare(null!, 1.0, ValidOptions));
        Assert.Throws<ArgumentNullException>(() =>
            ProblemValidation.Prepare(ValidProblem(), 1.0, null!));
    }

    [Fact]
    public void RejectsNullProblemArrays()
    {
        Assert.Throws<ArgumentNullException>(() => Prepare(new(null!, [1.0], new double[,] { { 0.0 } })));
        Assert.Throws<ArgumentNullException>(() => Prepare(new([1.0], null!, new double[,] { { 0.0 } })));
        Assert.Throws<ArgumentNullException>(() => Prepare(new([1.0], [1.0], null!)));
    }

    [Fact]
    public void RejectsZeroSizedCostDimensions()
    {
        Assert.Throws<ArgumentException>(() => Prepare(new([], [1.0], new double[0, 1])));
        Assert.Throws<ArgumentException>(() => Prepare(new([1.0], [], new double[1, 0])));
    }

    [Fact]
    public void RejectsNonemptyWeightsWithWrongLengths()
    {
        Assert.Throws<ArgumentException>(() => Prepare(new([0.5, 0.5], [1.0], new double[1, 1])));
        Assert.Throws<ArgumentException>(() => Prepare(new([1.0], [0.5, 0.5], new double[1, 1])));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsInvalidSourceWeights(double value)
    {
        Assert.Throws<ArgumentException>(() => Prepare(new([value], [1.0], new double[1, 1])));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsInvalidTargetWeights(double value)
    {
        Assert.Throws<ArgumentException>(() => Prepare(new([1.0], [value], new double[1, 1])));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsNonfiniteCosts(double value)
    {
        Assert.Throws<ArgumentException>(() => Prepare(new([1.0], [1.0], new double[,] { { value } })));
    }

    [Fact]
    public void RejectsNonpositiveAndOverflowedTotals()
    {
        Assert.Throws<ArgumentException>(() => Prepare(new([0.0], [0.0], new double[1, 1])));
        Assert.Throws<ArgumentException>(() => Prepare(new([double.MaxValue, double.MaxValue], [1.0], new double[2, 1])));
        Assert.Throws<ArgumentException>(() => Prepare(new([1.0], [double.MaxValue, double.MaxValue], new double[1, 2])));
    }

    [Fact]
    public void RejectsUnequalMass()
    {
        var problem = new TransportProblem([1.0], [2.0], new double[,] { { 0.0 } });

        Assert.Throws<ArgumentException>(() =>
            ProblemValidation.Prepare(problem, 1.0, new SolverOptions()));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsInvalidRegularization(double regularization)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProblemValidation.Prepare(ValidProblem(), regularization, ValidOptions));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsInvalidThreshold(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProblemValidation.Prepare(ValidProblem(), 1.0, new SolverOptions(Threshold: threshold)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonpositiveIterationBudget(int budget)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProblemValidation.Prepare(ValidProblem(), 1.0, new SolverOptions(MaxIterations: budget)));
    }

    [Fact]
    public void RejectsWarmStartsWithWrongLengths()
    {
        Assert.Throws<ArgumentException>(() => Prepare(
            ValidProblem(), new(WarmStart: new([], [0.0]))));
        Assert.Throws<ArgumentException>(() => Prepare(
            ValidProblem(), new(WarmStart: new([0.0], []))));
    }

    [Fact]
    public void RejectsNullWarmStartArrays()
    {
        Assert.Throws<ArgumentNullException>(() => Prepare(
            ValidProblem(), new(WarmStart: new(null!, [0.0]))));
        Assert.Throws<ArgumentNullException>(() => Prepare(
            ValidProblem(), new(WarmStart: new([0.0], null!))));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsInvalidSourceWarmStarts(double value)
    {
        Assert.Throws<ArgumentException>(() => Prepare(
            ValidProblem(), new(WarmStart: new([value], [0.0]))));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsInvalidTargetWarmStarts(double value)
    {
        Assert.Throws<ArgumentException>(() => Prepare(
            ValidProblem(), new(WarmStart: new([0.0], [value]))));
    }

    [Fact]
    public void ExpandsEmptyWeightsToUniformVectors()
    {
        var prepared = Prepare(new([], [], new double[2, 4]));

        Assert.Equal([0.5, 0.5], prepared.Source);
        Assert.Equal([0.25, 0.25, 0.25, 0.25], prepared.Target);
    }

    [Fact]
    public void PreservesZeroSupportEqualNonunitMassAndNegativeCosts()
    {
        var prepared = Prepare(new(
            [100.0, 0.0],
            [0.0, 100.0],
            new double[,] { { -2.0, 1.0 }, { 3.0, -4.0 } }));

        Assert.Equal([100.0, 0.0], prepared.Source);
        Assert.Equal([0.0, 100.0], prepared.Target);
        Assert.Equal(-2.0, prepared.Costs[0, 0]);
        Assert.Equal(-4.0, prepared.Costs[1, 1]);
    }

    [Fact]
    public void AcceptsNegativeInfinityWarmStarts()
    {
        var prepared = Prepare(
            ValidProblem(),
            new(WarmStart: new([double.NegativeInfinity], [double.NegativeInfinity])));

        Assert.Equal([1.0], prepared.Source);
    }

    [Fact]
    public void AppliesRelativeMassToleranceWithoutRepairingAcceptedMass()
    {
        double acceptedTarget = 100.0 + 5e-11;
        double rejectedTarget = 100.0 + 2e-10;

        var prepared = Prepare(new([100.0], [acceptedTarget], new double[1, 1]));

        Assert.Equal(100.0, prepared.Source[0]);
        Assert.Equal(acceptedTarget, prepared.Target[0]);
        Assert.Throws<ArgumentException>(() =>
            Prepare(new([100.0], [rejectedTarget], new double[1, 1])));
    }

    [Fact]
    public void AppliesMassToleranceAtAdjacentRepresentableBoundaryValues()
    {
        const double toleranceFactor = 1e-12;
        const double sourceTotal = 100.0;
        double roundedBoundary = sourceTotal / (1.0 - toleranceFactor);
        double acceptedTarget = Math.BitDecrement(roundedBoundary);
        double rejectedTarget = Math.BitIncrement(acceptedTarget);

        Assert.Equal(roundedBoundary, rejectedTarget);
        Assert.True(
            Math.Abs(sourceTotal - acceptedTarget)
            <= toleranceFactor * Math.Max(sourceTotal, acceptedTarget));
        Assert.True(
            Math.Abs(sourceTotal - rejectedTarget)
            > toleranceFactor * Math.Max(sourceTotal, rejectedTarget));

        var prepared = Prepare(new([sourceTotal], [acceptedTarget], new double[1, 1]));

        Assert.Equal([sourceTotal], prepared.Source);
        Assert.Equal([acceptedTarget], prepared.Target);
        Assert.Throws<ArgumentException>(() =>
            Prepare(new([sourceTotal], [rejectedTarget], new double[1, 1])));
    }

    [Fact]
    public void ClonesAllReturnedProblemArrays()
    {
        double[] source = [1.0];
        double[] target = [1.0];
        double[,] costs = { { 2.0 } };
        var prepared = Prepare(new(source, target, costs));

        source[0] = 9.0;
        target[0] = 9.0;
        costs[0, 0] = 9.0;

        Assert.Equal(1.0, prepared.Source[0]);
        Assert.Equal(1.0, prepared.Target[0]);
        Assert.Equal(2.0, prepared.Costs[0, 0]);
        Assert.NotSame(source, prepared.Source);
        Assert.NotSame(target, prepared.Target);
        Assert.NotSame(costs, prepared.Costs);
    }

    private static TransportProblem ValidProblem() =>
        new([1.0], [1.0], new double[,] { { 0.0 } });

    private static TransportProblem Prepare(TransportProblem problem, SolverOptions? options = null) =>
        ProblemValidation.Prepare(problem, 1.0, options ?? ValidOptions);
}
