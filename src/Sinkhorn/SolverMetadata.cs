namespace Sinkhorn;

public sealed record SolverMetadata(
    SolverKind Solver,
    string ReferenceVersion,
    string ReferenceCommit,
    double Regularization,
    SolverOptions EffectiveOptions,
    string PreprocessingPolicy,
    double SourceTotal,
    double TargetTotal)
{
    internal static SolverMetadata Create(
        SolverKind solver,
        TransportProblem preparedProblem,
        double regularization,
        SolverOptions effectiveOptions)
    {
        WarmStart? warmStart = effectiveOptions.WarmStart is null
            ? null
            : new WarmStart(
                (double[])effectiveOptions.WarmStart.SourceLogScaling.Clone(),
                (double[])effectiveOptions.WarmStart.TargetLogScaling.Clone());
        var optionsSnapshot = new SolverOptions(
            effectiveOptions.MaxIterations,
            effectiveOptions.Threshold,
            warmStart);

        return new SolverMetadata(
            solver,
            "0.9.6.post1",
            "85113e9a380f5fcf684c50c73c1ff6a164a7366e",
            regularization,
            optionsSnapshot,
            "none",
            Sum(preparedProblem.Source),
            Sum(preparedProblem.Target));
    }

    private static double Sum(double[] values)
    {
        double total = 0.0;
        foreach (double value in values)
        {
            total += value;
        }

        return total;
    }
}
