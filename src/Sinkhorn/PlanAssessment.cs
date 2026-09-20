namespace Sinkhorn;

public static class PlanAssessment
{
    public static PlanChecks Assess(
        double[,] plan,
        TransportProblem problem,
        double threshold,
        TerminationReason termination)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(problem.Source);
        ArgumentNullException.ThrowIfNull(problem.Target);

        if (plan.GetLength(0) != problem.Source.Length
            || plan.GetLength(1) != problem.Target.Length)
        {
            throw new ArgumentException("Plan dimensions must match the transport problem.", nameof(plan));
        }

        if (!double.IsFinite(threshold) || threshold <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                "The assessment threshold must be finite and positive.");
        }

        bool finite = true;
        bool nonnegative = true;
        double totalMass = 0.0;
        double sourceL1 = 0.0;
        var targetTotals = new double[problem.Target.Length];

        for (int i = 0; i < problem.Source.Length; i++)
        {
            double rowTotal = 0.0;
            for (int j = 0; j < problem.Target.Length; j++)
            {
                double value = plan[i, j];
                finite &= double.IsFinite(value);
                nonnegative &= value >= 0.0;
                rowTotal += value;
                targetTotals[j] += value;
                totalMass += value;
            }

            sourceL1 += Math.Abs(rowTotal - problem.Source[i]);
        }

        double targetL1 = 0.0;
        for (int j = 0; j < problem.Target.Length; j++)
        {
            targetL1 += Math.Abs(targetTotals[j] - problem.Target[j]);
        }

        bool usable = termination == TerminationReason.ThresholdMet
            && finite
            && nonnegative
            && sourceL1 < threshold
            && targetL1 < threshold;
        return new PlanChecks(finite, nonnegative, sourceL1, targetL1, totalMass, usable);
    }
}
