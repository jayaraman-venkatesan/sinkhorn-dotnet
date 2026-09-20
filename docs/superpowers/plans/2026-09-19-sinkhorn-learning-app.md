# Sinkhorn Learning Application Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development with the orchestrator's executing-plans routing to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a beginner-readable Markdown course and rich interactive app using the actual C# library.

**Architecture:** ASP.NET Core serves a React/TypeScript build and bounded solve
API on one origin. The library is a pinned Git submodule. Shared Markdown and
examples drive static and interactive lessons; solver states and shipment
playback are separate presentation modes.

**Tech Stack:** .NET 10, React/TypeScript/Vite, native SVG/CSS, approved Markdown/math
plugins, Vitest, Playwright, xUnit, Docker Compose; Node 24 build/test-only.

**Spec:** [Approved design](../specs/2026-09-19-sinkhorn-learning-design.md).

## Global Constraints

- Plan/ticket approval and repository approval precede execution. Paths below
  belong to the future app repository, not the orchestration repo.
- Consume [library plan](2026-09-19-sinkhorn-library.md) Tasks 1–5 and its exact public
  interfaces. Never implement a TypeScript numerical solver.
- Website limits: 1–8 sources and destinations, 1,000 update pairs, 200 retained
  trace frames per solver; API response cap 4 MiB; request body cap 256 KiB.
- One active solve, no unbounded queue; comparison sequential; 30-second compute
  deadline; cancellation at update-pair boundaries is not numerical convergence.
- One Compose service, loopback host binding, no database/accounts/cloud/telemetry.
- Preserve approved numerical behavior and rich/accessibility requirements.
- Fresh sequential implementer per ticket; task review checks spec and quality;
  final code-review checks whole app. No parallel implementation workers.
- User approval gates override SDD's general autonomy advice. No unauthorized
  dependency expansion, pushes, PRs, merges or publication. Preserve work on failure.
- Maintain plan-specific ledger with test commands/results, commit ranges,
  reviews and handoffs. Do not mark a task complete merely because code exists.

## Repository map

```text
vendor/sinkhorn/                   pinned library Git submodule
src/Api/                          HTTP adapter, DTOs, bounded trace collector
tests/Api.Tests/                   real-library integration and adapter tests
web/src/contracts.ts              one TypeScript wire schema
web/src/api/                      requests, strict decoding, cancellation
web/src/scenario/                 editor state and cost modes
web/src/playback/                 authoritative display accounting
web/src/scenes/                   SVG, matrix, comparison, controls
web/src/lessons/                  Markdown renderer and scene registry
web/tests/                       Vitest tests
web/e2e/                         Playwright real-service tests
content/lessons/                  six ordinary Markdown chapters
content/examples/                shared versioned JSON scenarios/results
content/figures/                  reproducible static SVGs
scripts/                         content verification and static-figure tooling
docs/adr/                        approved project ADRs
compose.yaml, Dockerfile          one local runtime image
```

Library namespace and solve signature are those in the companion plan.
The submodule's repository URL is resolved only from the approved repository
proposal; do not invent or create a remote during this plan.

## Dependencies and review sequence

APP-01 requires LIB-05; APP-02 requires APP-01; APP-03 requires APP-02;
APP-04 requires APP-03; APP-05 requires APP-04; APP-06 requires APP-05.
One implementer owns each ticket. Changes to earlier shared files are listed
explicitly in the later ticket; reviewers receive the full affected commit range.

### Task 1: Real-library HTTP contract and resource guards (ticket APP-01)

**Files:** Create `src/Api/Program.cs`, `SolveRequest.cs`, `SolveResponse.cs`,
`DiagnosticNumber.cs`, `SolveService.cs`, `BoundedTraceCollector.cs`,
`tests/Api.Tests/{SolveTests,TraceLimitTests,ConcurrencyTests}.cs`, solution/project
configuration. Add approved submodule as `vendor/sinkhorn` and direct project reference.

**Interfaces:** `POST /api/solve` accepts the following wire shape (camelCase):

```json
{"requestId":"example-1","source":[0.5,0.5],"target":[0.5,0.5],
 "costs":[[0,1],[1,0]],"regularization":1,"solver":"Basic",
 "maxIterations":1000,"threshold":1e-9,"traceMode":"Phases"}
```

