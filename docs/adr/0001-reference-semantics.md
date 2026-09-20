# Preserve reference behavior and assess results explicitly

The library ports the pinned Basic and LogDomain update and stopping behavior
without silent normalization, support reduction, solver substitution, or
automatic fallback. Validation and explicit result assessment surround that
calculation instead: this exposes inconvenient numerical failures while
preserving reproducibility and making limitations visible to callers.

## Final-review amendment: result provenance snapshot

The final whole-library review found that the original implementation plan's
[`SolverResult` shape](../superpowers/plans/2026-09-19-sinkhorn-library.md#task-1-validated-numerical-inputs-and-stable-primitives-ticket-lib-01)
omitted provenance and both input totals required by the approved
[numerical result contract](../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/library-contract-proposal.md#what-every-result-reports).
The controller ruled that the normative contract governs because no waiver was
approved. The result therefore gains one additive `SolverMetadata` snapshot;
the `Solve` signature, existing result fields, and numerical behavior remain
unchanged. This is a final-review correction, not a claim that the original plan
already specified the added record.

The snapshot records the solver, pinned POT version and commit, regularization,
effective options, preprocessing policy `none`, and both prepared input totals.
It is created after validation and empty-vector fallback but before solver or
observer execution. Supplied warm-start arrays are copied, and an accepted mass
discrepancy is reported rather than repaired.
