# Sinkhorn implementation tickets — approval draft

Status: **Approved**. The human replied “looks good” to the complete plans and
11-ticket review package at commit `10d04c39a87824ab39d75fdcd084ff00d6f4dfdf`.
Specification approved; no implementation
issues or project repositories created. These stable IDs become project issues
only after approval and repository readiness. Titles and acceptance criteria
come from the corresponding complete task, not this summary alone.

| ID | Deliverable | Depends on | Task |
| --- | --- | --- | --- |
| LIB-01 | Validated inputs and stable numerical primitives | Repository ready | [Library task 1](2026-09-19-sinkhorn-library.md#task-1-validated-numerical-inputs-and-stable-primitives-ticket-lib-01) |
| LIB-02 | Basic solver, fixtures and honest result assessment | LIB-01 | Library task 2 |
| LIB-03 | LogDomain solver and Python parity | LIB-02 | Library task 3 |
| LIB-04 | Immutable phase traces and cancellation | LIB-03 | Library task 4 |
| LIB-05 | Standalone usage, container, notices and verification | LIB-04 | Library task 5 |
| APP-01 | Actual-library API, JSON, trace/resource bounds | LIB-05; app repository ready | [App task 1](2026-09-19-sinkhorn-learning-app.md#task-1-real-library-http-contract-and-resource-guards-ticket-app-01) |
| APP-02 | Scenario editor, client and stale-result safety | APP-01 | App task 2 |
| APP-03 | Correct shipment accounting and trace playback state | APP-02 | App task 3 |
| APP-04 | Rich linked scenes, comparison and accessibility | APP-03 | App task 4 |
| APP-05 | Shared Markdown course, examples and static figures | APP-04 | App task 5 |
| APP-06 | One-command app distribution and final evidence | APP-05 | App task 6 |

## Execution contract

Use the already selected subagent-driven approach; do not ask the user to choose
it again. The controller dispatches one fresh implementer per task, then a
task-scoped reviewer covering spec compliance and quality. Review fixes return
to that implementer; a final standards/spec review covers each repository.
No parallel implementers modifying shared code; independently useful research
or read-only checks may be delegated when permitted.

Each task includes files, interfaces, concrete test examples, red/green commands,
implementation steps, documentation, and commit/review criteria. Commits remain
on isolated feature branches. The repository workflow's approval rules take
precedence over generic skill suggestions to continue autonomously or publish.
Skill model routing is applied at execution using the actual available models;
no agents are dispatched for implementation during planning.

## Approval boundary

Approve both linked plans and these 11 tickets, or request changes. Approval
permits preparing the concrete repository proposals next. It does not authorize
creating public repositories, coding, opening PRs or publishing packages.

After repository approval, create implementation issues in their respective
project repositories, wire native dependencies, and retain this ID-to-issue
mapping. Cross-repository readiness is documented with the approved library
commit and project links. The orchestration repo is not the application code host.
