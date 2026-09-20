# sinkhorn-dotnet

Status: in development. The independently runnable implementation and its local
verification evidence are awaiting repository review and an approved merge. No
NuGet package or container image has been published.

Reusable C# Basic and LogDomain Sinkhorn library, with Python POT parity as an acceptance requirement.

- [Approved specification](docs/superpowers/specs/2026-09-19-sinkhorn-learning-design.md)
- [Implementation plans](docs/superpowers/plans/2026-09-19-sinkhorn-tickets.md)
- [Project issues](https://github.com/jayaraman-venkatesan/sinkhorn-dotnet/issues)

## Architecture

`SinkhornSolver.Solve` is the stable public entry point. It sends every call
through a validation boundary that checks the problem and options and copies
caller-owned arrays. The entry point then dispatches to separate Basic or
LogDomain numerical cores; neither core silently substitutes the other.

Calculation and assessment remain distinct: the selected core preserves its
reference update and stopping behavior, while `PlanAssessment` independently
computes finite/nonnegative and marginal-error checks for the returned
`SolverResult`. Optional trace observation is another copied boundary: each
`TraceFrame` owns scaling-array copies, so an observer cannot mutate live solver
state. The console example consumes this same public API, and the multistage
container publishes that example and runs it on the .NET runtime image—there is
no second solver or service implementation in either path.

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

Run the checked-in console example with the pinned .NET 10 SDK:

```sh
dotnet run --project examples/Usage -c Release
```

The example requires both ordinary solvers to return usable plans and requires
the intentional Basic zero-support case to report `NumericalBreakdown`. It exits
nonzero if any of those expectations changes. The same acceptance is available
as `tests/usage-smoke.sh`.

Library consumers should follow the same inspect-before-use pattern:

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
work starts, before every update pair, and after every completed pair before
termination or finalization. Basic performs and reports a required restoration
before that completed-pair check. A requested cancellation throws
`OperationCanceledException`, including cancellation requested by an observer,
and is never reported as `ThresholdMet` or as a completed result.

## Development

The repository pins .NET SDK 10.0.201 and uses NuGet lock files. Restore and run
the local verification with:

```sh
dotnet restore --locked-mode
dotnet test -c Release
dotnet format --verify-no-changes
dotnet build -c Release
tests/usage-smoke.sh
```

The reference fixtures can be regenerated only with the pinned Python
environment:

```sh
python3 -m venv .venv
.venv/bin/pip install -r reference/requirements.txt
.venv/bin/python reference/generate_fixtures.py
git diff --exit-code -- tests/Sinkhorn.Tests/Fixtures
```

The generator checks POT, NumPy, SciPy, the pinned upstream source Git blob, and
its own provenance before writing fixtures. Generated files are acceptance
oracles; review any diff rather than updating it mechanically.

## Container usage

The multistage image publishes and runs the same console example. Both Microsoft
.NET base image versions and multi-architecture manifest digests are pinned in
the `Dockerfile`.

```sh
docker build -t sinkhorn-library-check .
docker run --rm sinkhorn-library-check
```

The first build requires network access to the pinned Microsoft images and
NuGet source unless they are already cached. The repository does not push this
image to a registry. See [verification evidence](docs/verification.md) for the
architectures actually exercised; a manifest advertising an architecture is
not treated as runtime evidence.

## Precision and scope limitations

- The first release is a dense, single-problem, CPU, double-precision library.
  It does not provide GPU, autodifferentiation, sparse, batching, unbalanced-OT,
  cost-generation, or whole-POT APIs.
- Reference compatibility is tolerance based, not bitwise. Ordinary fixture
  comparisons use `1e-12 + 1e-9 * abs(reference)` for entries and costs, with
  marginal L1 error below `1e-8` in normalized cases.
- Stopping thresholds and residuals are absolute in the supplied mass units.
  Non-unit mass is preserved, and arbitrary magnitude stability is not promised.
- Neither solver is guaranteed to converge within its budget. Basic can
  underflow or break down and roll back; LogDomain avoids materializing the
  direct kernel but can still exhaust or produce nonfinite diagnostics.
- `TransportCost` is not the regularized objective, exact Wasserstein distance,
  or Sinkhorn divergence. An unusable plan remains diagnostic output only.
- There is no silent normalization, zero-support removal, solver fallback, or
  retry. Cancellation throws rather than producing a completed result.

## License and dependency audit

This project is MIT licensed. POT provenance and its complete MIT notice,
locked development/test dependencies, pinned container bases, and CI-only
actions are recorded in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
The class library and console example have no third-party NuGet runtime package
dependencies.

## Reference and attribution

The numerical implementation is being translated against Python Optimal
Transport (POT) 0.9.6.post1 at commit
`85113e9a380f5fcf684c50c73c1ff6a164a7366e`, specifically its single-target
`sinkhorn_knopp` and `sinkhorn_log` behavior. The project preserves POT's MIT
notice and source/file credits in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
This C# validation layer intentionally adds explicit argument checks described
above; those checks are not claimed as literal POT behavior.

Cuturi's 2013 paper provides the regularized optimal-transport foundation; the
upstream log-domain implementation also cites Feydy et al. (2019). Full links
and file-level provenance are retained in the third-party notice.
