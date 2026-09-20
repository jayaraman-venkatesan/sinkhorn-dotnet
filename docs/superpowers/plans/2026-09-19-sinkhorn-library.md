# Sinkhorn Numerical Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development with the orchestrator's executing-plans routing to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver an independently usable and verified C# Basic/LogDomain Sinkhorn library.

**Architecture:** A dense numerical core preserves pinned Python computation.
Validation and assessment surround the core; immutable observation enables
teaching without changing numerical behavior. The application consumes a pinned
Git submodule, not a second solver.

**Tech Stack:** .NET 10, C#, xUnit, pinned Python reference fixtures.

**Spec:** [Approved design](../specs/2026-09-19-sinkhorn-learning-design.md), including its linked numerical contract and research.

## Global Constraints

- Planning only until ticket and repository approvals. All paths below are
  relative to the future numerical-library repository, not the orchestration repo.
- Both Basic and LogDomain; no whole-POT API, GPU, batching or implicit normalization.
- Reference POT 0.9.6.post1, commit `85113e9a380f5fcf684c50c73c1ff6a164a7366e`.
- Defaults: 1,000 update pairs, threshold 1e-9; checks at 0,10,20,…; strict `<`.
- Invalid calls throw argument exceptions; unsuccessful numerical calls return
  the available result with explicit status and separate validity checks.
- Mass tolerance `1e-12 * max(sourceTotal, targetTotal)`; accepted discrepancy
  remains unmodified. Preserve empty-vector uniform fallback and zero weights.
- Isolated worktree; TDD; fresh implementer and task reviewer; user gates override
  skill advice to make autonomous consequential changes. No automatic push/PR/merge.
- Read installed SDD prompts at execution time. Workers do not spawn helpers.
  Review covers spec and quality; final code-review covers the whole branch.
- Record commands, observed red/green evidence, commits, review findings and
  decisions in the plan-specific SDD ledger. Never claim tests passed from this plan.

## File map and stable interfaces

`src/Sinkhorn/` holds one type/responsibility per file. `tests/Sinkhorn.Tests/`
holds xUnit tests and copied fixture JSON. `reference/` holds the pinned Python
generator and requirements; `examples/Usage/` is a runnable console example.
Use a solution `Sinkhorn.slnx`, `global.json`, `.editorconfig`, and a minimal
`Directory.Build.props` enabling nullable reference types and warnings as errors.

Public contracts created in Task 1 and completed without renaming downstream:

```csharp
namespace Sinkhorn;
public enum SolverKind { Basic, LogDomain }
public enum TerminationReason { ThresholdMet, IterationLimit, NumericalBreakdown }
public enum TracePhase { Initial, AfterDestination, AfterSource, Restored }
public sealed record WarmStart(double[] SourceLogScaling, double[] TargetLogScaling);
public sealed record SolverOptions(int MaxIterations = 1000,
    double Threshold = 1e-9, WarmStart? WarmStart = null);
public sealed record TransportProblem(double[] Source, double[] Target, double[,] Costs);
public sealed record ErrorSample(int Index, double TargetL2);
public sealed record PlanChecks(bool Finite, bool Nonnegative,
    double SourceL1, double TargetL1, double TotalMass, bool Usable);
public sealed record ScalingDiagnostics(double[] Source, double[] Target, bool IsLog);
public sealed record TraceFrame(int Index, TracePhase Phase, SolverKind Solver,
    double[] SourceScaling, double[] TargetScaling, bool Rejected);
public sealed record SolverResult(SolverKind Solver, double[,] Plan,
    TerminationReason Termination, int LastAttemptedIndex, int AttemptedPairs,
    int AcceptedPairs, ErrorSample[] Errors, ScalingDiagnostics Scaling,
    PlanChecks Checks, double TransportCost, string[] Warnings);
public interface ITraceObserver { void Observe(TraceFrame frame); }
// Static entry point introduced in Task 2, extended by Tasks 3–4:
// SolverResult SinkhornSolver.Solve(TransportProblem problem, double regularization,
//   SolverKind solver, SolverOptions? options = null,
//   ITraceObserver? observer = null, CancellationToken cancellationToken = default)
```

