using Sinkhorn;

var ordinary = new TransportProblem(
    [0.5, 0.5],
    [0.5, 0.5],
    new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

foreach (SolverKind kind in new[] { SolverKind.Basic, SolverKind.LogDomain })
{
    SolverResult result = SinkhornSolver.Solve(ordinary, 1.0, kind);
    if (!result.Checks.Usable)
    {
        Console.Error.WriteLine(
            $"{kind}: unusable ({result.Termination}; " +
            $"source L1={result.Checks.SourceL1:R}; target L1={result.Checks.TargetL1:R})");
        return 1;
    }

    Console.WriteLine($"{kind}: usable");
}

var zeroSupport = new TransportProblem(
    [1.0, 0.0],
    [0.0, 1.0],
    new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });
SolverResult failed = SinkhornSolver.Solve(zeroSupport, 1.0, SolverKind.Basic);
Console.WriteLine($"Basic zero support: {failed.Termination}");

return failed.Termination == TerminationReason.NumericalBreakdown
    && !failed.Checks.Usable
    ? 0
    : 1;
