# Sinkhorn learning project

Vocabulary for the numerical library and its educational application.

## Language

**Transport problem**:
Source weights, destination weights, and pairwise per-unit costs describing
the allocation being sought.

**Transport plan**:
A matrix whose entries are amounts allocated from each source to each
destination. An intermediate or failed plan need not satisfy the requested totals.
_Avoid_: Route map, completed shipment

**Marginal**:
A plan's row or column totals, corresponding to source or destination quantities.

**Regularization**:
The term that favors spreading a plan's mass; its strength changes the optimization
problem rather than merely its implementation speed.

**Transport cost**:
The sum of each plan entry multiplied by its corresponding per-unit cost.
_Avoid_: Sinkhorn divergence, regularized objective, exact Wasserstein distance

**Basic solver**:
The direct kernel-scaling Sinkhorn implementation corresponding to the pinned
Python `sinkhorn_knopp` reference.

**LogDomain solver**:
The logarithmic-coordinate Sinkhorn implementation corresponding to the pinned
Python `sinkhorn_log` reference.
_Avoid_: Sinkhorn stabilized solver

**Solver trace**:
A sequence of recorded intermediate numerical states with iteration and phase
identities, including whether an attempted update was accepted or rejected.
_Avoid_: Shipment playback

**Shipment playback**:
A presentation of grain moving according to a fixed final transport plan.
_Avoid_: Solver iteration, convergence animation

**Termination reason**:
The numerical stopping event, distinct from whether the returned plan is usable.

**Usable plan**:
A returned plan satisfying the approved conservative finite-value, nonnegativity,
termination, and marginal-error policy.

**Scenario**:
An educational transport problem together with its units, labels, and layout.

**Sampled trace**:
A trace presentation retaining only selected recorded states, with omitted
iterations identified rather than implied to have never occurred.