Caller arrays are copied/validated before solving; observer frames receive copies.
Warnings are stable codes: `iteration-limit`, `numerical-breakdown`,
`nonfinite-plan`, `diagnostic-overflow`. Python incidental runtime warnings are
not promised verbatim. Cancellation throws `OperationCanceledException`, outside
numerical termination; it is never returned as a usable solution.

## Dependencies

Task 1 → Task 2 → Task 3 → Task 4 → Task 5. Execute sequentially. App work may
start after the reviewed Task 5 library commit is available locally; publication
and app pinning still require the applicable repository/PR approvals.

### Task 1: Validated numerical inputs and stable primitives (ticket LIB-01)

**Files:** Create the public type files above; `ProblemValidation.cs`,
`Numerics.cs`, `tests/Sinkhorn.Tests/ValidationTests.cs`, `NumericsTests.cs`,
solution/project configuration, initial README and third-party provenance notice.

**Interfaces:** `ProblemValidation.Prepare(problem, regularization, options)`
returns a cloned `TransportProblem`. `Numerics.LogSumExp(ReadOnlySpan<double>)`
returns double. Internal primitives remain internal with test assembly access.

- [ ] Create the .NET class/test projects and pin compatible SDK/xUnit versions
  after checking selected release licenses. Remove generated placeholder tests.
  Add a project reference and nullable/warnings-as-errors configuration.
- [ ] Add failing tests including:

```csharp
[Fact]
public void RejectsUnequalMass()
{
    var p = new TransportProblem([1.0], [2.0], new double[,] {{0.0}});
    Assert.Throws<ArgumentException>(() =>
        ProblemValidation.Prepare(p, 1.0, new SolverOptions()));
}
[Fact]
public void LogSumExpPreservesLargeFiniteValues()
{
    Assert.InRange(Numerics.LogSumExp([1000.0, 1000.0]),
        1000.0 + Math.Log(2.0) - 1e-12, 1000.0 + Math.Log(2.0) + 1e-12);
    Assert.Equal(double.NegativeInfinity,
        Numerics.LogSumExp([double.NegativeInfinity, double.NegativeInfinity]));
}
```

- [ ] Run `dotnet test --filter 'FullyQualifiedName~ValidationTests|FullyQualifiedName~NumericsTests'`.
  Confirm expected missing implementation/assertion failure, not broken SDK setup.
- [ ] Implement validation in explicit order: null/shape and positive dimensions;
  empty-vector uniform expansion; finite/nonnegative weights and finite costs;
  finite positive sums and compatible mass; positive finite reg/threshold and
  positive budget; warm-start lengths and no NaN/+infinity. Accept negative
  infinity warm starts and negative finite costs; do not preprocess support.
  Implement stable primitive:

```csharp
internal static double LogSumExp(ReadOnlySpan<double> values)
{
    double max = double.NegativeInfinity;
    foreach (double x in values) { if (double.IsNaN(x)) return double.NaN; max = Math.Max(max, x); }
    if (double.IsInfinity(max)) return max;
    double sum = 0;
    foreach (double x in values) sum += Math.Exp(x - max);
    return max + Math.Log(sum);
}
```

- [ ] Add case-driven tests for every validation rule, clone isolation, zero
  support, equal 100-unit mass, negative costs, uniform fallback, tolerances
  immediately inside/outside the accepted mass range, and overflowed totals.
  Run `dotnet test` and `dotnet format --verify-no-changes`.
- [ ] Update README input rules and source/license provenance. Commit the explicit
  task files with `git commit -m "feat: validate transport inputs and numerical primitives"`;
  controller records reviewed commit. No push.

### Task 2: Basic solver and truthful assessment (ticket LIB-02)

