# Basic and log-domain Sinkhorn: reference comparison

Research for [ticket #8](https://github.com/jayaraman-venkatesan/research-to-repo/issues/8), checked 2026-09-19. Complements [the log-port evidence](csharp-port-research.md). This is numerical research and proposed result/trace semantics, not approved architecture, a C# implementation, or a performance benchmark.

## Pinned primary source

Reference: POT **0.9.6.post1**, commit **85113e9a380f5fcf684c50c73c1ff6a164a7366e**, [solver source](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py#L487-L923). The recovered installed wheel's `_sinkhorn.py` Git blob hash is `cf5efadfc0f33300899d9b8a20f762e5f96a2759`, matching the earlier pinned-source evidence. Source behavior below was checked directly against that installed file. The browser could not fetch the pinned GitHub page during this pass; this is not a fresh remote commit verification. The mathematical reference is Cuturi's [2013 paper](https://arxiv.org/abs/1306.0895); substantial translated source requires preserving the pinned [MIT notice](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/LICENSE).

## Exact single-target basic behavior

All implementation claims in this section refer to [pinned `sinkhorn_knopp`](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py#L487-L685). With cost matrix `M`, regularization `r`, source `a`, target `b`, and dimensions `n,m`:

1. Convert inputs and choose backend. Empty weight arrays become uniform weights. Defaults are `numItermax=1000`, `stopThr=1e-9`, `verbose=False`, `log=False`, `warn=True`, `warmstart=None`.
2. Initialize scaling `u=ones(n)/n`, `v=ones(m)/m`. A supplied warm start is **log scaling**: initialize with `exp(warmstart[0])`, `exp(warmstart[1])`, even though ordinary output logs contain scaling `u,v` rather than their logarithms.
3. Construct `K=exp(M/(-r))` and `Kp=(1/a)[:,None]*K`. Save previous scaling before each update pair. Calculate `q=K.T@u`, then **v first**: `v=b/q`; then `u=1/(Kp@v)`. In exact arithmetic for positive `a`, the latter equals `a/(K@v)`. Substituting that algebraically equivalent formula changes zero-support and floating-point behavior and is not a faithful literal port.
4. After both updates, detect any `q==0`, NaN in `u` or `v`, or infinity in `u` or `v`. Emit `Warning: numerical errors at iteration N`, restore the previous `u,v`, and break **before** computing that iteration's residual. This warning is unconditional: `warn=False` suppresses only exhaustion warnings. NumPy arithmetic warnings are separate.
5. On indices `0,10,20,...`, compute column sums by `einsum("i,ij,j->j",u,K,v)` and their Euclidean residual against `b`; append to `err` if logging. Stop only for residual **strictly less than** `stopThr`. It does not test both marginals or every update pair.
6. Exhaustion emits `Sinkhorn did not converge...` if `warn=True`; either exhaustion or numerical failure still returns a matrix. `niter` is the last attempted zero-based index, including a failed rolled-back attempt. Logs contain `err,u,v,niter`; no status or full trace. Return expression is `u[:,None]*K*v[None,:]`. Its intermediate arithmetic can underflow even when the saved scalings are finite. Consequently rollback does not guarantee a feasible, or even faithfully materialized, preceding plan. A zero iteration budget leaves `ii` undefined when logging; a wrapper should reject it.

The matrix-target branch returns costs, not individual plans. This note deliberately scopes comparisons to vector `b`. Input validation is not comprehensive; documented normalized-histogram assumptions must not be advertised as checks already performed by POT.

## Comparison with log-domain updates

[Pinned `sinkhorn_log`](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py#L690-L923) uses `S=-M/r`, log scaling `alpha,beta`, and starts both at zero by default. Updates are `beta=log(b)-LSE_rows(S+alpha[:,None])`, then `alpha=log(a)-LSE_columns(S+beta[None,:])`; materialize with `exp(S+alpha[:,None]+beta[None,:])`. Supplied warm starts are used directly as log scalings. The default constant initializations differ from basic scaling; they are gauge-equivalent after a full pair in exact arithmetic, but raw scaling arrays should not be compared directly.

Both solvers use the same default budget, threshold, checkpoint cadence, target L2 stopping rule, and exhaustion warning. Log mode has no corresponding numerical-failure rollback. Its returned `log_u,log_v` remain useful when optional exponentiated diagnostics `u,v` overflow. Avoiding direct kernel underflow does not guarantee fast convergence: the small-regularization example below exhausts 1,000 iterations. These are numerical implementations of the same entropic objective, not evidence that their transient floating-point behavior must match.

Zero source mass is especially consequential for basic mode: `Kp` divides by `a`, and products involving infinite entries and zero target scaling can yield NaN. Zero target weights alone need not fail when denominators remain positive. Neither observation implies a guarantee for arbitrary zero support. Log mode takes `log(0)=-inf` and can solve the tested zero-support case despite an arithmetic warning. Proposed support reduction—remove zero source/target coordinates, solve positive support, restore exact zero rows/columns—should be disclosed and applied to both modes when comparing a product wrapper. Do not silently replace zeros with small positive masses. These conclusions follow from the linked source and executed cases below.

## Executed fixtures

Executed on macOS arm64 using Python **3.12.3**, POT **0.9.6.post1**, NumPy **2.2.6**, SciPy **1.15.3** in an isolated temporary environment. All arrays were float64; both modes used `numItermax=1000, stopThr=1e-9, log=True, warn=True`. The following rounded summaries are observations from that execution, not C# guarantees. Except the rectangular and shifted-cost cases, `M=[[0,1],[1,0]]`.

| Case | Basic result | Log result |
| --- | --- | --- |
| `a=b=[.5,.5], r=1` | `T=[[.36552928931500245,.13447071068499758],[.13447071068499758,.36552928931500245]]`; index 0; cost `.26894142136999516`; no warnings | Same plan within `6e-17`; index 0; same cost; no warnings |
| `a=[.2,.3,.5], b=[.4,.6], M=[[0,1],[1,0],[.5,.2]], r=.3` | Index 30; final sampled L2 `1.4005617260074394e-12`; target L1 `1.9806378759312793e-12`; cost `.17712330022457987` | Index 30; sampled L2 `1.4005224731459274e-12`; same target L1; cost `.1771233002245799`; neither warns |
| `a=[1,0], b=[0,1], r=1` | Numerical failure at index 0; no sampled errors; rollback `T=[[.25,.09196986029286058],[.09196986029286058,.25]]`; source and target L1 both `1`; divide warning | `T=[[0,1],[0,0]]`; index 0; zero residual; `log(0)` warning |
| `a=[.9,.1], b=[.1,.9], r=1e-4` | Numerical failure at index 323; returned `T=[[.9,0],[0,0]]`; source L1 `.1`, target L1 `1.7`; last sampled L2 `1.131370849898476`; division warning | Exhaustion at index 999; `T` approximately `diag(.9,.1)`; source L1 `2.13e-13`, target L1 `1.6`; exhaustion and diagnostic exponentiation-overflow warnings |
| `a=[.2,.8], b=[.3,.7], r=1e10` | `T=[[.0600000000042,.13999999999579998],[.23999999998319999,.5600000000168]]`; index 0; target L1 `2.51999e-11`; no warnings | Same plan within `6e-17`; index 0; target L1 `2.52000e-11`; no warnings |
| `a=[1,0], b=[0,1], r=1e-4` | Failure at index 0; rollback `diag(.25,.25)`; both L1 residuals `1`; division and invalid-multiply warnings | `T=[[0,1],[0,0]]`; index 0; zero residual; logarithm and diagnostic exponentiation warnings |
| `a=b=[.5,.5], M=[[1,2],[2,1]], r=1e-4` | Entire kernel underflows; failure at index 0; zero matrix; both L1 residuals `1`; division warning | Approximately `diag(.5,.5)`; index 0; both L1 residuals `1.72418e-13`; diagnostic exponentiation warning |

The rectangular basic matrix was `[[.19572778843833752,.00427221156166249],[.016527750278165335,.28347224972183466],[.18774446128250682,.3122555387174932]]`. The original five log outputs were independently reproduced. Warm-starting each solver from its rectangular solution converged at index 0, with maximum change from its previous plan `4.838351941316432e-13`. Giving that same rectangular problem a one-pair budget produced the exhaustion warning in both modes. Calling basic on the zero-support example with `warn=False` still produced the division and numerical-error warnings.

Reproduce with these exact package versions in an isolated environment. This compact code prints full plans, sample histories, scaling diagnostics, costs, final residuals, and warnings; it includes the warm-start and warning-control checks. `niter+1` below must not be interpreted as accepted-pair count on rollback.

```python
import sys, warnings
import numpy as np
import scipy, ot
print(sys.version, ot.__version__, np.__version__, scipy.__version__)
cases = [
    ("balanced", [.5,.5], [.5,.5], [[0,1],[1,0]], 1),
    ("rectangular", [.2,.3,.5], [.4,.6], [[0,1],[1,0],[.5,.2]], .3),
    ("zero_support", [1,0], [0,1], [[0,1],[1,0]], 1),
    ("tiny_reg", [.9,.1], [.1,.9], [[0,1],[1,0]], 1e-4),
    ("large_reg", [.2,.8], [.3,.7], [[0,1],[1,0]], 1e10),
    ("zero_and_underflow", [1,0], [0,1], [[0,1],[1,0]], 1e-4),
    ("all_kernel_underflow", [.5,.5], [.5,.5], [[1,2],[2,1]], 1e-4),
]
for name, aa, bb, mm, r in cases:
    a,b,M = [np.asarray(x, dtype=np.float64) for x in (aa,bb,mm)]
    for f in (ot.bregman.sinkhorn_knopp, ot.bregman.sinkhorn_log):
        with warnings.catch_warnings(record=True) as ws:
            warnings.simplefilter("always")
            T,d = f(a,b,M,r,numItermax=1000,stopThr=1e-9,log=True,warn=True)
        print(name, f.__name__, T.tolist(), d)
        print("L1", abs(T.sum(1)-a).sum(), abs(T.sum(0)-b).sum(),
              "cost", np.sum(T*M), "warnings", [str(w.message) for w in ws])
        if name == "rectangular":
            warm = ((np.log(d["u"]), np.log(d["v"]))
                    if f is ot.bregman.sinkhorn_knopp
                    else (d["log_u"], d["log_v"]))
            W,e = f(a,b,M,r,log=True,warmstart=warm)
            print("warm", e["niter"], abs(W-T).max())
            with warnings.catch_warnings(record=True) as limited:
                warnings.simplefilter("always")
                f(a,b,M,r,numItermax=1,log=True,warn=True)
            print("one pair", [str(w.message) for w in limited])
        if name == "zero_support" and f is ot.bregman.sinkhorn_knopp:
            with warnings.catch_warnings(record=True) as quiet:
                warnings.simplefilter("always")
                f(a,b,M,r,log=True,warn=False)
            print("warn=False", [str(w.message) for w in quiet])
```

## Fair comparison and proposed result/trace semantics

Use identical original costs, regularization, masses, support policy, dtype, budgets, and stopping checks. Record initialization and warm-start units. For deliberate step-by-step comparison, supply identical log scalings to both solvers; do not compare exponentiated basic and raw log coordinates as if they were the same units. Compare plans and marginal residuals before scaling vectors because a compensating scaling/gauge shift preserves the plan. Separate raw POT behavior from wrapper validation or support reduction. Separate final transport cost `sum(T*M)` from regularized objective `sum(T*M)+r*sum(T*log(T))` and from debiased Sinkhorn divergence. An infeasible failed plan's low transport cost is not evidence of a good solution. These objective conventions are defined by the [pinned solver source](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py).

For ordinary converged float64 fixtures, the earlier proposed tolerance `abs(actual-reference)<=1e-12+1e-9*abs(reference)` remains a reasonable starting test policy, not measured port accuracy. Check both marginal L1 residuals below `1e-8`; extreme fixtures should check honest failure/exhaustion diagnostics. Do not widen tolerances until incorrect results pass. Threshold crossings may differ near tolerance because reduction arithmetic differs. No timing or comparative speed claim was measured here.

Proposed result semantics: identify solver mode, source version and preprocessing; report plan, transport cost, requested threshold, last sampled error **and its index**, separately computed final row/column residuals, mass and finite-value checks. Distinguish convergence, budget exhaustion, numerical failure, and rejected input. Keep diagnostics-overflow warnings distinct from an unusable final plan: the shifted-cost log example has a good plan and overflowing exponentiated scaling. Expose last attempted index, attempted pairs and accepted pairs separately. For basic failure at index 0, attempted pairs are 1 and accepted pairs 0; at index 323 they are 324 and 323. Preserve the raw upstream warnings alongside any interpreted status.

Proposed trace semantics: immutable snapshots after the v update and after the u update, with explicit phase and iteration index; basic uses scaling coordinates and log uses log coordinates. A failed trial is marked rejected and followed by the restored state, never presented as accepted progress. Observer copies must not round or mutate live arrays or change stopping arithmetic. Bound trace storage and let users request checkpoints or detailed phases. Column agreement after the v step and row agreement after the u step are exact-arithmetic expectations for valid support, not assurances about failed floating-point states. Label a later animation that ships the final plan as shipment playback; it is not an iteration trace. POT does not expose this trace API, and no instrumented-trace oracle was executed in this pass. These are proposals for a later specification decision, not implementation authorization.