Response combines `requestId`, `solver`, `referenceVersion`, `options`,
`inputTotals`, `plan`, `transportCost`, `termination`, `lastAttemptedIndex`,
`attemptedPairs`, `acceptedPairs`, `errors`, `scaling`, `checks`, `warnings`,
and `trace`. Result fields use the library meaning; `trace` contains `frames`,
`observedCount`, `omittedCount`, `sampled`, `policy`.
`DiagnosticNumber` serializes double as finite number or `{nonFinite:tag}`.
`GET /api/health` returns `{"status":"ready"}` only when the app is ready.

- [ ] Scaffold the API and test projects with the pinned library reference.
  Add `WebApplicationFactory<Program>` using real `SolveService`; only time and
  concurrency instrumentation may be controlled for deterministic tests.
- [ ] Write an integration test that posts the exact request above, asserts
  HTTP 200, echoed identity, ThresholdMet and `checks.usable=true`. Add malformed
  shape → 400, body >256 KiB → 413, and Basic zero support → HTTP 200 with
  NumericalBreakdown and `usable=false`. Run `dotnet test --filter FullyQualifiedName~SolveTests`
  and capture missing endpoint/behavior red.
- [ ] Implement strict DTO validation and jagged-to-rectangular matrix conversion;
  forward quantities unchanged to `SinkhornSolver.Solve`. Use explicit enum-string
  serialization and a diagnostic converter; converter rule:

```csharp
if (double.IsFinite(value)) writer.WriteNumberValue(value);
else {
    writer.WriteStartObject();
    writer.WriteString("nonFinite", double.IsNaN(value) ? "NaN" :
        double.IsPositiveInfinity(value) ? "PositiveInfinity" : "NegativeInfinity");
    writer.WriteEndObject();
}
```

- [ ] Add one `SemaphoreSlim(1,1)` service gate acquired with zero wait; return
  429 if busy. Run CPU solving off the request event loop while awaiting completion.
  Link RequestAborted with a 30-second cancellation source and release the gate
  in `finally`. Disconnected clients get no synthesized solver result; a deadline
  with a connected client returns HTTP 408 ProblemDetails labelled `compute-timeout`.
  Never map cancellation to ThresholdMet or an unexplained 500.
- [ ] Implement bounded trace retention during observation. Reserve initial,
  last accepted pair and current pair/restoration; fill remaining slots using a
  deterministic stride that doubles when full, evicting interior indices not
  divisible by the new stride. Retained count never exceeds 200; on restoration
  mark current retained trial phases rejected. Track total observed/omitted counts;
  describe exact stride/gaps, not an unqualified “all iterations” label.
  Materialize display matrices from copied coordinates after solving.
- [ ] Serialize to a size-limited buffer before sending headers. Remove optional
  interior trace frames until <=4 MiB while preserving essential failure evidence;
  if mandatory output still exceeds cap return HTTP 500 ProblemDetails labelled
  `response-size-limit`, not partial JSON. Cap metadata/error samples as constrained
  by 1,000 iterations. Do not silently discard the final plan/status.
- [ ] Test deterministic concurrent requests with a barrier (no sleeps), gate
  release on cancellation/failure, response cap with an injectable small test cap,
  actual default cap values, tagged NaN/infinity, phase order/rollback, retention
  through 2,000+ observed phases, and unchanged library results with tracing.
- [ ] Run all API tests, Release build and format checks. Document error/status
  schema. Commit `feat: expose bounded real-library solve API`.

### Task 2: Scenario editor, strict client, and stale-result safety (ticket APP-02)

**Files:** Create frontend scaffold, `web/src/contracts.ts`,
`api/solve.ts`, `scenario/{model,reducer,costs}.ts`, `scenario/ScenarioEditor.tsx`,
`web/src/App.tsx`, `web/tests/{scenario,costs,client}.test.ts`.

**Interfaces:** Use these names throughout later tasks:

```typescript
export type SolverKind = 'Basic' | 'LogDomain';
export type DiagnosticNumber = number | { nonFinite: 'NaN' | 'PositiveInfinity' | 'NegativeInfinity' };
export type Point = { id: string; label: string; x: number; y: number; amount: number };
export type Scenario = { id: string; sources: Point[]; targets: Point[];
  costs: number[][]; costMode: 'Distance' | 'Custom'; regularization: number;
  threshold: number; maxIterations: number };
// SolveResponse mirrors APP-01 exactly; reject malformed fields in decodeSolveResponse.
// solve(request: SolveRequest, signal: AbortSignal): Promise<SolveResponse>
// distanceCosts(sources: Point[], targets: Point[]): number[][]
// scenarioReducer(state: EditorState, action: EditorAction): EditorState
```