**Files:** Create `SinkhornSolver.cs`, `BasicSolver.cs`, `PlanAssessment.cs`,
`tests/Sinkhorn.Tests/BasicSolverTests.cs`, `AssessmentTests.cs`,
`reference/generate_fixtures.py`, `reference/requirements.txt`,
`tests/Sinkhorn.Tests/Fixtures/basic.json`.

**Interfaces:** Introduce the exact `SinkhornSolver.Solve` signature above.
`PlanAssessment.Assess(plan, problem, threshold, termination)` returns `PlanChecks`.
Until Task 3, LogDomain selection explicitly throws `NotSupportedException` in
the intermediate branch; it must be removed before library release.

- [ ] Add the symmetric reference test and the failure test first:

```csharp
[Fact]
public void BasicMatchesSymmetricReference()
{
    var p = new TransportProblem([.5,.5], [.5,.5], new double[,] {{0,1},{1,0}});
    var r = SinkhornSolver.Solve(p, 1, SolverKind.Basic);
    Assert.InRange(r.Plan[0,0], .365529289314, .365529289317);
    Assert.Equal(0, r.LastAttemptedIndex);
    Assert.True(r.Checks.Usable);
}
[Fact]
public void BasicReturnsRejectedZeroSupportResult()
{
    var p = new TransportProblem([1,0], [0,1], new double[,] {{0,1},{1,0}});
    var r = SinkhornSolver.Solve(p, 1, SolverKind.Basic);
    Assert.Equal(TerminationReason.NumericalBreakdown, r.Termination);
    Assert.Equal(1, r.AttemptedPairs);
    Assert.Equal(0, r.AcceptedPairs);
    Assert.False(r.Checks.Usable);
}
```

- [ ] Run `dotnet test --filter FullyQualifiedName~BasicSolverTests` and record red.
- [ ] Translate the pinned Basic source's single-target body. Preserve
  `K=exp(-M/reg)`, `Kp[i,j]=K[i,j]/a[i]`, default u=1/n, v=1/m,
  exponentiated log warm starts, and v-then-u operations:

```csharp
for (int j = 0; j < m; j++) {
    q[j] = 0; for (int i = 0; i < n; i++) q[j] += kernel[i,j] * u[i];
    v[j] = b[j] / q[j];
}
for (int i = 0; i < n; i++) {
    double sum = 0; for (int j = 0; j < m; j++) sum += scaledKernel[i,j] * v[j];
    u[i] = 1 / sum;
}
```

  Save previous scalings before each pair; on zero q/nonfinite scaling restore
  and break before the checkpoint. At indices divisible by 10 calculate target
  L2 and stop strictly below threshold. Materialize `(u[i]*K[i,j])*v[j]`;
  do not replace it with a numerically different stable expression.
- [ ] Compute transport cost and final row/column L1, finite/nonnegative checks,
  total mass, warnings and counts independently. Usable requires ThresholdMet,
  finite/nonnegative plan and both L1 below the requested threshold. Neither a
  cheap infeasible plan nor a rollback implies usability.
- [ ] Generate strict fixture JSON from the pinned Python source for all seven
  existing cases, warm start, one-pair exhaustion, non-unit cases and negative
  costs. Record versions/source hash; represent nonfinite values as explicit
  tags. The generator uses the research scripts' verified inputs, not hand-edited
  expected outputs. Add per-field fixture assertions; entry/cost tolerance is
  `1e-12 + 1e-9*abs(reference)`. Extreme tests assert failure and counts, not relaxed accuracy.
- [ ] Run `dotnet test`, `dotnet build -c Release`, format check. Add README
  Basic example and failure explanation; commit as `feat: port basic Sinkhorn with explicit results`.

### Task 3: LogDomain solver parity (ticket LIB-03)

**Files:** Create `LogDomainSolver.cs`, `tests/Sinkhorn.Tests/LogDomainSolverTests.cs`,
`Fixtures/log-domain.json`; modify `SinkhornSolver.cs`, fixture generator and README.

