namespace Sinkhorn;

public sealed record PlanChecks(
    bool Finite,
    bool Nonnegative,
    double SourceL1,
    double TargetL1,
    double TotalMass,
    bool Usable);
