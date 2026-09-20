# Project workflow

Before implementation or review, read the approved specification at
`docs/superpowers/specs/2026-09-19-sinkhorn-learning-design.md` and this repository's
task in `docs/superpowers/plans/2026-09-19-sinkhorn-library.md`.
Read the numerical/animation contracts linked by the spec when working on those
behaviors. The spec is authoritative; preserve its exact numerical semantics.

Use isolated feature worktrees, test-driven-development, subagent-driven task
implementation, and spec/quality review. Record task evidence in the plan ledger.
Human approval is required for consequential scope/dependency changes, public
side effects, PR creation, merge and release. No direct default-branch work.
Retain research provenance and upstream notices. Do not claim shipped before
the complete spec quality gate and approved merges.
