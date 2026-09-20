namespace Sinkhorn.Tests;

public sealed class AssessmentTests
{
    [Fact]
    public void ThresholdMetFeasiblePlanIsUsable()
    {
        var problem = new TransportProblem([1.0], [1.0], new double[,] { { 2.0 } });

        PlanChecks checks = PlanAssessment.Assess(
            new double[,] { { 1.0 } },
            problem,
            1e-9,
            TerminationReason.ThresholdMet);

        Assert.True(checks.Finite);
        Assert.True(checks.Nonnegative);
        Assert.Equal(0.0, checks.SourceL1);
        Assert.Equal(0.0, checks.TargetL1);
        Assert.Equal(1.0, checks.TotalMass);
        Assert.True(checks.Usable);
    }

    [Fact]
    public void IterationLimitedFeasiblePlanIsNotUsable()
    {
        var problem = new TransportProblem([1.0], [1.0], new double[,] { { 0.0 } });

        PlanChecks checks = PlanAssessment.Assess(
            new double[,] { { 1.0 } },
            problem,
            1e-9,
            TerminationReason.IterationLimit);

        Assert.False(checks.Usable);
    }

    [Fact]
    public void FiniteAndNonnegativeChecksAreIndependent()
    {
        var problem = new TransportProblem(
            [1.0],
            [0.5, 0.5],
            new double[,] { { 0.0, 0.0 } });

        PlanChecks positiveInfinity = PlanAssessment.Assess(
            new double[,] { { double.PositiveInfinity, 0.0 } },
            problem,
            1e-9,
            TerminationReason.ThresholdMet);
        PlanChecks finiteNegative = PlanAssessment.Assess(
            new double[,] { { 1.1, -0.1 } },
            problem,
            1e-9,
            TerminationReason.ThresholdMet);

        Assert.False(positiveInfinity.Finite);
        Assert.True(positiveInfinity.Nonnegative);
        Assert.False(positiveInfinity.Usable);
        Assert.True(finiteNegative.Finite);
        Assert.False(finiteNegative.Nonnegative);
        Assert.False(finiteNegative.Usable);
    }

    [Fact]
    public void ComputesRowsColumnsAndTotalIndependently()
    {
        var problem = new TransportProblem(
            [0.5, 0.5],
            [0.4, 0.6],
            new double[2, 2]);

        PlanChecks checks = PlanAssessment.Assess(
            new double[,] { { 0.3, 0.1 }, { 0.2, 0.4 } },
            problem,
            1.0,
            TerminationReason.ThresholdMet);

        Assert.Equal(0.2, checks.SourceL1, 12);
        Assert.Equal(0.2, checks.TargetL1, 12);
        Assert.Equal(1.0, checks.TotalMass, 12);
        Assert.True(checks.Usable);
    }

    [Fact]
    public void RejectsMismatchedPlanAndInvalidThreshold()
    {
        var problem = new TransportProblem([1.0], [1.0], new double[,] { { 0.0 } });

        Assert.Throws<ArgumentException>(() => PlanAssessment.Assess(
            new double[2, 1],
            problem,
            1e-9,
            TerminationReason.ThresholdMet));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlanAssessment.Assess(
            new double[1, 1],
            problem,
            0.0,
            TerminationReason.ThresholdMet));
    }
}
