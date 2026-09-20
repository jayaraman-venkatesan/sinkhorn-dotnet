namespace Sinkhorn;

public sealed record SolverOptions(
    int MaxIterations = 1000,
    double Threshold = 1e-9,
    WarmStart? WarmStart = null);
