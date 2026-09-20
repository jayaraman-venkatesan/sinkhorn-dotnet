# Animation and playground — approved design

The human approved this design with “good” after reviewing the proposal.
The linked decision records approval of the version at commit
`461d74ff3213ec6ffb952bc8978a55dc70641dd7`. Proposed behaviors and limits below
are accepted design requirements; implementation and verification remain pending.

Review: [animation and playground experience](https://github.com/jayaraman-venkatesan/research-to-repo/issues/15).
Design only; not an implementation or dependency approval. Builds on approved
shared Markdown lessons, TypeScript presentation, local C# API, and the
[numerical contract](library-contract-proposal.md). Numerical behavior is grounded
in the [paired Python evidence](basic-solver-comparison.md).

## Recommendation and alternatives

Recommend a guided story followed by a small editable lab, with two separate
visual modes: solver steps and final shipments. An always-visible technical
dashboard would expose more detail immediately but overwhelm new engineers.
A cinematic-only story would be attractive but would not let learners inspect
the real algorithm or reuse the examples. The proposed combination preserves
both understanding and experimentation without mixing up what is happening.

## The learning journey

1. **Meet the problem.** Warehouses contain grain; destinations need it. Show
   kilograms as numbers and fill levels. Start with two of each, then grow to
   a small asymmetric example. The text explains supply, demand, and a plan.
2. **Try a plan.** Select a route and type an amount or use an accessible slider.
   Preview the shipment, then confirm. Source stock drops, destination demand
   drops, received grain rises, and route cost accumulates. Reject allocations
   exceeding remaining stock/demand. Reset and undo support experimentation.
3. **Compare two valid plans.** Keep the same problem, show different allocations
   and costs, and explain why merely satisfying demand is not enough. Do not
   present the regularized Sinkhorn plan as the exact cheapest unregularized plan.
4. **Understand the research.** Shared text introduces the history and the
   spreading/regularization idea before formulas. A “show the mathematics”
   section explains the objective and updates with linked symbols. Historical
   claims still require primary-source research; this proposal invents no dates.
5. **Watch the solver think.** Play or step through actual returned intermediate
   states. Routes and matrix cells change together; row/column totals reveal
   what is currently satisfied and what is not. These are tentative plans,
   not goods already shipped.
6. **Ship the final plan.** Only a result passing the approved usability policy
   enables completed-shipment playback. Finish with the same numerical inputs,
   C# usage, result checks, and an invitation to edit the scenario in the lab.

## Rich motion with numerical meaning

- Grain ribbons flow along curved routes; a moving highlight makes direction
  clear. Route thickness reflects assigned quantity with a readable legend.
- Warehouse containers empty and destination containers fill during shipment
  playback. Exact displayed counters derive from the same shipment amounts,
  not decorative particle counts. Particles are illustrative, not individual kg.
- Hovering or focusing a route highlights its matrix cell, source, destination,
  quantity, and cost contribution; focusing the cell performs the same action.
- A cumulative cost display grows with transported quantities. The final value
  equals the returned plan's transport cost within documented display rounding.
- Pause, speed control, step forward/back, scrub, replay, and reset are explicit
  controls. No scroll-jacking, flashing effects, or motion required to read text.

For shipment progress p from 0 to 1, each route's delivered amount is p times
its final allocation. Source remaining is original supply minus outgoing
delivered mass; destination unmet demand is original demand minus incoming
delivered mass. The flow cannot create or lose mass. Small solver residuals are
shown as such rather than secretly rewriting the final matrix to force zeros.
Rounding is presentation-only and never feeds the solver.

## Solver view is not shipment playback

The solver view uses real trace snapshots, tagged with solver, zero-based
iteration, and phase (after destination update or after source update).
Basic shows scaling values; LogDomain shows log-scaling values. A plain-language
caption explains the current phase. Units and coordinates are never mixed.

Animate transitions between discrete snapshots only as visual interpolation;
label the actual sampled endpoints. Do not describe interpolated frames as
extra mathematical iterations. Display residual charts at the recorded checks,
not invented errors for every rendered frame. Optional detail panels expose
matrix values and formulas without making them prerequisites for the story.

If Basic rejects an attempted update, mark it rejected and show the restored
state. It is not accepted progress. Trace capture copies data and must not
mutate arrays, round computation, or change stopping arithmetic. Verify this
against an instrumented pinned Python reference before trusting the animation.

## Editable lab and fair comparison

Proposed website bounds: 1–8 sources and 1–8 destinations; maximum 1,000 update
pairs per solver request. These are UI/API resource limits, not new library
limits or measured performance guarantees. Start with useful presets, including
ordinary success, zero weights, and difficult small regularization.

Learners can add/remove or move points, edit kilograms, edit costs, choose
Basic/LogDomain/Compare, and change regularization, threshold, and iteration
budget. Supply and demand totals remain visible. Unequal totals block Run with
a clear explanation; nothing silently changes another quantity.

Default cost mode is labelled straight-line distance in arbitrary map units,
computed from shared coordinates. It is not a real road-network estimate.
Switching to a custom cost table is explicit: moving points then changes only
layout, not costs. Switching back previews and confirms replacement of custom
costs. The generic library receives the resulting numeric cost table only.

Run is explicit; editing stops playback and marks the previous result stale.
Late responses for older inputs cannot overwrite current state. Compare submits
the same costs, weights, regularization, budget, and threshold to both solvers;
their default initialization remains reference-aligned. Do not imply identical
trajectories, equal run times, or that LogDomain always converges.

Both comparison panels show requested/actual totals, status, iterations, and
transport cost. A low cost on an infeasible result is labelled misleading, not
highlighted as a winner. A failed panel remains inspectable while the other can
show success. Mobile layouts stack panels with the same scenario and controls.

## Bounded trace and response policy

Propose at most 200 retained trace frames per solver and a 4 MiB response limit
per request, enforced by the API. The solver runs unchanged even when display
frames are sampled: keep initial/final state and first/last relevant frames,
then deterministically sample interior frames. Indicate gaps with iteration
labels and a visible “sampled trace” notice. Do not imply step controls show
every update when frames were omitted. Preserve breakdown/rollback evidence
or disclose when it cannot be represented within the cap.

Do not truncate JSON or drop final status/plan to squeeze in trace frames.
Reduce trace retention first and mark truncation. If essential output cannot
fit, return an explicit response-size error. Trace settings do not alter the
library's numerical iteration limit. Bound concurrent API work; excessive load
returns a clear busy response. Detailed cancellation/time-budget behavior is
implementation-spec work and must not be disguised as solver convergence.

JSON cannot represent NaN or infinity as ordinary numbers. Define explicit
tagged diagnostic values at the API boundary; never convert them to zero.
The library retains its numerical values. The UI renders “not finite” and
disables shipment playback when checks fail. Transport/network errors are
distinct from a numerical solver result. These boundary contracts need tests.

## Accessibility and matching Markdown

All editing and playback controls work by keyboard. Provide labelled inputs,
visible focus, sufficient contrast, and non-colour status indicators. Screen
reader summaries update on deliberate steps and completion, not every frame.
Provide numerical alternatives to dragging. Reduced-motion mode replaces grain
flow and interpolation with static before/after states and step controls.

Shared Markdown carries explanations, formulas, example identifiers, tables,
and static figures. The website enhances named scenes using those same
examples. GitHub readers never need custom JavaScript to understand a chapter.
Generated illustrations and published result tables must come from verified
fixtures, with regeneration and drift checks documented. No duplicate solver
or independently maintained second lesson narrative.

## Acceptance evidence for the eventual implementation

- Shipment accounting preserves the returned plan and mass at pause, scrub,
  replay, and completion; displayed cost matches the same delivered amounts.
- Routes, tables, counters, and formulas identify the same scenario/cell.
- Tracing enabled/disabled produces the same numerical computation and result.
- Failed, exhausted, stale, nonfinite, and sampled-trace states remain honest.
- Compare uses identical problem inputs; neither panel overwrites the other.
- Trace caps, response-size handling, and out-of-order responses are tested.
- Keyboard, reduced-motion, mobile layout, and static Markdown paths are checked.
- End-to-end tests exercise the real C# API/library, not a TypeScript imitation.

This proposal approves behavior and limits if accepted, not a particular visual
style, frontend framework, animation package, or final project specification.
