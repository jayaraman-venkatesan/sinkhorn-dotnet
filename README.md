# sinkhorn-dotnet

Status: in development. The public data contracts, input validation, and stable
log-sum-exp primitive are implemented. The Basic and LogDomain solvers are not
implemented yet.

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
