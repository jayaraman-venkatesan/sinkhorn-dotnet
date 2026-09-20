# Library review handoff

2026-09-20. Library implementation is locally verified and awaiting human PR
approval. It is not merged, published, or shipped. The separate learning website
has not been implemented.

## Standards

The whole-branch standards review found no Critical or Important issue. Its
minor duplicated fixture decoding/conversion finding was addressed by a shared
test helper. The historical LIB-02 compile-only RED remains disclosed: later
behavioral mutation checks strengthen the tests but do not rewrite that history.
The scoped final re-review passed the changed standards surface.

## Specification

The whole-library review found one Important gap: the original implementation
plan omitted required result provenance, settings, and input totals. The final
fix adds owned `SolverMetadata` snapshots for both solvers, including failed
results. Independent re-review confirms the gap is addressed without changing
solver arithmetic, stopping, rollback, assessment, or existing result fields.

Initial findings: Standards two Minor (one code smell fixed, one historical
process deviation retained); Specification one Important, fixed. No unresolved
Critical or Important finding remains.

## Verification and exact revisions

- Initial whole-library review: `096186e..18df041`.
- Final production correction: `773665f56a6a71d1b5465a2afaaef56f8964fabd`.
- Independent scoped re-review: `18df041..862f080` (includes controller evidence).
- Fresh controller checks: locked restore; 85 Release tests; zero-warning
  Release build; format check; actual usage smoke; current native ARM64 and
  emulated AMD64 containers with networking disabled. See [verification](verification.md).
- Remote CI and native AMD64 execution are not claimed. No package or container
  registry publication, PR, or merge has occurred.

## Rulings retained

1. Use compilable seams for behavioral RED, excluding SDK/missing-reference
   failures. This preserves the intended test-first evidence without changing
   product behavior; a wrong seam could weaken that evidence.
2. Preserve pinned Python reciprocal-then-multiply arithmetic rather than the
   plan's division shorthand. The normative spec and actual source govern;
   choosing incorrectly could change floating-point parity/failure behavior.
3. Accept the honestly recorded LIB-02 initial compile-only RED deviation with
   subsequent restored mutation evidence and independent review, rather than
   manufacture earlier evidence. Initial sequencing assurance remains weaker;
   later tests do not erase that limitation.
4. Apply the normative result-metadata requirements over the plan's incomplete
   record shape. The additive public result surface expands, so downstream DTOs
   must carry `Metadata`; solver calculations remain unchanged.

## Next human gate

Proposed action: push `feat/sinkhorn-library` to the approved library repository
and open a PR against `main`, titled **Add verified Basic and LogDomain Sinkhorn
library**. This approval would not authorize merge, release, NuGet publication,
or container-registry publication. Keep the worktree and review ledger until
the applicable integration decisions are complete.
