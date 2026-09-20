namespace Sinkhorn;

public sealed record TraceFrame(
    int Index,
    TracePhase Phase,
    SolverKind Solver,
    double[] SourceScaling,
    double[] TargetScaling,
    bool Rejected);
