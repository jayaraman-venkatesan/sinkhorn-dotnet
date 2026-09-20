namespace Sinkhorn;

internal static class BasicSolver
{
    internal static SolverResult Solve(
        TransportProblem problem,
        double regularization,
        SolverOptions options,
        ITraceObserver? observer,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int sourceCount = problem.Source.Length;
        int targetCount = problem.Target.Length;
        var kernel = new double[sourceCount, targetCount];
        var scaledKernel = new double[sourceCount, targetCount];
        for (int i = 0; i < sourceCount; i++)
        {
            double inverseSource = 1.0 / problem.Source[i];
            for (int j = 0; j < targetCount; j++)
            {
                kernel[i, j] = Math.Exp(problem.Costs[i, j] / -regularization);
                scaledKernel[i, j] = inverseSource * kernel[i, j];
            }
        }

        double[] sourceScaling = InitializeScaling(
            sourceCount,
            options.WarmStart?.SourceLogScaling);
        double[] targetScaling = InitializeScaling(
            targetCount,
            options.WarmStart?.TargetLogScaling);
        var previousSourceScaling = new double[sourceCount];
        var previousTargetScaling = new double[targetCount];
        var denominators = new double[targetCount];
        var errors = new List<ErrorSample>();
        TerminationReason termination = TerminationReason.IterationLimit;
        int lastAttemptedIndex = -1;
        int attemptedPairs = 0;
        int acceptedPairs = 0;

        Observe(
            observer,
            -1,
            TracePhase.Initial,
            sourceScaling,
            targetScaling);

        for (int index = 0; index < options.MaxIterations; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastAttemptedIndex = index;
            attemptedPairs++;
            Array.Copy(sourceScaling, previousSourceScaling, sourceCount);
            Array.Copy(targetScaling, previousTargetScaling, targetCount);

            for (int j = 0; j < targetCount; j++)
            {
                denominators[j] = 0.0;
                for (int i = 0; i < sourceCount; i++)
                {
                    denominators[j] += kernel[i, j] * sourceScaling[i];
                }

                targetScaling[j] = problem.Target[j] / denominators[j];
            }

            Observe(
                observer,
                index,
                TracePhase.AfterDestination,
                sourceScaling,
                targetScaling);

            for (int i = 0; i < sourceCount; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < targetCount; j++)
                {
                    sum += scaledKernel[i, j] * targetScaling[j];
                }

                sourceScaling[i] = 1.0 / sum;
            }

            Observe(
                observer,
                index,
                TracePhase.AfterSource,
                sourceScaling,
                targetScaling);

            if (HasNumericalBreakdown(denominators, sourceScaling, targetScaling))
            {
                Array.Copy(previousSourceScaling, sourceScaling, sourceCount);
                Array.Copy(previousTargetScaling, targetScaling, targetCount);
                Observe(
                    observer,
                    index,
                    TracePhase.Restored,
                    sourceScaling,
                    targetScaling);
                cancellationToken.ThrowIfCancellationRequested();
                termination = TerminationReason.NumericalBreakdown;
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            acceptedPairs++;
            if (index % 10 == 0)
            {
                double targetL2 = TargetL2(
                    kernel,
                    sourceScaling,
                    targetScaling,
                    problem.Target);
                errors.Add(new ErrorSample(index, targetL2));
                if (targetL2 < options.Threshold)
                {
                    termination = TerminationReason.ThresholdMet;
                    break;
                }
            }
        }

        double[,] plan = Materialize(kernel, sourceScaling, targetScaling);
        PlanChecks checks = PlanAssessment.Assess(
            plan,
            problem,
            options.Threshold,
            termination);
        double transportCost = Numerics.TransportCost(plan, problem.Costs);
        string[] warnings = Warnings(termination, checks, transportCost, errors);

        return new SolverResult(
            SolverKind.Basic,
            plan,
            termination,
            lastAttemptedIndex,
            attemptedPairs,
            acceptedPairs,
            [.. errors],
            new ScalingDiagnostics(
                (double[])sourceScaling.Clone(),
                (double[])targetScaling.Clone(),
                false),
            checks,
            transportCost,
            warnings);
    }

    private static void Observe(
        ITraceObserver? observer,
        int index,
        TracePhase phase,
        double[] sourceScaling,
        double[] targetScaling)
    {
        observer?.Observe(
            new TraceFrame(
                index,
                phase,
                SolverKind.Basic,
                (double[])sourceScaling.Clone(),
                (double[])targetScaling.Clone(),
                false));
    }

    private static double[] InitializeScaling(int count, double[]? logScaling)
    {
        var scaling = new double[count];
        if (logScaling is null)
        {
            Array.Fill(scaling, 1.0 / count);
            return scaling;
        }

        for (int i = 0; i < count; i++)
        {
            scaling[i] = Math.Exp(logScaling[i]);
        }

        return scaling;
    }

    private static bool HasNumericalBreakdown(
        double[] denominators,
        double[] sourceScaling,
        double[] targetScaling)
    {
        foreach (double denominator in denominators)
        {
            if (denominator == 0.0)
            {
                return true;
            }
        }

        return HasNonfinite(sourceScaling) || HasNonfinite(targetScaling);
    }

    private static bool HasNonfinite(double[] values)
    {
        foreach (double value in values)
        {
            if (!double.IsFinite(value))
            {
                return true;
            }
        }

        return false;
    }

    private static double TargetL2(
        double[,] kernel,
        double[] sourceScaling,
        double[] targetScaling,
        double[] target)
    {
        double squared = 0.0;
        for (int j = 0; j < target.Length; j++)
        {
            double actual = 0.0;
            for (int i = 0; i < sourceScaling.Length; i++)
            {
                actual += (sourceScaling[i] * kernel[i, j]) * targetScaling[j];
            }

            double difference = actual - target[j];
            squared += difference * difference;
        }

        return Math.Sqrt(squared);
    }

    private static double[,] Materialize(
        double[,] kernel,
        double[] sourceScaling,
        double[] targetScaling)
    {
        var plan = new double[sourceScaling.Length, targetScaling.Length];
        for (int i = 0; i < sourceScaling.Length; i++)
        {
            for (int j = 0; j < targetScaling.Length; j++)
            {
                plan[i, j] = (sourceScaling[i] * kernel[i, j]) * targetScaling[j];
            }
        }

        return plan;
    }

    private static string[] Warnings(
        TerminationReason termination,
        PlanChecks checks,
        double transportCost,
        List<ErrorSample> errors)
    {
        var warnings = new List<string>();
        if (termination == TerminationReason.IterationLimit)
        {
            warnings.Add("iteration-limit");
        }
        else if (termination == TerminationReason.NumericalBreakdown)
        {
            warnings.Add("numerical-breakdown");
        }

        if (!checks.Finite)
        {
            warnings.Add("nonfinite-plan");
        }

        if (checks.Finite
            && (!double.IsFinite(checks.SourceL1)
                || !double.IsFinite(checks.TargetL1)
                || !double.IsFinite(checks.TotalMass)))
        {
            warnings.Add("diagnostic-overflow");
        }
        else if (!double.IsFinite(transportCost)
            || errors.Exists(error => !double.IsFinite(error.TargetL2)))
        {
            warnings.Add("diagnostic-overflow");
        }

        return [.. warnings];
    }
}
