namespace Sinkhorn;

internal static class Numerics
{
    internal static double TransportCost(double[,] plan, double[,] costs)
    {
        double total = 0.0;
        for (int i = 0; i < plan.GetLength(0); i++)
        {
            for (int j = 0; j < plan.GetLength(1); j++)
            {
                total += plan[i, j] * costs[i, j];
            }
        }

        return total;
    }

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
