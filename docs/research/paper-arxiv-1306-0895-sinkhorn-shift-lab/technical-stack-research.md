# Technical stack research

Checked 2026-09-19 for decision ticket #16. This is a recommendation, not
implementation or dependency approval. It preserves the approved
[numerical contract](library-contract-proposal.md) and
[animation experience](animation-playground-proposal.md).

## Recommended stack

| Area | Recommendation | Evidence and reason |
| --- | --- | --- |
| Presentation | React + TypeScript + Vite, plain CSS, local component state/reducer | React documents Vite as an option for a from-scratch app; this bounded local teaching app already has its server in C#. React supports the linked controls, tables, and SVG scene without an additional application framework. This fit assessment is engineering judgment, not a performance benchmark. [React guide](https://react.dev/learn/build-a-react-app-from-scratch), [Vite guide](https://vite.dev/guide/) |
| Build runtime | Node 24 LTS, npm lockfile; choose exact compatible frontend versions at implementation | Node 24 entered LTS and receives updates through April 2028. It satisfies the documented Vite Node floor (20.19+ or 22.12+); check selected packages' engines together when pinning. Node is a build/test dependency, not an extra deployed application server. [Node migration guide](https://nodejs.org/en/blog/migrations/v22-to-v24), [Vite requirements](https://vite.dev/guide/) |
| Numerical library/API | .NET 10 LTS class library and ASP.NET Core local HTTP API, with a direct project reference | Current policy supports .NET 10 until November 14, 2028; .NET 8/9 expire November 10, 2026. Keep the solver reusable and the API responsible for bounded requests, JSON diagnostics, and trace retention. [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy) |
| Rich animation | Native SVG paths, fills, masks and CSS, driven by one explicit playback clock using requestAnimationFrame | SVG defines curved paths and geometry; browsers provide animation-frame callbacks. For at most 64 routes, start with native rendering and measure before adding a library. The adequacy/performance assessment remains to be verified. [SVG paths specification](https://www.w3.org/TR/SVG2/paths.html), [HTML animation frames](https://html.spec.whatwg.org/multipage/imagebitmap-and-animations.html#animation-frames) |
| Shared lessons | Ordinary `.md` files rendered with react-markdown + remark-gfm + remark-math + rehype-katex, KaTeX CSS/fonts bundled locally | The renderer documents the math pipeline; GFM adds tables; GitHub supports dollar-delimited math. Keep JSX and component directives out of shared prose. [react-markdown](https://github.com/remarkjs/react-markdown), [remark-gfm](https://github.com/remarkjs/remark-gfm), [math plugins](https://github.com/remarkjs/remark-math), [GitHub math](https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/writing-mathematical-expressions) |
| Local distribution | One Compose service and multistage image; documented `docker compose up --build` | Build the frontend in a Node stage and API/library in a .NET SDK stage; copy built assets into the API before publish; final ASP.NET runtime serves assets and API on one origin. Compose's build flag builds before starting. [Multistage builds](https://docs.docker.com/build/building/multi-stage/), [Compose command](https://docs.docker.com/reference/cli/docker/compose/up/), [ASP.NET static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0) |
| Verification | xUnit for C#; ASP.NET integration tests; Vitest for pure presentation/accounting logic; Playwright against the running real API/library | ASP.NET documents WebApplicationFactory integration testing; Playwright can start/wait for application servers. Vitest shares Vite configuration. Exact test package versions must be pinned together. [ASP.NET testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0), [Vitest](https://vitest.dev/guide/), [Playwright servers](https://playwright.dev/docs/test-webserver) |

## Boundaries and caveats

The architecture is browser → local C# API → real C# library. TypeScript handles
editing, layout, and presentation arithmetic only; it does not reimplement
Sinkhorn. Use same-origin HTTP in the packaged app and a development proxy if
needed during frontend development. Bind the published host port to loopback
by default. No database, external service, account, second runtime server, or
public deployment is needed by this proposal. A static-only host cannot run
the C# API; public hosting is a separate decision.

The one-command experience assumes a working container engine and Compose,
available port, and network access for the first build's images/packages.
It is not a promise that Docker installation, first build, or offline use
requires no setup. Pin images and dependency lockfiles at implementation;
validate Linux arm64 and amd64 on the intended machines. Copying frontend
assets before .NET publish is significant for build-time static-asset discovery.

Make playback state authoritative: time maps to a progress value and all
quantities derive from that same value. Pause/scrub/replay must not accumulate
rounding drift or depend on particle counts. Use real trace endpoints, label
interpolation, and keep reduced-motion/static states and keyboard controls.
Native SVG is a starting choice; add an animation package only if measured
complexity or performance justifies it. These are design recommendations
derived from the approved experience, not verified implementation results.

Keep one Markdown narrative and stable example identifiers. A website registry
can enhance ordinary example links or associated scenes; the Markdown still
contains the explanation, static figure, and numerical table. Avoid raw HTML
execution in the Markdown renderer. GitHub uses MathJax while this proposal uses
KaTeX, so verify every chosen formula in both and restrict to their shared
syntax; do not promise pixel-identical rendering. Check relative links/images
in both contexts. The renderer's defaults are safe, but plugins and URL
transforms can change that property. [Renderer security guidance](https://github.com/remarkjs/react-markdown#security),
[GitHub math rendering](https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/writing-mathematical-expressions).

Tests should concentrate on pinned Python/C# parity, tracing invariance,
nonfinite serialization, rejected/exhausted results, resource caps, stale
responses, shared fixture drift, mass/cost accounting at arbitrary scrub
positions, keyboard use, reduced motion, and mobile rendering. End-to-end
success must exercise the real library. Browser screenshots alone cannot
establish numerical compatibility. No packages were installed and no runtime,
browser, cross-language, or container tests were run for this research.

## Alternatives considered

Vanilla TypeScript removes React but requires more manual synchronization for
the scenario editor, matrix, comparison panels, and accessible controls.
React/Vite is the recommended balance for this specific interaction design.
A full React framework is useful when its routing/server features are needed;
React's own guide explains those tradeoffs. Here they overlap with the chosen
C# server. MDX would couple the shared lesson text to a JavaScript component
format; ordinary Markdown meets the already-approved repository reading path.
Canvas/WebGL or an animation/chart library should wait for an evidenced need.
These are scope judgments, not claims that the alternatives cannot work.

## Direct license check

The linked upstream license files/readmes were inspected. These are project
level direct-license findings, not an audit of locked packages, transitive
dependencies, container OS packages, bundled fonts, browser binaries, or notices.
Recheck the actual selected releases and retain required notices before release.

| Proposed direct dependency/project | Upstream license |
| --- | --- |
| React / React DOM | [MIT](https://github.com/react/react/blob/main/LICENSE) |
| Vite and its React plugin | [MIT](https://github.com/vitejs/vite/blob/main/LICENSE), [MIT](https://github.com/vitejs/vite-plugin-react/blob/main/LICENSE) |
| TypeScript | [Apache-2.0](https://github.com/microsoft/TypeScript/blob/main/LICENSE.txt) |
| .NET runtime / ASP.NET Core | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [MIT](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt) |
| react-markdown / remark-gfm | [MIT](https://github.com/remarkjs/react-markdown#license), [MIT](https://github.com/remarkjs/remark-gfm#license) |
| remark-math / rehype-katex monorepo | [MIT](https://github.com/remarkjs/remark-math#license) |
| KaTeX | [MIT](https://github.com/KaTeX/KaTeX/blob/main/LICENSE) |
| Vitest | [MIT](https://raw.githubusercontent.com/vitest-dev/vitest/main/LICENSE) |
| Playwright | [Apache-2.0](https://github.com/microsoft/playwright/blob/main/LICENSE) |
| xUnit | [Apache-2.0 with a noted MIT-covered source portion](https://raw.githubusercontent.com/xunit/xunit/main/LICENSE) |

POT source attribution and port licensing remain governed by the separate port
research; this table does not replace that obligation. Tool selection is not
a blanket commercial-license determination for a user's container desktop
product or operating system.
