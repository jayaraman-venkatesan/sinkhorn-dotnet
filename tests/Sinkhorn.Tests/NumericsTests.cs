namespace Sinkhorn.Tests;

public sealed class NumericsTests
{
    [Fact]
    public void LogSumExpPreservesLargeFiniteValues()
    {
        Assert.InRange(
            Numerics.LogSumExp([1000.0, 1000.0]),
            1000.0 + Math.Log(2.0) - 1e-12,
            1000.0 + Math.Log(2.0) + 1e-12);
        Assert.Equal(
            double.NegativeInfinity,
            Numerics.LogSumExp([double.NegativeInfinity, double.NegativeInfinity]));
    }

    [Fact]
    public void LogSumExpPropagatesNaN()
    {
        Assert.True(double.IsNaN(Numerics.LogSumExp([0.0, double.NaN])));
    }

    [Fact]
    public void LogSumExpPreservesPositiveInfinity()
    {
        Assert.Equal(double.PositiveInfinity, Numerics.LogSumExp([0.0, double.PositiveInfinity]));
    }

    [Fact]
    public void LogSumExpOfEmptyInputIsNegativeInfinity()
    {
        Assert.Equal(double.NegativeInfinity, Numerics.LogSumExp([]));
    }
}
