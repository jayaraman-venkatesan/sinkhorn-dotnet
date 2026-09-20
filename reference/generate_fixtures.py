"""Generate Basic and LogDomain fixtures from the pinned POT implementation."""

from __future__ import annotations

import hashlib
import inspect
import json
import platform
import sys
import warnings
from pathlib import Path
from typing import Any

import numpy as np
import ot
import scipy
import ot.bregman._sinkhorn as sinkhorn_source


POT_COMMIT = "85113e9a380f5fcf684c50c73c1ff6a164a7366e"
SOURCE_GIT_BLOB = "cf5efadfc0f33300899d9b8a20f762e5f96a2759"
EXPECTED_VERSIONS = {
    "pot": "0.9.6.post1",
    "numpy": "2.2.6",
    "scipy": "1.15.3",
}
DEFAULT_THRESHOLD = 1e-9


def git_blob(raw: bytes) -> str:
    header = f"blob {len(raw)}\0".encode()
    return hashlib.sha1(header + raw).hexdigest()


def tagged(value: Any) -> Any:
    if isinstance(value, np.ndarray):
        return tagged(value.tolist())
    if isinstance(value, np.generic):
        return tagged(value.item())
    if isinstance(value, float):
        if np.isnan(value):
            return {"nonFinite": "NaN"}
        if np.isposinf(value):
            return {"nonFinite": "PositiveInfinity"}
        if np.isneginf(value):
            return {"nonFinite": "NegativeInfinity"}
        return value
    if isinstance(value, dict):
        return {key: tagged(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [tagged(item) for item in value]
    return value


def same(left: Any, right: Any) -> bool:
    if isinstance(left, dict):
        return left.keys() == right.keys() and all(
            same(left[key], right[key]) for key in left
        )
    return bool(
        np.array_equal(np.asarray(left), np.asarray(right), equal_nan=True)
    )


def measures(plan: np.ndarray, source: np.ndarray, target: np.ndarray) -> dict[str, Any]:
    rows = plan.sum(axis=1)
    columns = plan.sum(axis=0)
    finite = bool(np.isfinite(plan).all())
    nonnegative = bool((plan >= 0).all())
    return {
        "finite": finite,
        "nonnegative": nonnegative,
        "sourceL1": float(np.abs(rows - source).sum()),
        "targetL1": float(np.abs(columns - target).sum()),
        "totalMass": float(plan.sum()),
    }


def run_case(case: dict[str, Any], solver_name: str) -> dict[str, Any]:
    solver = {
        "basic": ot.bregman.sinkhorn_knopp,
        "log-domain": ot.bregman.sinkhorn_log,
    }[solver_name]
    source = np.asarray(case["source"], dtype=np.float64)
    target = np.asarray(case["target"], dtype=np.float64)
    costs = np.asarray(case["costs"], dtype=np.float64)
    threshold = case.get("threshold", DEFAULT_THRESHOLD)
    max_iterations = case.get("maxIterations", 1000)
    warm_start = case.get("warmStart")
    if warm_start == "rectangular-solution":
        rectangular = next(item for item in CASES if item["name"] == "rectangular")
        base_source = np.asarray(rectangular["source"], dtype=np.float64)
        base_target = np.asarray(rectangular["target"], dtype=np.float64)
        base_costs = np.asarray(rectangular["costs"], dtype=np.float64)
        _, base_log = solver(
            base_source,
            base_target,
            base_costs,
            rectangular["regularization"],
            numItermax=1000,
            stopThr=DEFAULT_THRESHOLD,
            log=True,
            warn=True,
        )
        warm_start = (
            (np.log(base_log["u"]), np.log(base_log["v"]))
            if solver_name == "basic"
            else (base_log["log_u"], base_log["log_v"])
        )
    elif warm_start is not None:
        warm_start = tuple(np.asarray(values, dtype=np.float64) for values in warm_start)

    with warnings.catch_warnings(record=True) as caught:
        warnings.simplefilter("always")
        plan, log = solver(
            source,
            target,
            costs,
            case["regularization"],
            numItermax=max_iterations,
            stopThr=threshold,
            log=True,
            warn=True,
            warmstart=warm_start,
        )

    messages = [str(item.message) for item in caught]
    breakdown = solver_name == "basic" and any(
        "numerical errors at iteration" in message for message in messages
    )
    if breakdown:
        termination = "NumericalBreakdown"
    elif log["err"] and log["err"][-1] < threshold:
        termination = "ThresholdMet"
    else:
        termination = "IterationLimit"

    checks = measures(plan, source, target)
    checks["usable"] = bool(
        termination == "ThresholdMet"
        and checks["finite"]
        and checks["nonnegative"]
        and checks["sourceL1"] < threshold
        and checks["targetL1"] < threshold
    )
    transport_cost = float(np.sum(plan * costs))
    stable_warnings = []
    if termination == "IterationLimit":
        stable_warnings.append("iteration-limit")
    elif termination == "NumericalBreakdown":
        stable_warnings.append("numerical-breakdown")
    if not checks["finite"]:
        stable_warnings.append("nonfinite-plan")
    diagnostic_values = [
        checks["sourceL1"],
        checks["targetL1"],
        checks["totalMass"],
        transport_cost,
        *log["err"],
        *np.ravel(log["u"]),
        *np.ravel(log["v"]),
    ]
    if any(not np.isfinite(value) for value in diagnostic_values):
        stable_warnings.append("diagnostic-overflow")

    attempted_pairs = int(log["niter"]) + 1
    expected = {
        "termination": termination,
        "lastAttemptedIndex": int(log["niter"]),
        "attemptedPairs": attempted_pairs,
        "acceptedPairs": attempted_pairs - int(breakdown),
        "errors": [
            {"index": index * 10, "targetL2": float(value)}
            for index, value in enumerate(log["err"])
        ],
        "plan": plan,
        "sourceScaling": log["u"] if solver_name == "basic" else log["log_u"],
        "targetScaling": log["v"] if solver_name == "basic" else log["log_v"],
        "scalingIsLog": solver_name == "log-domain",
        "checks": checks,
        "transportCost": transport_cost,
        "warnings": stable_warnings,
    }
    fixture_case = {
        key: value
        for key, value in case.items()
        if key != "warmStart"
    }
    fixture_case["threshold"] = threshold
    fixture_case["maxIterations"] = max_iterations
    fixture_case["warmStart"] = (
        None
        if warm_start is None
        else {
            "sourceLogScaling": warm_start[0],
            "targetLogScaling": warm_start[1],
        }
    )
    fixture_case["expected"] = expected
    return tagged(fixture_case)


def run_observed_case(case: dict[str, Any], solver_name: str) -> dict[str, Any]:
    solver = {
        "basic": ot.bregman.sinkhorn_knopp,
        "log-domain": ot.bregman.sinkhorn_log,
    }[solver_name]
    source = np.asarray(case["source"], dtype=np.float64)
    target = np.asarray(case["target"], dtype=np.float64)
    costs = np.asarray(case["costs"], dtype=np.float64)
    threshold = case.get("threshold", DEFAULT_THRESHOLD)
    max_iterations = case.get("maxIterations", 1000)

    lines, start = inspect.getsourcelines(solver)
    targets = {}
    for offset, line in enumerate(lines):
        stripped = line.strip()
        if stripped in (
            "v = b / KtransposeU",
            "v = logb - nx.logsumexp(Mr + u[:, None], 0)",
        ):
            targets[start + offset] = "AfterDestination"
        if stripped in (
            "u = 1.0 / nx.dot(Kp, v)",
            "u = loga - nx.logsumexp(Mr + v[None, :], 1)",
        ):
            targets[start + offset] = "AfterSource"
        if stripped == "v = vprev":
            targets[start + offset] = "Restored"

    assert list(targets.values()).count("AfterDestination") == 1
    assert list(targets.values()).count("AfterSource") == 1

    events = []
    previous = None

    def observer(frame: Any, event: str, _argument: Any) -> Any:
        nonlocal previous
        if frame.f_code is not solver.__code__:
            return None
        if event in ("line", "return"):
            if previous in targets:
                local = frame.f_locals
                events.append(
                    (
                        int(local["ii"]),
                        targets[previous],
                        local["u"].copy(),
                        local["v"].copy(),
                    )
                )
            previous = frame.f_lineno if event == "line" else None
        return observer

    def invoke(trace: bool) -> tuple[np.ndarray, dict[str, Any], list[tuple[str, str]]]:
        previous_trace = sys.gettrace()
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            try:
                if trace:
                    sys.settrace(observer)
                plan, log = solver(
                    source,
                    target,
                    costs,
                    case["regularization"],
                    numItermax=max_iterations,
                    stopThr=threshold,
                    log=True,
                    warn=True,
                )
            finally:
                sys.settrace(previous_trace)
        warning_values = [
            (item.category.__name__, str(item.message)) for item in caught
        ]
        return plan, log, warning_values

    unobserved_plan, unobserved_log, unobserved_warnings = invoke(False)
    observed_plan, log, observed_warnings = invoke(True)
    assert same(unobserved_plan, observed_plan)
    assert same(unobserved_log, log)
    assert unobserved_warnings == observed_warnings

    rejected_indices = {
        index for index, phase, _, _ in events if phase == "Restored"
    }
    retained_indices = {0, int(log["niter"]), *rejected_indices}
    snapshots = []
    for index, phase, source_scaling, target_scaling in events:
        if index not in retained_indices:
            continue
        with np.errstate(all="ignore"):
            if solver_name == "basic":
                plan = (
                    source_scaling[:, None]
                    * np.exp(costs / -case["regularization"])
                    * target_scaling[None, :]
                )
            else:
                plan = np.exp(
                    costs / -case["regularization"]
                    + source_scaling[:, None]
                    + target_scaling[None, :]
                )
        snapshots.append(
            {
                "index": index,
                "phase": phase,
                "rejected": (
                    index in rejected_indices
                    and phase in ("AfterDestination", "AfterSource")
                ),
                "sourceScaling": source_scaling,
                "targetScaling": target_scaling,
                "plan": plan,
            }
        )

    return tagged(
        {
            "name": case["name"],
            "solver": solver_name,
            "source": source,
            "target": target,
            "costs": costs,
            "regularization": case["regularization"],
            "threshold": threshold,
            "maxIterations": max_iterations,
            "observationParity": True,
            "snapshots": snapshots,
        }
    )


RECTANGULAR_COSTS = [[0, 1], [1, 0], [0.5, 0.2]]
SYMMETRIC_COSTS = [[0, 1], [1, 0]]
CASES = [
    {"name": "balanced", "source": [0.5, 0.5], "target": [0.5, 0.5], "costs": SYMMETRIC_COSTS, "regularization": 1},
    {"name": "rectangular", "source": [0.2, 0.3, 0.5], "target": [0.4, 0.6], "costs": RECTANGULAR_COSTS, "regularization": 0.3},
    {"name": "zero_support", "source": [1, 0], "target": [0, 1], "costs": SYMMETRIC_COSTS, "regularization": 1, "extreme": True},
    {"name": "tiny_reg", "source": [0.9, 0.1], "target": [0.1, 0.9], "costs": SYMMETRIC_COSTS, "regularization": 1e-4, "extreme": True},
    {"name": "large_reg", "source": [0.2, 0.8], "target": [0.3, 0.7], "costs": SYMMETRIC_COSTS, "regularization": 1e10},
    {"name": "zero_and_underflow", "source": [1, 0], "target": [0, 1], "costs": SYMMETRIC_COSTS, "regularization": 1e-4, "extreme": True},
    {"name": "all_kernel_underflow", "source": [0.5, 0.5], "target": [0.5, 0.5], "costs": [[1, 2], [2, 1]], "regularization": 1e-4, "extreme": True},
    {"name": "rectangular_warm_start", "source": [0.2, 0.3, 0.5], "target": [0.4, 0.6], "costs": RECTANGULAR_COSTS, "regularization": 0.3, "warmStart": "rectangular-solution"},
    {"name": "rectangular_one_pair", "source": [0.2, 0.3, 0.5], "target": [0.4, 0.6], "costs": RECTANGULAR_COSTS, "regularization": 0.3, "maxIterations": 1},
    {"name": "symmetric100", "source": [50, 50], "target": [50, 50], "costs": SYMMETRIC_COSTS, "regularization": 1},
    {"name": "rectangular100", "source": [20, 30, 50], "target": [40, 60], "costs": RECTANGULAR_COSTS, "regularization": 0.3},
    {"name": "rectangular_quarter", "source": [0.05, 0.075, 0.125], "target": [0.1, 0.15], "costs": RECTANGULAR_COSTS, "regularization": 0.3},
    {"name": "tiny_reg100", "source": [90, 10], "target": [10, 90], "costs": SYMMETRIC_COSTS, "regularization": 1e-4, "extreme": True},
    {"name": "zero_support100", "source": [100, 0], "target": [0, 100], "costs": SYMMETRIC_COSTS, "regularization": 1, "extreme": True},
    {"name": "negative_costs", "source": [0.5, 0.5], "target": [0.5, 0.5], "costs": [[-1, 0], [0, -1]], "regularization": 0.5},
]


def main() -> None:
    actual_versions = {
        "pot": ot.__version__,
        "numpy": np.__version__,
        "scipy": scipy.__version__,
    }
    if actual_versions != EXPECTED_VERSIONS:
        raise RuntimeError(f"Pinned package versions required: {EXPECTED_VERSIONS}; got {actual_versions}")

    raw = Path(sinkhorn_source.__file__).read_bytes()
    actual_blob = git_blob(raw)
    if actual_blob != SOURCE_GIT_BLOB:
        raise RuntimeError(f"Pinned source blob {SOURCE_GIT_BLOB} required; got {actual_blob}")

    provenance = {
        "pythonVersion": platform.python_version(),
        "potVersion": ot.__version__,
        "numpyVersion": np.__version__,
        "scipyVersion": scipy.__version__,
        "potCommit": POT_COMMIT,
        "sourceGitBlob": actual_blob,
        "sourceSha256": hashlib.sha256(raw).hexdigest(),
        "generatorSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    }
    fixture_directory = (
        Path(__file__).parents[1] / "tests" / "Sinkhorn.Tests" / "Fixtures"
    )
    fixture_directory.mkdir(parents=True, exist_ok=True)
    for solver_name, filename in (
        ("basic", "basic.json"),
        ("log-domain", "log-domain.json"),
    ):
        fixture = {
            "provenance": provenance,
            "cases": [run_case(case, solver_name) for case in CASES],
        }
        output = fixture_directory / filename
        output.write_text(
            json.dumps(fixture, indent=2, allow_nan=False) + "\n", encoding="utf-8"
        )
        print(output)

    phase_cases = []
    for name, solver_name in (
        ("rectangular100", "basic"),
        ("rectangular100", "log-domain"),
        ("zero_support100", "basic"),
        ("tiny_reg100", "basic"),
        ("tiny_reg100", "log-domain"),
    ):
        case = next(item for item in CASES if item["name"] == name)
        phase_cases.append(run_observed_case(case, solver_name))
    phase_fixture = {"provenance": provenance, "cases": phase_cases}
    phase_output = fixture_directory / "phase-traces.json"
    phase_output.write_text(
        json.dumps(phase_fixture, indent=2, allow_nan=False) + "\n",
        encoding="utf-8",
    )
    print(phase_output)


if __name__ == "__main__":
    main()
