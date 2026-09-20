namespace Sinkhorn;

internal static class Numerics
{
    internal static double LogSumExp(ReadOnlySpan<double> values)
    {
        double max = double.NegativeInfinity;
        foreach (double value in values)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            max = Math.Max(max, value);
        }

        if (double.IsInfinity(max))
        {
            return max;
        }

        double sum = 0.0;
        foreach (double value in values)
        {
            sum += Math.Exp(value - max);
        }

        return max + Math.Log(sum);
    }
}
