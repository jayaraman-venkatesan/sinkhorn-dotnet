namespace Sinkhorn;

internal static class LogDomainSolver
{
    internal static SolverResult Solve(
        TransportProblem problem,
        double regularization,
        SolverOptions options,
        CancellationToken cancellationToken)
    {
        int sourceCount = problem.Source.Length;
        int targetCount = problem.Target.Length;
        var scaledCost = new double[sourceCount, targetCount];
        for (int i = 0; i < sourceCount; i++)
        {
            for (int j = 0; j < targetCount; j++)
            {
                scaledCost[i, j] = -problem.Costs[i, j] / regularization;
            }
        }

        double[] sourceLogScaling = InitializeScaling(
            sourceCount,
            options.WarmStart?.SourceLogScaling);
        double[] targetLogScaling = InitializeScaling(
            targetCount,
            options.WarmStart?.TargetLogScaling);
        double[] logSource = Log(problem.Source);
        double[] logTarget = Log(problem.Target);
        var sourceTerms = new double[sourceCount];
        var targetTerms = new double[targetCount];
        var errors = new List<ErrorSample>();
        TerminationReason termination = TerminationReason.IterationLimit;
        int lastAttemptedIndex = -1;
        int attemptedPairs = 0;

        for (int index = 0; index < options.MaxIterations; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastAttemptedIndex = index;
            attemptedPairs++;

            for (int j = 0; j < targetCount; j++)
            {
                for (int i = 0; i < sourceCount; i++)
                {
                    sourceTerms[i] = scaledCost[i, j] + sourceLogScaling[i];
                }

                targetLogScaling[j] = logTarget[j] - Numerics.LogSumExp(sourceTerms);
            }

            for (int i = 0; i < sourceCount; i++)
            {
                for (int j = 0; j < targetCount; j++)
                {
                    targetTerms[j] = scaledCost[i, j] + targetLogScaling[j];
                }

                sourceLogScaling[i] = logSource[i] - Numerics.LogSumExp(targetTerms);
            }

            if (index % 10 == 0)
            {
                double targetL2 = TargetL2(
                    scaledCost,
                    sourceLogScaling,
                    targetLogScaling,
                    problem.Target);
                errors.Add(new ErrorSample(index, targetL2));
                if (targetL2 < options.Threshold)
                {
                    termination = TerminationReason.ThresholdMet;
                    break;
                }
            }
        }

        double[,] plan = Materialize(scaledCost, sourceLogScaling, targetLogScaling);
        PlanChecks checks = PlanAssessment.Assess(
            plan,
            problem,
            options.Threshold,
            termination);
        double transportCost = TransportCost(plan, problem.Costs);
        string[] warnings = Warnings(
            termination,
            checks,
            transportCost,
            errors,
            sourceLogScaling,
            targetLogScaling);

        return new SolverResult(
            SolverKind.LogDomain,
            plan,
            termination,
            lastAttemptedIndex,
            attemptedPairs,
            attemptedPairs,
            [.. errors],
            new ScalingDiagnostics(
                (double[])sourceLogScaling.Clone(),
                (double[])targetLogScaling.Clone(),
                true),
            checks,
            transportCost,
            warnings);
    }

    private static double[] InitializeScaling(int count, double[]? warmStart)
    {
        return warmStart is null ? new double[count] : (double[])warmStart.Clone();
    }

    private static double[] Log(double[] values)
    {
        var result = new double[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            result[i] = Math.Log(values[i]);
        }

        return result;
    }

    private static double TargetL2(
        double[,] scaledCost,
        double[] sourceLogScaling,
        double[] targetLogScaling,
        double[] target)
    {
        double squared = 0.0;
        for (int j = 0; j < target.Length; j++)
        {
            double actual = 0.0;
            for (int i = 0; i < sourceLogScaling.Length; i++)
            {
                actual += Math.Exp(
                    scaledCost[i, j]
                    + sourceLogScaling[i]
                    + targetLogScaling[j]);
            }

            double difference = actual - target[j];
            squared += difference * difference;
        }

        return Math.Sqrt(squared);
    }

    private static double[,] Materialize(
        double[,] scaledCost,
        double[] sourceLogScaling,
        double[] targetLogScaling)
    {
        var plan = new double[sourceLogScaling.Length, targetLogScaling.Length];
        for (int i = 0; i < sourceLogScaling.Length; i++)
        {
            for (int j = 0; j < targetLogScaling.Length; j++)
            {
                plan[i, j] = Math.Exp(
                    scaledCost[i, j]
                    + sourceLogScaling[i]
                    + targetLogScaling[j]);
            }
        }

        return plan;
    }

    private static double TransportCost(double[,] plan, double[,] costs)
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

    private static string[] Warnings(
        TerminationReason termination,
        PlanChecks checks,
        double transportCost,
        List<ErrorSample> errors,
        double[] sourceLogScaling,
        double[] targetLogScaling)
    {
        var warnings = new List<string>();
        if (termination == TerminationReason.IterationLimit)
        {
            warnings.Add("iteration-limit");
        }

        if (!checks.Finite)
        {
            warnings.Add("nonfinite-plan");
        }

        if (HasExponentiationOverflow(sourceLogScaling)
            || HasExponentiationOverflow(targetLogScaling)
            || !double.IsFinite(checks.SourceL1)
            || !double.IsFinite(checks.TargetL1)
            || !double.IsFinite(checks.TotalMass)
            || !double.IsFinite(transportCost)
            || errors.Exists(error => !double.IsFinite(error.TargetL2)))
        {
            warnings.Add("diagnostic-overflow");
        }

        return [.. warnings];
    }

    private static bool HasExponentiationOverflow(double[] logScaling)
    {
        foreach (double value in logScaling)
        {
            if (!double.IsFinite(Math.Exp(value)))
            {
                return true;
            }
        }

        return false;
    }
}
