# Sinkhorn library and interactive learning application

Date: 2026-09-19. Status: **Approved**.

The human approved the complete review package with “good”; approval is
recorded in the Idea issue against commit
`ba9e42025846c4c372ef55c4f7ed107a60b2190b`. Integration policies and ADR wording
below are included in that approval. Planning is authorized; coding is not yet.

This is the complete first-release design for review, not implementation
authorization. It consolidates the human decisions in the
[design map](https://github.com/jayaraman-venkatesan/research-to-repo/issues/4).
Repository creation, implementation tickets, PR creation, and release retain
their separate approval gates.

## 1. What we are building

A reusable C# library containing Basic and LogDomain Sinkhorn solvers, plus a
TypeScript learning application that calls that actual library. A new software
engineer should understand optimal transport, why this research matters, how
the calculations work, where they fail, and how to use the library elsewhere.

The app teaches with divisible grain moving between warehouses and destinations,
then provides an editable experiment. It is educational decision support, not
a validated production logistics optimizer. The library knows only numbers,
not grain, map coordinates, currencies, or browser state.

Success means a documented, tested, container-runnable learning experience and
an independently usable C# library. No deadline weakens those requirements.

## 2. Authority and detailed contracts

The following approved contracts are normative parts of this specification.
Their original “proposal” filenames are retained to preserve existing links.
This document supplies integration requirements; the referenced numerical and
visual behavior must not be silently weakened during implementation.

- [Numerical contract](../../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/library-contract-proposal.md).
- [Animation/playground contract](../../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/animation-playground-proposal.md).
- [Project vocabulary](../../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/CONTEXT.md).
- [Verified stack and direct-license evidence](../../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/technical-stack-research.md).

New integration policies in sections 6–7 and the ADR wording in section 11 are
proposals included in this spec approval, not earlier separately approved facts.
Historical proposals are superseded by their recorded approvals and this spec.

## 3. Deliverables and repository boundaries

Two separately approved repositories are required:

| Repository role | Contents |
| --- | --- |
| Numerical library | .NET 10 class library, xUnit tests, reference fixtures, usage README, license/notices, glossary and relevant ADRs |
| Learning application | TypeScript website, ASP.NET Core API, shared lessons/examples/static figures, integration/browser tests, container setup and library Git submodule |

The app references the library project at a pinned submodule commit. It never
copies or independently rewrites the solver in TypeScript. Each project can be
understood and tested independently; the app integration verifies the exact
pinned library revision. Submodule updates use a reviewed app change.

Repository names, descriptions and visibility will be presented in the later
repository-creation proposal; those administrative decisions do not prevent
specification or ticket drafting. MIT remains the default subject to actual
release compatibility and notice review. No repositories or packages are
created or published by this specification.

## 4. Learning content and user journey

One ordinary Markdown source and shared example data power both GitHub-readable
lessons and the website. Static figures/tables must explain every important
interactive scene without requiring JavaScript. The website enhances named
scenes through a separate registry; no embedded JSX is required in lesson prose.

Chapters cover:

1. Supply, demand, cost, and a transport plan using small grain examples.
2. Manual allocation and comparison of two feasible plans.
3. Historical milestones and why modern regularized OT is useful.
4. Regularization and alternating updates, first intuitively then mathematically.
5. Basic versus LogDomain, including numerical failures and iteration exhaustion.
6. C# integration, result inspection, practical uses and limitations.

Historical and practical claims follow the
[primary-source teaching note](../../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/teaching-history-research.md).
Do not conflate Monge/Kantorovich transport, 1960s matrix scaling, Cuturi's 2013
computational work, and later debiased divergences. Explain source notation
differences. Do not copy paper figures or example photographs without verified
reuse rights; prefer original synthetic diagrams and examples.

Guided lessons lead to a lab with editable sources/destinations, quantities,
positions, cost matrix, regularization, threshold and iteration budget. All
quantities retain their numerical values, including fractional and zero weights.
Unequal totals block Run rather than silently changing data. Straight-line
map cost and custom cost modes are explicit, with confirmation before replacing
custom costs. A numerical input alternative accompanies every drag interaction.

## 5. Numerical behavior

Port the single-target dense double-precision implementations of
`sinkhorn_knopp` and `sinkhorn_log` from POT 0.9.6.post1, commit
`85113e9a380f5fcf684c50c73c1ff6a164a7366e`. Retain source attribution and notices.
LogDomain does not mean POT's different `sinkhorn_stabilized` solver.

Preserve update order, initialization, warm-start interpretation, Basic rollback,
and checkpoints at indices 0,10,20,… with strict target L2 threshold comparison.
Defaults remain 1,000 update pairs and threshold 1e-9. No implicit normalization,
rounding, support reduction, cost shifting, solver substitution, or fallback.

Reject invalid arguments using the approved validation policy. For a valid call
that exhausts or breaks down numerically, return available diagnostics and plan,
not an exception disguising or discarding the numerical outcome. Stop reason and
plan validity are separate; diagnostic overflow alone is not an invalid plan.
The approved conservative usability flag additionally checks both final L1
marginal errors and finite/nonnegative entries. Transport cost is neither the
regularized objective nor an exact Wasserstein distance or Sinkhorn divergence.

Support non-unit equal total masses directly, with absolute error thresholds
documented in the supplied mass units. Existing
[non-unit/trace evidence](../../research/paper-arxiv-1306-0895-sinkhorn-shift-lab/mass-trace-research.md)
establishes selected Python cases, not C# verification or all-magnitude stability.
Normalization can change numerical failure behavior and is not a transparent fix.

## 6. Runtime, API and resources

Use React/TypeScript/Vite, plain CSS/native SVG, react-markdown, remark-gfm,
remark-math, rehype-katex and locally bundled KaTeX assets. Use .NET 10 LTS and
ASP.NET Core for API/library integration; Node 24 LTS is build/test-only. Pin
compatible exact versions and lockfiles during setup, recheck actual release
licenses, and ask before adding significant dependencies.

Package one multistage image: build frontend in a Node stage, build/publish API
and referenced library with .NET SDK, serve the built website and API together
from the final ASP.NET runtime. One Compose service exposes one loopback-bound
host port. The command is `docker compose up --build`, assuming a compatible
container engine/Compose installation and first-build network access. Document
port conflicts, recursive submodule checkout, build time and offline limitations.
No database, accounts, telemetry, external API or cloud deployment is required.

Proposed concrete integration contract:

- `GET /api/health` reports service readiness without exposing host details.
- `POST /api/solve` accepts a request identity, source/target arrays, cost matrix,
  positive regularization, explicit Basic/LogDomain choice, options and trace mode.
  Array dimensions and all numerical settings are validated server-side.
- Success at the HTTP level includes numerical failure: HTTP 200 carries the
  solver result and honest termination/validity fields. Invalid requests use 400;
  oversize request 413; busy 429; unexpected server failure 500. No stack traces
  or secrets are returned. UI language distinguishes these from numerical outcomes.
- Response includes request identity, solver/provenance, input totals/options,
  plan, transport cost, termination, usability checks, final residuals, sampled
  errors with indices, iteration counts, warnings and bounded trace metadata.
- Strict JSON represents diagnostic numbers as finite numbers or objects with
  `nonFinite` equal to `NaN`, `PositiveInfinity`, or `NegativeInfinity`. No NaN or
  infinity is coerced to zero. Requests must contain finite values except
  explicitly supported warm-start diagnostics, which the website need not expose.
- Website limits: 1–8 source and destination entries, at most 1,000 update pairs,
  at most 200 retained trace frames per solver, 4 MiB response cap. API request
  body cap: 256 KiB. These service limits do not constrain standalone library users.
- Allow one active solve per API instance, with no unbounded queue. The comparison
  view requests solvers sequentially and preserves both independent results.
  Cancel/edit aborts the request; cancellation is checked at update-pair boundaries,
  is not labelled numerical convergence, and creates no completed-shipment result.
  A 30-second request compute deadline uses the same explicit cancellation path.
  Ordinary, uncancelled library computation remains reference-aligned.
- Editing invalidates the current result immediately. Request identity prevents
  late responses from replacing results for newer inputs. No run occurs merely
  because a learner drags a point; Run is explicit.

Test and document Linux arm64/amd64 image builds and startup before claiming
both architectures supported. No public ingress or remote deployment is implicit.

## 7. Trace and animation contract

Actual solver trace and shipment playback are separate modes with clear labels.
Record immutable phase snapshots after destination/source updates; distinguish
rejected attempts and Basic restoration events. Trace collection never changes
live arrays, precision, update order or stopping arithmetic. Do not infer a
mathematical iteration from an interpolated display frame.

Frames identify index, phase, solver coordinate system, rejection/restoration
state and corresponding tentative plan. Retained sampling preserves endpoints
and failure/restoration evidence before evenly sampling interior phases. Expose
retained indices, omitted count and sampling policy. Reject or visibly disclose
incomplete evidence rather than presenting a truncated trace as full history.
Memory retention must be bounded during collection, not only trimmed afterward.
Compute derived display diagnostics from copies, outside solver stopping logic.
Reduce optional frames first to meet response caps; never truncate JSON or drop
the final result/status. Essential-output overflow is an explicit transport error.

Shipment playback requires a usable final plan. One playback clock determines
all delivered quantities, remaining supply/demand and accumulated cost. Visual
particles are decorative, never the numerical source of truth. Preserve small
solver residuals rather than changing the matrix to make labels reach zero.

Linked curved routes, container fill levels, matrix highlighting, running cost,
pause/scrub/replay and phase stepping provide the rich experience described in
the approved visual contract. No misleading performance winner is displayed
when comparing infeasible plans. Reduced-motion users get equivalent static
steps and tables. Keyboard navigation, focus, non-colour indicators, mobile
stacked comparison and deliberate screen-reader updates are required.

## 8. Testing and acceptance mapping

| Area | Required evidence |
| --- | --- |
| Numerical parity | xUnit reference-fixture tests for both solvers: ordinary, rectangular, warm starts, non-unit mass, zero support, tiny/large regularization, exhaustion and rollback |
| Precision | Ordinary normalized fixtures start with entry/cost tolerance `1e-12 + 1e-9*abs(reference)` and marginal L1 under 1e-8; non-unit fixtures also apply approved usability checks in their units; never relax thresholds to hide bugs |
| Validation | Every rejected shape/value/budget case; uniform empty-vector fallback; negative finite costs; accepted mass discrepancy policy |
| Trace | C# phase/rollback fixtures against observed pinned Python; trace on/off equivalence; bounded retention and honest omitted-frame metadata |
| API | Real-library integration, tagged nonfinite JSON, error distinctions, identity/stale results, cancellation, concurrency and resource limits |
| Presentation | Vitest mass/cost accounting at arbitrary scrub positions, shared matrix/route identity, both cost modes and same-input solver comparison |
| End to end | Playwright exercises actual API/library success, exhaustion/failure, edits, replay, keyboard and reduced motion; no fake solver supporting acceptance |
| Content | Markdown and website links/formulas/assets checked; static examples match verified fixtures; provenance and lesson-source drift checks |
| Distribution | Production frontend/.NET build, lint/format/type checks, container build/startup, clean recursive checkout setup, target architecture evidence |

New C# results must be tested; the Python research already run is not a substitute.
Pin reference environment and fixture provenance. Performance and accessibility
claims require measured evidence, not attractive screenshots alone.

## 9. Documentation, licenses and Definition of Shipped

Both repositories need README, compatible license and notices, research citations,
architecture explanation, setup/build/test steps, limitations and examples.
App documentation additionally covers containers, submodules, static lessons,
scene regeneration and screenshots/demo media. Include `.env.example` for
documented configuration only; no credentials are necessary for the default app.

Preserve POT's MIT notice, contributors/file credits, pinned source provenance,
translation changes and relevant Cuturi/Feydy citations. Recheck all locked direct
and transitive dependencies, images, fonts, container packages and assets; the
research's direct-license table is not a finished distribution audit.

**Shipped** requires passing tests, lint, type checking, production builds,
container build/startup, documented setup, example data, compatible licenses,
README/supporting docs, screenshots/demo media, and approved merged PRs with
verification of the merged state. Each repository must meet its applicable
checks, and the app must pin the verified merged library commit. A successful
unit test run or unmerged PR is not shipment. No automatic package publication.

## 10. Non-goals and delivery workflow

No full POT toolbox, GPU/autodiff/batching, unbalanced transport, separate
TypeScript solver, guaranteed convergence, production routing, hosted accounts,
database, paid services or changes to existing personal repositories.
The warehouse app demonstrates the library; broader examples explain possible
uses without adding unapproved applications to this first release.

After human spec approval: produce a dependency-aware implementation plan and
tickets using the workflow's planning skill, then obtain ticket and repository
approvals. Implementation uses subagent-driven development, isolated worktrees,
test-first slices, and standards/spec review. Ask before opening PRs; merge and
release authority remain separate. Preserve unfinished work and report failures;
retry a clearly transient failure once. No code is implemented by approving a
stack or drafting this document.

## 11. Proposed project ADRs for approval with this specification

These record consequential approved directions in concise Matt Pocock format;
their wording is proposed here rather than silently marked accepted. After
approval, place numbered ADRs in the appropriate new project repository.

### ADR 0001 — Use one reusable numerical implementation

The C# library owns numerical computation and lives in its own repository,
included by pinned submodule in the teaching app. A local C# API invokes it
instead of maintaining a browser solver: this costs a backend runtime and
submodule coordination but keeps teaching behavior and reusable-library behavior
identical and independently testable.

### ADR 0002 — Preserve reference behavior, add explicit assessment

Port the pinned Basic and LogDomain update/stopping behavior without silent
normalization, support reduction or automatic fallback. Validate calls and add
explicit result assessment outside that calculation: this exposes inconvenient
failures but preserves reproducibility and makes the numerical limitations teachable.

### ADR 0003 — Separate algorithm states from shipment presentation

Solver traces are tentative numerical states; shipment playback represents a
fixed usable final plan. Keeping these separate requires additional presentation
state but prevents animations from teaching that iterative reassignment means
physical goods were already delivered.

### ADR 0004 — Share lessons and package one local origin

Ordinary Markdown and shared examples supply GitHub and website learning content;
the .NET runtime serves the built TypeScript website and API from one container.
This constrains interactive embedding and independent deployment, but avoids
duplicated prose, runtime Node hosting, and cross-origin local configuration.

## 12. Review result and next gate

Self-review checked scope, cross-contract consistency, explicit resource/error
policies, research/implementation distinction and complete shipped criteria.
The contracts, glossary and research notes above form the review package.
Review the new integration policies and ADR wording along with the consolidated
requirements. Approval permits planning/ticket drafting only; it does not create
repositories or start implementation.
