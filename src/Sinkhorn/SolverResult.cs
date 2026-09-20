namespace Sinkhorn;

public sealed record SolverResult(
    SolverKind Solver,
    double[,] Plan,
    TerminationReason Termination,
    int LastAttemptedIndex,
    int AttemptedPairs,
    int AcceptedPairs,
    ErrorSample[] Errors,
    ScalingDiagnostics Scaling,
    PlanChecks Checks,
    double TransportCost,
    string[] Warnings,
    SolverMetadata Metadata);