`EditorState` contains scenario, monotonically increasing revision, busy flag,
current requestId, optional Basic/LogDomain results, and transport error.
`EditorAction` discriminants are Edit, RunStarted, RunFinished, RunFailed, Cancel.
Edit clears results and increments revision; finished/failed events are accepted
only for the active identity/revision. A Cancel action clears active identity.

- [ ] Add tests before reducer/client implementation:

```typescript
it('uses Euclidean costs without changing supplied quantities', () => {
  const a = [{id:'a',label:'A',x:0,y:0,amount:40}];
  const b = [{id:'b',label:'B',x:3,y:4,amount:40}];
  expect(distanceCosts(a,b)).toEqual([[5]]);
  expect(a[0].amount).toBe(40);
});
```

  Also test RunStarted(old), Edit, RunFinished(old) leaves results absent;
  custom-cost position edits leave costs unchanged. Run `npm test -- --run`
  from `web` and capture red before implementing.
- [ ] Implement distances with `Math.hypot(target.x-source.x,target.y-source.y)`.
  Validate 1–8 entries, finite values and equal totals; show errors without
  silently rebalancing. Custom→Distance transition requires confirmation before
  replacement. Numeric position fields supplement dragging. Runs are explicit.
- [ ] Implement fetch with AbortSignal, strict response decoding, enum checks,
  diagnostic tags and non-200 ProblemDetails. Compare awaits Basic then LogDomain
  with identical serialized numerical settings, preserving independent results.
  Editing/cancelling aborts current fetch and prevents the second comparison call.
- [ ] Render labelled controls for quantity, positions, costs and settings;
  validate server-side limits client-side as guidance, not security. Display
  source/demand totals and stale/transport errors. No playback is enabled yet.
- [ ] Verify tests, `npm run typecheck`, `npm run lint`, `npm run build` and
  real-API manual smoke. Pin approved package versions and npm lockfile; commit
  `feat: add scenario editing and safe solve requests`.

### Task 3: Numerically faithful playback model (ticket APP-03)

**Files:** Create `web/src/playback/{shipment,trace,clock}.ts`,
`web/tests/{shipment,trace,clock}.test.ts`; extend contracts with fully decoded
trace/frame fields from APP-01, without changing the wire schema.

**Interfaces:** `shipmentAt(source:number[], target:number[], costs:number[][],
plan:number[][], progress:number): ShipmentState` returns delivered matrix,
sourceRemaining, targetRemaining, targetReceived and cost.
`selectTraceFrame(frames,index)` returns a retained frame without inventing steps.
`PlaybackState` uses mode `Solver|Shipment`, progress, retainedFrameIndex, playing,
speed; clock actions use absolute elapsed time, not incremental quantity changes.

- [ ] Add a failing invariant test:

```typescript
it('derives every shipment quantity from one progress value', () => {
  const s = shipmentAt([40,60],[50,50],[[1,4],[3,1]],[[40,0],[10,50]],.5);
  expect(s.sourceRemaining).toEqual([20,30]);
  expect(s.targetReceived).toEqual([25,25]);
  expect(s.targetRemaining).toEqual([25,25]);
  expect(s.cost).toBe(60);
});
```

- [ ] Run targeted Vitest tests and record red. Implement accounting from the
  original arrays each time, not accumulated frame deltas:

```typescript
const p = Math.max(0, Math.min(1, progress));
const delivered = plan.map(row => row.map(amount => p * amount));
const sourceRemaining = source.map((a,i) => a - delivered[i].reduce((s,x) => s+x,0));
const targetReceived = target.map((_,j) => delivered.reduce((s,row) => s+row[j],0));
const targetRemaining = target.map((b,j) => b-targetReceived[j]);
const cost = delivered.reduce((s,row,i) => s+row.reduce((v,x,j) => v+x*costs[i][j],0),0);
```

  Validate finite progress and matrix shapes; reject nonfinite plans before
  calling accounting. Do not clamp residuals/round masses to invent conservation.
