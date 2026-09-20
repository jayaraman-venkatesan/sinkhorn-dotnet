namespace Sinkhorn;

public static class SinkhornSolver
{
    public static SolverResult Solve(
        TransportProblem problem,
        double regularization,
        SolverKind solver,
        SolverOptions? options = null,
        ITraceObserver? observer = null,
        CancellationToken cancellationToken = default)
    {
        SolverOptions effectiveOptions = options ?? new SolverOptions();
        TransportProblem prepared = ProblemValidation.Prepare(
            problem,
            regularization,
            effectiveOptions);

        return solver switch
        {
            SolverKind.Basic => BasicSolver.Solve(
                prepared,
                regularization,
                effectiveOptions,
                cancellationToken),
            SolverKind.LogDomain => throw new NotSupportedException(
                "The LogDomain solver is not implemented yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(solver), solver, "Unknown solver kind."),
        };
    }
}