**Interfaces:** Same entry point/result types; `Scaling.IsLog=true` for LogDomain.
Optional exponentiated diagnostics must not overwrite log coordinates.

- [ ] Add failing tests:

```csharp
[Fact]
public void LogDomainSolvesZeroSupportWithoutChangingInputs()
{
    var p = new TransportProblem([100,0], [0,100], new double[,] {{0,1},{1,0}});
    var r = SinkhornSolver.Solve(p, 1, SolverKind.LogDomain);
    Assert.InRange(r.Plan[0,1], 100 - 1e-10, 100 + 1e-10);
    Assert.True(r.Checks.Usable);
    Assert.Equal(0.0, p.Source[1]);
    Assert.True(r.Scaling.IsLog);
}
```

- [ ] Run `dotnet test --filter FullyQualifiedName~LogDomainSolverTests`; confirm
  unsupported-mode red, then replace dispatch with the real LogDomain core.
- [ ] Implement `S=-M/reg`, alpha/beta initialized to zero or supplied log warm
  starts. For each j use `beta[j]=log(b[j])-LSE_i(S[i,j]+alpha[i])`, then for
  each i use `alpha[i]=log(a[i])-LSE_j(S[i,j]+beta[j])`. Materialize with:

```csharp
plan[i,j] = Math.Exp(scaledCost[i,j] + alpha[i] + beta[j]);
```

  Preserve the same sampled target L2 stopping rule; do not add Basic rollback.
  Reuse independent assessment, retaining nonfinite/limit facts separately.
- [ ] Generate LogDomain fixtures from the same pinned inputs; compare plans,
  sampled errors and safely away-from-threshold indices. Test tiny-reg exhaustion,
  all-kernel-underflow fixture with valid log plan, warm starts, large reg and
  non-unit totals. Do not compare raw Basic and log coordinate values directly.
- [ ] Run full tests, Release build and format checks. Update runnable usage for
  both modes and diagnostic overflow. Commit `feat: port log-domain Sinkhorn`.

### Task 4: Immutable phase observation and cancellation (ticket LIB-04)

**Files:** Modify both solver cores and `SinkhornSolver.cs`; create
`tests/Sinkhorn.Tests/TraceTests.cs`, `CancellationTests.cs`,
`Fixtures/phase-traces.json`; extend reference generator with actual pinned-code
observation from the recorded mass/trace research.

**Interfaces:** `ITraceObserver.Observe(TraceFrame)` receives copied scalings;
rejection is identified by a subsequent `Restored` event for the same index.
Observer implementations must mark retained trial phases at that index rejected.
For ordinary phases `Rejected=false` means provisional until the next accepted
iteration or successful return. This event protocol avoids mutating old snapshots.

- [ ] Add a collecting observer in tests, then assert output equality with and
  without observation, exact phase order, and rollback evidence:

```csharp
sealed class Collector : ITraceObserver {
    public List<TraceFrame> Frames { get; } = [];
    public void Observe(TraceFrame f) => Frames.Add(f);
}
[Fact]
public void BasicFailureEmitsRestoration()
{
    var c = new Collector();
    var p = new TransportProblem([1,0], [0,1], new double[,] {{0,1},{1,0}});
    var r = SinkhornSolver.Solve(p, 1, SolverKind.Basic, observer:c);
    Assert.Contains(c.Frames, f => f.Phase == TracePhase.Restored && f.Index == 0);
    Assert.Equal(0, r.AcceptedPairs);
}
```

- [ ] Run `dotnet test --filter 'FullyQualifiedName~TraceTests|FullyQualifiedName~CancellationTests'`.
  Confirm missing trace/cancellation behavior before adding hooks.
