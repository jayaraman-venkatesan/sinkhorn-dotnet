# Non-unit mass and observed Sinkhorn phase traces

Research for [ticket #18](https://github.com/jayaraman-venkatesan/research-to-repo/issues/18), executed 2026-09-19. Extends [basic/log comparison](basic-solver-comparison.md) and [port evidence](csharp-port-research.md). This is numerical research, not a C# implementation, product trace API, benchmark, or approved specification.

## Source and execution provenance

Executed the installed POT **0.9.6.post1** wheel on macOS 26.5.1 arm64, Python **3.12.3**, NumPy **2.2.6**, SciPy **1.15.3**. Its complete `ot/bregman/_sinkhorn.py` file has Git blob SHA-1 `cf5efadfc0f33300899d9b8a20f762e5f96a2759` and SHA-256 `671958232afa87b8908d183c935475746f3d02c34827e78354d7505200e2dab2`. The blob matches the earlier evidence pinned to commit **85113e9a380f5fcf684c50c73c1ff6a164a7366e**; this pass verified the installed bytes, not a fresh remote tag lookup. Primary source: [basic solver](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py#L487-L685) and [log solver](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py#L690-L923).

All runs used float64, vector targets, default initialization, `numItermax=1000, stopThr=1e-9, log=True, warn=True`. No support reduction was applied. Each fixture/solver ran unobserved, observed, and normalized with threshold divided by total mass: **30 solver calls**. Below, “kg” is an interpretation of weights; POT attaches no physical units.

## Executed non-unit fixtures

| Fixture | Exact a; b; cost M; regularization | Basic result | Log result |
| --- | --- | --- | --- |
| Symmetric 100 kg | [50,50]; [50,50]; [[0,1],[1,0]]; 1 | Index 0, mass 100, target L1 0 | Index 0, mass 99.99999999999997, target L1 2.84217e-14 |
| Rectangular 100 kg | [20,30,50]; [40,60]; [[0,1],[1,0],[.5,.2]]; .3 | Index 30, mass 100, target L1 1.98064e-10 | Index 30, mass 100.00000000000001, target L1 1.98057e-10 |
| Rectangular quarter kg | [.05,.075,.125]; [.1,.15]; same rectangular M; .3 | Index 30, mass .25, target L1 4.95159e-13 | Index 30, mass .25, target L1 4.95187e-13 |
| Tiny regularization 100 kg | [90,10]; [10,90]; [[0,1],[1,0]]; 1e-4 | Failure/rollback at index 321; diag(90,10), target L1 160 | Exhaustion at index 999; approximately diag(90,10), target L1 160 |
| Zero support 100 kg | [100,0]; [0,100]; [[0,1],[1,0]]; 1 | Failure/rollback at index 0; mass .6839397205857212, both marginal L1 100 | Index 0; [[0,100.00000000000004],[0,0]], both marginal L1 4.26326e-14 |

The first three cases had no warnings. Their maximum basic/log entry differences were 1.42109e-14, 1.06581e-14, and 4.16334e-17, respectively. Rectangular 100 kg basic returned:

```text
[[19.57277884383375,   0.4272211561662488],
 [ 1.6527750278165343,28.347224972183465 ],
 [18.774446128250684, 31.225553871749316 ]]
```

Its transport cost was 17.71233002245799; log cost was 17.712330022457994. These are total transport costs, not cost per kg or the regularized objective.

Ordinary direct-mass runs matched normalized-then-rescaled plans to at most 1.42109e-14 (all six ordinary solver/fixture runs), with the same sampled stopping index. The normalized runs used `stopThr=1e-9 / total_mass`, preserving the physical absolute error threshold. This is evidence for these cases, not a guarantee across masses or implementations.

The [documented POT contract](https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/ot/bregman/_sinkhorn.py) describes histograms summing to one. These experiments establish observed equal non-unit behavior of the pinned implementation; a product accepting kg still needs an explicit mass and tolerance policy.

For positive total mass s, setting T=sP with normalized marginals gives the exact-arithmetic identity
`<T,M> + r sum(T log T) = s[<P,M> + r sum(P log P)] + r s log s`.
The final term is constant on the feasible set, so the optimizer scales by s with the same r. Likewise marginal residuals scale by s and the entropy-dominated plan is `a b^T / s`. This is algebra from the objective in the linked source, not a claim that floating-point trajectories or failures scale identically. If mass units change, a fixed absolute stopping threshold changes its meaning.

## Instrumentation and exact final parity

Instead of translating the algorithm, the script uses Python `sys.settrace` on the **actual installed function code object**. It recognizes the original v assignment, u assignment, and basic rollback v assignment. At the next line event (after the assignment completes), it copies u and v plus the current index. It does not modify source, solver locals, arrays, reductions, warnings, or stopping decisions. Derived plans and residuals are computed only after the solver has returned. The callback filters by code object, so nested backend and log-plan calls are not sampled.

All **10 observed runs** exactly matched their unobserved counterparts using `np.array_equal(..., equal_nan=True)` for the final plan and every log field (including full error history and scalings), and equality of the ordered warning category/message pairs. There is no tolerance in this parity assertion. This is exact numerical-array equality, not a signed-zero/payload byte comparison. Warning filenames/line metadata were not compared. Trace output was collected for every phase; the printed report retains index 0, final attempted index, and rollback events.

For the rectangular 100 kg case, both solvers generated 62 phase records over 31 update pairs. First-pair plans matched within rounding; the basic values show the alternating constraints directly:

| Phase at index 0 | Actual row sums | Actual column sums | Row L1 | Column L1 |
| --- | --- | --- | --- | --- |
| after-v | [34.0468104039706,39.897682823340176,26.055506772689213] | [39.99999999999999,60] | 47.888986454621566 | 7.10543e-15 |
| after-u | [19.999999999999996,30.000000000000004,50] | [31.903948415632357,68.09605158436764] | 7.10543e-15 | 16.192103168735287 |
| after-u, index 30 | [19.999999999999996,30,50] | [39.999999999900965,60.00000000009903] | 3.55271e-15 | 1.98064e-10 |

The first after-v basic coordinates were u=[1/3,1/3,1/3], v=[97.99521421906466,116.19716784990705]. Log coordinates were alpha=[0,0,0], beta=[3.486306354310679,3.6566761823881895]. First after-u basic u=[.1958088463373117,.2506411222997139,.6396600462262466]; log alpha=[-.532004080761658,-.2851208659442639,.651793867143311]. Different coordinate gauges still materialize essentially the same plan.

These snapshots measure tentative transport matrices, not parcels already shipped. A later final-plan shipment animation must be labeled separately from this solver trace.

## Failure and restoration evidence

For the tiny-regularization 100 kg basic case, there were **322 attempted pairs, 321 accepted pairs, 645 phase/restoration records**:

| Index 321 phase | u | v | Materialized trial |
| --- | --- | --- | --- |
| after-v, rejected | [1.0252163753230388e306,2.4385096260408355e-307] | [9.754038504163637e-306,Infinity] | [[10,NaN],[0,Infinity]] |
| after-u, rejected | [NaN,0] | [9.754038504163637e-306,Infinity] | [[NaN,NaN],[0,NaN]] |
| restored | [1.0252163753230388e306,2.4385096260408355e-307] | [8.778634653747274e-305,4.1008655012922796e307] | [[90,0],[0,10]] |

Observed upstream warnings were divide overflow and numerical errors at iteration 321. Restoration is a state event, not another accepted update. Both trial phases are retroactively marked rejected when restoration is observed. The last error checkpoint was index 320 (113.13708498984761); the failed attempt adds no checkpoint. The finite restored plan still violates target demand by L1=160.

Normalization changed this failure: the same normalized basic case failed at index 323 and, after rescaling by 100, differed from the direct returned plan by **10** in one entry. Thus normalization is mathematically justified for feasible solutions but changes numerical failure behavior. The log case executed 1,000 pairs/2,000 phase records without rollback and exhausted; it emitted nonconvergence plus two exponentiation-overflow warnings from diagnostics. “Accepted” here means not rolled back, not converged or feasible.

The zero-support basic case showed failure on the first pair: after-v was finite with columns approximately [0,100]; after-u contained NaN; restoration returned the initial kernel-scaled matrix
`[[.25,.09196986029286058],[.09196986029286058,.25]]`.
There were one attempted pair and zero accepted pairs, with no sampled errors. This restored mass is unrelated to the requested 100 kg. Warnings were divide-by-zero and the numerical-error warning. The log run solved this fixture but emitted two log-zero warnings. No claim of general zero-support robustness follows.

## Reproduction and limits

Create an isolated Python 3.12.3 environment with `POT==0.9.6.post1 numpy==2.2.6 scipy==1.15.3`, save the block below as `mass_trace_evidence.py`, and run it. The exact executed script SHA-256 was `ed6242db1c50b12ff92ec69d4f22e53f322c60e15f760168ab2f90ce0f7669b9` (UTF-8, LF, final newline). It asserts the installed source blob before execution. Output uses Python JSON's NaN/Infinity extensions for failed trial diagnostics; this is a research printout, not a strict JSON interchange format.

This pass did not measure performance, test other platforms/backends/dtypes, validate a C# port, implement bounded product trace storage, or verify arbitrary mass ranges. The observer stores all snapshots for these small fixtures and would require a retention policy in a product. Exact observed/unobserved parity on this runtime does not establish universal absence of observer effects. Source and script are sufficient to rerun the assertions; the tables preserve selected executed evidence, not every array from all 30 calls.

```python
import hashlib, inspect, json, platform, sys, warnings
from pathlib import Path
import numpy as np
import scipy, ot
import ot.bregman._sinkhorn as source

raw = Path(source.__file__).read_bytes()
blob = hashlib.sha1(b'blob ' + str(len(raw)).encode() + b'\0' + raw).hexdigest()
assert blob == 'cf5efadfc0f33300899d9b8a20f762e5f96a2759'
print(json.dumps(dict(python=platform.python_version(), platform=platform.platform(),
    pot=ot.__version__, numpy=np.__version__, scipy=scipy.__version__,
    source_git_blob=blob, source_sha256=hashlib.sha256(raw).hexdigest())))

def run(f, a, b, M, reg, threshold=1e-9, trace=False):
    lines, start = inspect.getsourcelines(f)
    targets = {}
    for offset, line in enumerate(lines):
        s = line.strip()
        if s in ('v = b / KtransposeU', 'v = logb - nx.logsumexp(Mr + u[:, None], 0)'):
            targets[start + offset] = 'after_v'
        if s in ('u = 1.0 / nx.dot(Kp, v)', 'u = loga - nx.logsumexp(Mr + v[None, :], 1)'):
            targets[start + offset] = 'after_u'
        if s == 'v = vprev':
            targets[start + offset] = 'restored'
    assert list(targets.values()).count('after_v') == 1
    assert list(targets.values()).count('after_u') == 1
    events, previous = [], None
    def observer(frame, event, arg):
        nonlocal previous
        if frame.f_code is not f.__code__:
            return None
        if event in ('line', 'return'):
            if previous in targets:
                d = frame.f_locals
                events.append((int(d['ii']), targets[previous], d['u'].copy(), d['v'].copy()))
            previous = frame.f_lineno if event == 'line' else None
        return observer
    old = sys.gettrace()
    with warnings.catch_warnings(record=True) as caught:
        warnings.simplefilter('always')
        try:
            if trace:
                sys.settrace(observer)
            T, log = f(a, b, M, reg, numItermax=1000, stopThr=threshold, log=True, warn=True)
        finally:
            sys.settrace(old)
    return T, log, [(w.category.__name__, str(w.message)) for w in caught], events

def same(x, y):
    if isinstance(x, dict):
        return x.keys() == y.keys() and all(same(x[k], y[k]) for k in x)
    return np.array_equal(np.asarray(x), np.asarray(y), equal_nan=True)

def measures(T, a, b):
    return dict(plan=T.tolist(), rows=T.sum(1).tolist(), columns=T.sum(0).tolist(),
        mass=float(T.sum()), row_l1=float(abs(T.sum(1)-a).sum()),
        column_l1=float(abs(T.sum(0)-b).sum()), finite=bool(np.isfinite(T).all()))

cases = [
    ('symmetric100', [50,50], [50,50], [[0,1],[1,0]], 1),
    ('rectangular100', [20,30,50], [40,60], [[0,1],[1,0],[.5,.2]], .3),
    ('rectangular_quarter', [.05,.075,.125], [.1,.15], [[0,1],[1,0],[.5,.2]], .3),
    ('tiny_reg100', [90,10], [10,90], [[0,1],[1,0]], 1e-4),
    ('zero_support100', [100,0], [0,100], [[0,1],[1,0]], 1),
]
for name, aa, bb, mm, reg in cases:
    a,b,M = [np.array(x,dtype=np.float64) for x in (aa,bb,mm)]
    results = []
    for f in (source.sinkhorn_knopp, source.sinkhorn_log):
        T,log,ws,_ = run(f,a,b,M,reg)
        Q,qlog,qws,events = run(f,a,b,M,reg,trace=True)
        assert same(T,Q) and same(log,qlog) and ws == qws
        rejected = {i for i,p,_,_ in events if p == 'restored'}
        snapshots = []
        for i,p,u,v in events:
            if i not in ({0,log['niter']} | rejected):
                continue
            with np.errstate(all='ignore'):
                P = (u[:,None]*np.exp(M/(-reg))*v[None,:]
                     if f is source.sinkhorn_knopp else np.exp(-M/reg+u[:,None]+v[None,:]))
                m = measures(P,a,b)
            snapshots.append(dict(index=i, phase=p, rejected=i in rejected and p!='restored',
                u=u.tolist(), v=v.tolist(), **m))
        m = measures(T,a,b)
        mass = float(a.sum())
        N,nlog,nws,_ = run(f,a/mass,b/mass,M,reg,threshold=1e-9/mass)
        summary = dict(case=name,solver=f.__name__,parity=True,niter=int(log['niter']),
            trace_events=len(events),accepted_pairs=log['niter']+1-len(rejected),
            sampled_errors=[float(e) for e in log['err']],warnings=ws,
            normalized_niter=int(nlog['niter']),rescaled_max_difference=float(abs(T-mass*N).max()),
            cost=float(np.sum(T*M)),**m,snapshots=snapshots)
        print(json.dumps(summary,allow_nan=True))
        results.append(T)
    print(json.dumps(dict(case=name,cross_solver_max_difference=float(abs(results[0]-results[1]).max()))))
```