- [ ] Implement separate trace selection and shipment progress; a UI transition
  to Shipment requires `checks.usable`. Speed/pause/scrub reset the absolute clock
  anchor, never the underlying plan. Label skipped indices and provisional/rejected
  phases; numerical counters are read at retained endpoints, not invented interpolated iterations.
- [ ] Test progress 0/1/arbitrary fractions, scrub backward, replay, pause/resume,
  preserved small residuals, nonfinite plan rejection, zero routes, sampled trace
  gaps and failed-result gating. Run unit/type/lint/build checks. Commit
  `feat: model faithful solver and shipment playback`.

### Task 4: Rich linked scenes and accessible comparison (ticket APP-04)

**Files:** Create `web/src/scenes/{WarehouseScene,PlanMatrix,ResultSummary,
PlaybackControls,ComparisonView}.tsx`, `scenes.css`; modify App/editor integration;
create `web/e2e/{playback,accessibility,comparison}.spec.ts`, Playwright config.

**Interfaces:** Components receive decoded Scenario/SolveResponse/PlaybackState
and selection `{sourceId,targetId}|null`; they never call or implement a solver.
`WarehouseScene` emits route selection; `PlanMatrix` emits the identical selection.

- [ ] Add Playwright test against the running real API:

```typescript
test('keeps failed plans inspectable but disables shipment', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Zero support preset' }).click();
  await page.getByLabel('Solver').selectOption('Basic');
  await page.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(page.getByText('Numerical breakdown', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Play shipments' })).toBeDisabled();
  await expect(page.getByRole('table', { name: 'Transport plan' })).toBeVisible();
});
```

- [ ] Run `npx playwright test playback.spec.ts` and record missing UI red.
- [ ] Implement SVG curved routes and flow ribbons, warehouse/destination fill
  levels and numerical labels. Thickness maps to quantities with a legend;
  decorative particles never determine quantities. Link hover/focus both ways
  between routes and matrix cells; display amount and cost contribution.
  Curved path uses source/target geometry, e.g. `M sx sy Q mx my tx ty`;
  calculate the control point perpendicular to the route, not hardcoded pixels.
- [ ] Add explicit Solver/Shipment tabs, play/pause/speed/scrub/replay and phase
  step controls. Render target L2 charts only at reported sample indices.
  Mark failed/restored phases and sampled gaps; keep both comparison panels'
  statuses and errors visible. No low-cost winner for an infeasible plan.
- [ ] Apply reduced-motion media query and JS preference: stop interpolation/
  flowing ribbons, retain readable states and step buttons. Provide keyboard
  input alternatives, focus styles, non-colour warnings, descriptive tables,
  no per-frame screen-reader announcements, and stacked mobile panels.
- [ ] Add success accounting/route-cell selection, keyboard-only editing,
  reduced-motion, 375px mobile, cancel/edit race and independent-panel tests.
  Run Playwright with actual service, unit/type/lint/build checks. Capture scene
  screenshots for review; no claims from mock screenshots. Commit
  `feat: render linked accessible transport animations`.

### Task 5: Shared Markdown course and reproducible figures (ticket APP-05)

**Files:** Create `content/lessons/01-problem.md` through `06-library-usage.md`,
`content/examples/{balanced,rectangular,zero-support,tiny-regularization}.json`,
`content/figures/`, `web/src/lessons/{LessonPage,SceneRegistry}.tsx`,
`scripts/{verify-content,render-figures}.mjs`, `web/tests/content.test.ts`.

**Interfaces:** Each example JSON has `id`, `title`, `source`, `target`, `costs`,
`regularization`, `reference` (version/hash), and expected named solver outcomes.
Markdown references example IDs with ordinary links; SceneRegistry maps IDs to
approved scene components. Result tables/static figures derive from verified
library/Python fixtures, never separately invented illustrative solver outputs.

- [ ] Write tests that every lesson example link resolves, every ID is unique,
  all result-table inputs match reference fixtures, and each chapter contains
  a readable static equivalent. Run `node scripts/verify-content.mjs` before
  implementation and confirm missing-content red.
- [ ] Write six complete lessons following spec chapters, with original warehouse
  explanations, mathematical detail and citations from the teaching-history note.
  Sample opening content:

```markdown
# Where should the grain go?

Two warehouses have 40 kg and 60 kg of grain. Two destinations each need 50 kg.
A transport plan is a table: each cell says how much one warehouse sends to
one destination. Its row totals must match supply; its column totals must match demand.

Moving grain has a cost. Two plans can satisfy everyone and still cost different
amounts. Sinkhorn solves a version of this problem that also favors spreading
the allocation. That changes the optimization problem—it is not just a shortcut
to the exact cheapest unregularized plan.
```

- [ ] Integrate react-markdown + GFM/math/KaTeX with local assets and safe URL
  handling; no raw HTML execution. Keep scene code out of Markdown. Validate
  shared GitHub/KaTeX formula syntax and relative links; render static figures
  from the same input/result JSON and compare regeneration output for drift.
- [ ] Implement manual allocation lesson using remaining supply/demand constraints,
  confirmation, undo/reset, and the APP-03 accounting model. Compare two valid
  plans; never label regularized output exact unregularized optimum.
- [ ] Add C# README snippets that compile in the library example or a docs test;
  explain failures, kg units, reg/threshold scale, true iterations versus playback,
  historical credit and possible uses. No copied unlicensed paper imagery.
- [ ] Run content verifier, renderer drift check, all frontend checks and browser
  chapter navigation tests; manually check GitHub-equivalent static Markdown and
  formula accessibility. Commit `docs: teach optimal transport from shared lessons`.

### Task 6: One-command distribution and whole-project quality gate (ticket APP-06)

**Files:** Create `Dockerfile`, `compose.yaml`, `.dockerignore`, `.env.example`,
`README.md`, `LICENSE`, `THIRD_PARTY_NOTICES.md`, `docs/verification.md`,
approved `docs/adr/0001-reusable-core.md` through `0004-shared-local-origin.md`,
glossary and CI workflow. Modify API static-file setup and build scripts.

**Interfaces:** `docker compose up --build` exposes website and API at
`http://127.0.0.1:8080`; document `APP_PORT` override. Health endpoint is APP-01.
Node is absent from final runtime; no API key or database is required.

- [ ] Write end-to-end deployment acceptance: GET `/` returns built website,
  `/api/health` is ready, `/api/solve` returns actual library result, and a nested
  lesson route reloads correctly. Run before container creation and record red.
- [ ] Implement multistage build using approved pinned Node/.NET images. Run
  npm ci/build in Node stage, copy built frontend into API `wwwroot` before
  `dotnet publish`, then copy publish output to final ASP.NET runtime. Runtime
  executes `dotnet Api.dll`; serve static files and API before SPA fallback.
  Use nonroot runtime and a loopback-bound Compose mapping:

```yaml
services:
  app:
    build: .
    ports:
      - "127.0.0.1:${APP_PORT:-8080}:8080"
    environment:
      ASPNETCORE_HTTP_PORTS: "8080"
```

- [ ] Document prerequisites, recursive clone/submodule update, build/start/stop,
  tests, examples, API/status semantics, limits, evidence, research provenance,
  relative Markdown assets, fixture regeneration, licensing and architecture.
  `.env.example` contains only `APP_PORT=8080`. Preserve POT/library attribution
  and audit actual locked dependencies, bundled fonts/images and container packages.
- [ ] Run backend tests/format/Release build; frontend unit/lint/type/build;
  content drift check; Compose build/startup and real-service browser suite.
  Build and run Linux arm64 and amd64 where supported; report unavailable runtime
  evidence rather than pretending a cross-build proves runtime success.
- [ ] Capture screenshots/demo media from the real app, verify a clean recursive
  checkout using the documented commands, and record the pinned library commit.
  Commit `build: package and verify the learning application`.
- [ ] Controller performs whole-branch standards/spec review and fresh shipped
  checks. Ask before app/library PRs and merges; after library merge update/pin
  its approved merged commit in app, reverify, then request app integration.
  Never mark shipped before both applicable approved merges and final checks.

## Plan self-review

Spec coverage: repository/runtime boundary APP-01/06; editor/cost modes APP-02;
mass/trace presentation APP-03; rich accessibility APP-04; all chapters/static
content APP-05; licenses/containers/docs/release APP-06. Numerical primitives and
reference parity belong to LIB-01–05. HTTP 408/size-error labels refine approved
error distinctions without changing solver semantics; review them with tickets.
Task interfaces use the library's exact type/entrypoint names and one wire schema.
All code/tests here are planned instructions, not implemented or verified results.