- [ ] Copy scalings at initial, after-v, after-u, and restored points. Invoke the
  observer only on copies. Check cancellation before each update pair with
  `cancellationToken.ThrowIfCancellationRequested();`; never replace cancelled
  execution with a ThresholdMet result. Do not recalculate checkpoint values
  through trace code. Exceptions in a caller observer propagate, not become
  numerical solver statuses; document this observer contract.
- [ ] Verify mutation of observer copies cannot alter live state; compare all
  result fields trace-on/off; compare phase coordinates/plans to Python evidence;
  test cancellation before start and between pairs using deterministic callback
  cancellation, not sleeps. Check Basic rejection/restore and LogDomain exhaustion.
- [ ] Run full tests/Release build/format. Document provisional phases and copied
  values in README. Commit `feat: expose faithful traces and cancellation`.

### Task 5: Independently runnable library and release evidence (ticket LIB-05)

**Files:** Create `examples/Usage/Program.cs`, its console project,
`Dockerfile`, `.dockerignore`, `docs/verification.md`, license/notices, glossary,
`docs/adr/0001-reference-semantics.md`, CI build/test workflow. Update README.

**Interfaces:** Console example exits 0 only when ordinary runs are usable and
the intentional Basic zero-support run is explicitly unsuccessful. No NuGet
publication or remote registry push is part of this ticket.

- [ ] Write smoke acceptance before the example: expected output includes
  `Basic: usable`, `LogDomain: usable`, `Basic zero support: NumericalBreakdown`.
  Run `dotnet run --project examples/Usage -c Release`; confirm missing example red.
- [ ] Implement the console using the tests' symmetric/zero-support inputs and
  `SinkhornSolver.Solve`; inspect status/checks before showing allocations.
  Exit nonzero for unexpected outcomes. Include no network/service dependencies.

```csharp
using Sinkhorn;
var ordinary = new TransportProblem([.5,.5], [.5,.5], new double[,] {{0,1},{1,0}});
foreach (var kind in new[] { SolverKind.Basic, SolverKind.LogDomain }) {
    var result = SinkhornSolver.Solve(ordinary, 1, kind);
    if (!result.Checks.Usable) return 1;
    Console.WriteLine($"{kind}: usable");
}
var zero = new TransportProblem([1,0], [0,1], new double[,] {{0,1},{1,0}});
var failed = SinkhornSolver.Solve(zero, 1, SolverKind.Basic);
Console.WriteLine($"Basic zero support: {failed.Termination}");
return failed.Termination == TerminationReason.NumericalBreakdown
    && !failed.Checks.Usable ? 0 : 1;
```

- [ ] Add a multistage SDK/runtime container for the usage executable. Pin selected
  image versions/digests during execution; do not invent them in this plan.
  Build/publish commands: `dotnet publish examples/Usage -c Release -o /out`;
  runtime entrypoint: `dotnet Usage.dll`.
- [ ] Run `dotnet test -c Release`, `dotnet format --verify-no-changes`,
  `dotnet build -c Release`, usage smoke, and `docker build -t sinkhorn-library-check .`
  followed by `docker run --rm sinkhorn-library-check`. Capture actual architecture
  evidence; no claim of cross-architecture runtime success without execution.
- [ ] Finish README, pinned fixture regeneration, precision/status limitations,
  MIT/POT notices and actual dependency audit. Record the approved reference
  semantics ADR and glossary in this repository. Audit Git diff for accidental
  secrets/unrelated files. Commit `docs: verify standalone library usage and distribution`.
- [ ] Controller performs whole-library standards/spec review and fresh checks.
  Ask before PR/release/merge. A reviewed local commit can serve app development;
  the shipped app must ultimately pin the verified merged library commit.

## Plan self-review

Spec coverage: numerical behavior and failure policy Tasks 1–3; phase traces and
cancellation Task 4; independent use, licenses and verification Task 5. Website
resource caps deliberately live in the companion app plan, not this core.
All tests call the single stable signature; every named public type is defined
above. TDD commands are future execution instructions, not reported results.
