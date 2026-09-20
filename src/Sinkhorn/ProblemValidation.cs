namespace Sinkhorn;

internal static class ProblemValidation
{
    private const double MassToleranceFactor = 1e-12;

    internal static TransportProblem Prepare(
        TransportProblem problem,
        double regularization,
        SolverOptions options)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(problem.Source);
        ArgumentNullException.ThrowIfNull(problem.Target);
        ArgumentNullException.ThrowIfNull(problem.Costs);

        int sourceCount = problem.Costs.GetLength(0);
        int targetCount = problem.Costs.GetLength(1);
        if (sourceCount <= 0 || targetCount <= 0)
        {
            throw new ArgumentException("The cost matrix must have positive dimensions.", nameof(problem));
        }

        if (problem.Source.Length != 0 && problem.Source.Length != sourceCount)
        {
            throw new ArgumentException("Source length must match the cost matrix row count.", nameof(problem));
        }

        if (problem.Target.Length != 0 && problem.Target.Length != targetCount)
        {
            throw new ArgumentException("Target length must match the cost matrix column count.", nameof(problem));
        }

        double[] source = problem.Source.Length == 0
            ? Uniform(sourceCount)
            : (double[])problem.Source.Clone();
        double[] target = problem.Target.Length == 0
            ? Uniform(targetCount)
            : (double[])problem.Target.Clone();
        double[,] costs = (double[,])problem.Costs.Clone();

        ValidateWeights(source, "Source");
        ValidateWeights(target, "Target");
        ValidateCosts(costs);

        double sourceTotal = Sum(source);
        double targetTotal = Sum(target);
        if (!double.IsFinite(sourceTotal) || sourceTotal <= 0.0)
        {
            throw new ArgumentException("Source weights must have a finite positive total.", nameof(problem));
        }

        if (!double.IsFinite(targetTotal) || targetTotal <= 0.0)
        {
            throw new ArgumentException("Target weights must have a finite positive total.", nameof(problem));
        }

        double massTolerance = MassToleranceFactor * Math.Max(sourceTotal, targetTotal);
        if (Math.Abs(sourceTotal - targetTotal) > massTolerance)
        {
            throw new ArgumentException("Source and target total mass must be compatible.", nameof(problem));
        }

        if (!double.IsFinite(regularization) || regularization <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(regularization),
                "Regularization must be finite and positive.");
        }

        if (!double.IsFinite(options.Threshold) || options.Threshold <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The stopping threshold must be finite and positive.");
        }

        if (options.MaxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The iteration budget must be positive.");
        }

        ValidateWarmStart(options.WarmStart, sourceCount, targetCount);

        return new TransportProblem(source, target, costs);
    }

    private static double[] Uniform(int count)
    {
        var values = new double[count];
        Array.Fill(values, 1.0 / count);
        return values;
    }

    private static void ValidateWeights(double[] values, string name)
    {
        foreach (double value in values)
        {
            if (!double.IsFinite(value) || value < 0.0)
            {
                throw new ArgumentException($"{name} weights must be finite and nonnegative.", "problem");
            }
        }
    }

    private static void ValidateCosts(double[,] costs)
    {
        foreach (double cost in costs)
        {
            if (!double.IsFinite(cost))
            {
                throw new ArgumentException("Costs must be finite.", "problem");
            }
        }
    }

    private static double Sum(double[] values)
    {
        double sum = 0.0;
        foreach (double value in values)
        {
            sum += value;
        }

        return sum;
    }

    private static void ValidateWarmStart(WarmStart? warmStart, int sourceCount, int targetCount)
    {
        if (warmStart is null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(warmStart.SourceLogScaling);
        ArgumentNullException.ThrowIfNull(warmStart.TargetLogScaling);

        if (warmStart.SourceLogScaling.Length != sourceCount)
        {
            throw new ArgumentException("Source warm-start length must match the source dimension.", "options");
        }

        if (warmStart.TargetLogScaling.Length != targetCount)
        {
            throw new ArgumentException("Target warm-start length must match the target dimension.", "options");
        }

        ValidateWarmStartValues(warmStart.SourceLogScaling, "Source");
        ValidateWarmStartValues(warmStart.TargetLogScaling, "Target");
    }

    private static void ValidateWarmStartValues(double[] values, string name)
    {
        foreach (double value in values)
        {
            if (double.IsNaN(value) || double.IsPositiveInfinity(value))
            {
                throw new ArgumentException(
                    $"{name} warm-start values cannot contain NaN or positive infinity.",
                    "options");
            }
        }
    }
}
