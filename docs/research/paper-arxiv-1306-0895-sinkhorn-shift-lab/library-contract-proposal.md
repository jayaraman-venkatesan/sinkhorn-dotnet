# C# numerical contract — approved design

The human approved the consolidated contract with “looks good” after an
explicit whole-contract review request. Approval evidence is in the linked
decision ticket. Recommendations below are now accepted design requirements;
verification obligations remain outstanding. This is not implementation approval.

Review: [Sinkhorn: approve numerical inputs and result reporting](https://github.com/jayaraman-venkatesan/research-to-repo/issues/12).

This is a design checkpoint, not the complete project specification or permission
to implement. Based on the already executed [paired reference comparison](basic-solver-comparison.md)
and [port investigation](csharp-port-research.md), not new experiments.

## Consolidated approval proposal

### Already decided — no new approval requested

Both Basic and LogDomain solvers; a reusable C# numerical library following the
pinned Python reference; no warehouse-specific types; and available results
returned with explicit failure information when a numerical run is unsuccessful.

### Remaining recommendations — approve together or request changes

1. **Keep version one focused.** Solve one source/destination problem per call
   using ordinary double-precision numbers on the CPU. Accept quantities, a
   cost table, regularization, a solver choice, and optional settings. No GPU,
   automatic differentiation, or whole-POT-toolbox promise.
2. **Use the numbers supplied.** Support zero and fractional weights without
   rounding, silently normalizing, or repairing them. Target equal positive
   totals, including non-unit totals such as 100 kg, with Python comparison
   tests required before shipping that support. The website passes kilograms
   directly and receives kilograms back; this mapping is a proposed release
   requirement, not an already verified guarantee. If parity fails, return
   for a design decision rather than silently adding normalization.
3. **Reject bad calls clearly.** Wrong dimensions, negative quantities, invalid
   numeric values, or incompatible totals throw documented argument errors.
   Preserve the documented empty-vector uniform fallback. The detailed
   tolerance below is a proposed engineering starting point, not a Python rule.
4. **Keep calculation and assessment separate.** Preserve Python's stopping
   checks and return the plan, transport cost, iteration information, warnings,
   and final source/destination errors. Also supply a conservative usability
   flag without changing the computed answer. Never silently retry with the
   other solver or label an unfinished result successful.
5. **Prove and explain compatibility.** Test both C# solvers against the pinned
   Python version, document intentional C# differences, and provide README
   examples for both successful and unsuccessful runs. Use numerical
   tolerances, not a promise of identical floating-point bits.

Approval covers these behavioral requirements and the detailed contract below,
including proposed validation and usability policies. It does not authorize
implementation, repository creation, or a release. Exact method/type names may
be refined within this contract; changes to accepted inputs, numerical
semantics, or success policy must be surfaced rather than silently changed.

### Concrete example

An app supplies source quantities `[40, 60]`, destination quantities `[50, 50]`,
and costs `[[1, 4], [3, 1]]`, plus a positive regularization value and solver
choice. The returned 2-by-2 table says how much each source sends to each
destination. No numerical answer is asserted here: the exact allocation
depends on the settings and must be verified. The library never needs to know
whether the units represent grain or something else.

### What this review does not settle

Animation trace format/limits, website technology and layout, shared
Markdown/website publishing, repository names, container architecture, and
the complete project specification remain subsequent design work. This is
one consolidated library contract review, not the final project-spec gate.

## What an application supplies

- A source-weight vector `a` and destination-weight vector `b`.
- A rectangular cost matrix `M`; `M[i,j]` is the cost per unit from source i to destination j.
- A finite positive regularization value: the strength of the spreading preference.
- An explicit choice of Basic or LogDomain; neither silently switches to the other.
- Optional iteration budget (default 1,000), stopping threshold (default 1e-9),
  and a pair of log-scaling warm-start vectors.

First release: one target vector per call, CPU, dense double-precision values.
No GPU, autodifferentiation, batching, sparse matrices, automatic cost generation,
or port of the full POT API is promised. Reuse means any application fitting
this numerical problem, not every variant of optimal transport.

The solver never knows about grain, currencies, warehouse positions, or pixels.
It does not normalize, round, clip, remove zero coordinates, shift costs, or
replace zero weights with small positive values behind the caller's back.
Fractional and zero weights retain their meaning. In particular, Basic may fail
on a zero-weight case that LogDomain solves; that difference must remain visible.

POT documents normalized histograms. Those inputs form the already researched
compatibility baseline. Proposed release scope also accepts non-unit equal
totals without changing their magnitude. This requires explicit Python/C#
fixtures before being advertised as supported, including kilograms passed
directly by the app. Document the absolute stopping threshold's scale dependence;
arbitrary magnitude is not a guarantee of convergence or numerical stability.

Empty weight vectors follow the reference's uniform-weight fallback, with
dimensions inferred from a nonempty cost matrix. Document this convenience
prominently; it is not a zero-mass problem.

## Deliberate C# boundary validation

Reject invalid arguments before iteration: null inputs, mismatched shapes,
zero-sized cost dimensions, negative or nonfinite weights, nonpositive or
nonfinite total mass, nonfinite costs, invalid regularization, nonpositive
iteration budget, or nonpositive/nonfinite threshold. Finite costs may be
negative; the library adds no application-specific distance assumption.

Warm starts must match dimensions; reject NaN and positive infinity. Negative
infinity is representable as zero scaling and is not silently replaced.
It can still lead to a numerical failure, which is reported honestly.

Reject unequal total weights when their difference exceeds
`1e-12 * max(sourceTotal, targetTotal)`. This is a proposed validation tolerance,
not a Python guarantee. Accepting a tiny discrepancy does not repair it;
report both totals and resulting residuals. Reject totals that overflow.

Use documented argument exceptions for these invalid calls. These checks are
intentional additions: POT does not comprehensively enforce its preconditions.
They must have separate tests, not be described as literal source parity.

## What remains reference-aligned

Reference: POT 0.9.6.post1 at commit
`85113e9a380f5fcf684c50c73c1ff6a164a7366e`, single-target `sinkhorn_knopp`
and `sinkhorn_log`. Preserve initialization, warm-start interpretation, v-then-u
update order, and each solver's arithmetic formulation.

Sample the target L2 residual at zero-based indices 0, 10, 20, and so on;
stop only when it is strictly below the requested threshold. Final validation
does not alter that rule. Basic preserves its numerical-error rollback;
LogDomain does not acquire a new early-exit or rollback policy silently.
Floating-point reduction differences mean tolerance-based parity, not bitwise
identity or identical stopping indices on borderline cases.

## What every result reports

Return a structured result for completed solver calls, including unsuccessful
ones. Invalid arguments instead throw before a call starts.

| Result information | Meaning |
| --- | --- |
| Plan | Amount assigned to each source/destination pair; may be an unfinished or invalid diagnostic result |
| Termination reason | Reference stopping threshold met, iteration limit reached, or Basic numerical breakdown |
| Plan checks | All entries finite and nonnegative, actual total, both source and destination residuals |
| Iterations | Last attempted zero-based index, attempted update pairs, accepted update pairs; rollback is not accepted progress |
| Sample history | Target L2 errors with their iteration indices; missing samples remain absent, not zero |
| Transport cost | Sum of plan entry times corresponding input cost, explicitly unavailable/nonfinite when arithmetic fails |
| Scaling diagnostics | Basic scaling values; log-domain log scalings, with exponentiated values optional |
| Provenance | Solver choice, reference version, supplied options, and explicit preprocessing policy: none |
| Warnings | Structured codes for exhaustion, numerical breakdown, nonfinite plan, and diagnostic overflow as applicable |

Termination and validity are separate. A LogDomain run can hit the iteration
limit with nonfinite values; retain both facts. Overflow in optional
exponentiated scaling does not by itself invalidate a finite transport plan.
Do not promise NumPy's exact warning text or incidental runtime warnings in C#.

Compute final row and column L1 residuals independently. For the first-release
conservative `UsablePlan` flag, require the reference stopping condition,
finite/nonnegative entries, and both L1 residuals below the requested stopping
threshold. This added flag is stricter than POT's target-only L2 test and does
not change the iteration result. Return the separate checks so callers can
apply their own documented accuracy policy. A false flag must not be disguised
as success because the transport cost happens to be low.

Transport cost is not the regularized objective, exact Wasserstein distance,
or Sinkhorn divergence. Do not return those under misleading names.

## Teaching and integration

The website must inspect result status and validity before presenting shipment
playback as a completed solution. Failed or exhausted results may be shown as
clearly marked diagnostic views, never as completed deliveries.

An optional read-only trace is a separate teaching extension, not part of the
upstream API. Its detailed schema and limits remain a later design decision.
Enabling it must not change solver arithmetic, stopping checks, or results.

Method names and C# container types remain reversible API-design details.
The README must show both solver choices, explain each input, show checking
results before use, identify differences from Python, and preserve attribution.

## Review and verification still required

Human review is required for the remaining supported scope, explicit validation,
and usability policy; returning unsuccessful results is already approved.
Numerical thresholds here are proposals,
not measured C# guarantees. Tests must cover the seven existing paired cases,
warm starts, exhaustion, validation, uniform fallback, non-unit mass, negative
costs, and the distinction between diagnostic overflow and invalid plans.
No C# implementation or cross-language verification has happened yet.
