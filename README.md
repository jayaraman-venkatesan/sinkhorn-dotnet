# sinkhorn-dotnet

Status: in development. The Basic and LogDomain solvers, explicit result
assessment, public data contracts, input validation, and stable log-sum-exp
primitive are implemented.

Reusable C# Basic and LogDomain Sinkhorn library, with Python POT parity as an acceptance requirement.

- [Approved specification](docs/superpowers/specs/2026-09-19-sinkhorn-learning-design.md)
- [Implementation plans](docs/superpowers/plans/2026-09-19-sinkhorn-tickets.md)
- [Project issues](https://github.com/jayaraman-venkatesan/sinkhorn-dotnet/issues)

## Input rules

A transport problem contains source weights, target weights, and a dense cost
matrix. Cost rows correspond to sources and columns correspond to targets.

- The cost matrix must have at least one row and one column. Nonempty weight
  vectors must match the corresponding matrix dimension.
- An empty source or target vector is expanded to a uniform vector using the
  corresponding matrix dimension. This represents unit total mass; it is not a
  zero-mass input.
- Weights must be finite and nonnegative. Zero and fractional weights are kept
  exactly; support is not removed, rounded, normalized, or repaired.
- Costs must be finite but may be negative.
- Source and target totals must be finite, positive, and differ by no more than
  `1e-12 * max(sourceTotal, targetTotal)`. A discrepancy inside this tolerance
  is accepted without changing either total.
- Regularization and the stopping threshold must be finite and positive. The
  iteration budget must be positive.
- Warm-start vectors must match the source and target dimensions. They may
  contain finite values or negative infinity (zero scaling), but not NaN or
  positive infinity.

Validated problem arrays are copies, so later caller mutation does not affect
solver input. Absolute stopping thresholds retain the units and scale of the
supplied mass; the library does not silently normalize inputs.

## Solver example

```csharp
using Sinkhorn;

var problem = new TransportProblem(
    [0.5, 0.5],
    [0.5, 0.5],
    new double[,] { { 0.0, 1.0 }, { 1.0, 0.0 } });

foreach (SolverKind solver in new[] { SolverKind.Basic, SolverKind.LogDomain })
{
    SolverResult result = SinkhornSolver.Solve(problem, 1.0, solver);
    if (result.Checks.Usable)
    {
        Console.WriteLine($"{solver} transport cost: {result.TransportCost}");
        Console.WriteLine($"First route: {result.Plan[0, 0]}");
    }
    else
    {
        Console.WriteLine($"No usable {solver} plan: {result.Termination}");
    }
}
```

`Termination` reports whether the pinned stopping threshold was met, the
iteration budget was exhausted, or Basic encountered a numerical breakdown.
It is deliberately separate from `Checks.Usable`, which also requires a finite,
nonnegative plan and both final marginal L1 errors strictly below the requested
threshold. For example, a zero-support Basic run can break down on its first
update pair and return the restored initial diagnostic plan. That plan and its
cost remain available for inspection, but it is not a feasible result and must
not be used merely because its cost looks small.

Basic returns ordinary scaling values and rolls back a numerically invalid
update pair. LogDomain returns log scaling values (`Scaling.IsLog` is `true`),
uses log-sum-exp updates, and does not add a rollback absent from the pinned
source. The two coordinate arrays are therefore not directly comparable even
when their plans agree.

`Warnings` can include `diagnostic-overflow` when optional exponentiated log
scalings or summary arithmetic overflow. The returned LogDomain coordinates
remain logarithms and are never replaced by those exponentiated diagnostics.
Inspect `Checks` independently: diagnostic overflow by itself does not make a
finite, marginally feasible plan unusable.

## Phase observation and cancellation

Pass an `ITraceObserver` to `SinkhornSolver.Solve` to receive solver-coordinate
snapshots without changing the numerical calculation. `Initial` has index `-1`;
each attempted pair then emits `AfterDestination` followed by `AfterSource`.
Basic additionally emits `Restored` after rolling back a numerical breakdown.
Basic frames contain ordinary scalings, while LogDomain frames contain log
scalings.

Every frame owns copies of both scaling arrays. An observer may retain or mutate
those arrays without changing live solver state or later frames. Ordinary frames
have `Rejected == false`, which means they remain provisional until the next
pair is accepted or the solve returns successfully. A later `Restored` frame at
the same index identifies that pair's earlier update frames as rejected;
collectors that retain them must mark that relationship in their own view rather
than expecting old immutable snapshots to change.

Observer exceptions are caller exceptions and propagate from `Solve`; they are
not converted to numerical termination results. Cancellation is checked before
work starts and before every update pair. A requested cancellation throws
`OperationCanceledException`, including cancellation requested by an observer
between pairs, and is never reported as `ThresholdMet` or as a completed result.

## Development

The repository pins .NET SDK 10.0.201. Run the current verification with:

```sh
dotnet test
dotnet format --verify-no-changes
```

Development uses isolated feature worktrees and test-first tasks. Runnable
solver usage and container instructions will be added with their verified
implementation.

## Reference and attribution

The numerical implementation is being translated against Python Optimal
Transport (POT) 0.9.6.post1 at commit
`85113e9a380f5fcf684c50c73c1ff6a164a7366e`, specifically its single-target
`sinkhorn_knopp` and `sinkhorn_log` behavior. The project preserves POT's MIT
notice and source/file credits in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
This C# validation layer intentionally adds explicit argument checks described
above; those checks are not claimed as literal POT behavior.
