# Local verification evidence

This document records local evidence collected on 2026-09-20 for LIB-05. It is
not a release, remote-CI, registry-publication, or merged-state claim.

## Environment

| Component | Observed value |
| --- | --- |
| Source baseline | `fb84d02812dc5ea27900ce4bdccbd708e0fad407` before LIB-05 changes |
| Host | macOS 26.5, arm64 |
| .NET | SDK 10.0.201 (`4d3023de60`), runtime 10.0.5 (`a612c2a105`) |
| Docker | Docker Desktop 4.44.3, Engine 28.3.2, Linux/aarch64 daemon |
| Docker Buildx | 0.26.1-desktop.1 |
| Python fixture environment | Python 3.12.3; POT 0.9.6.post1; NumPy 2.2.6; SciPy 1.15.3 |

The explicit host .NET executable was
`/Users/jayaramanvenkatesan/.dotnet/dotnet`. Sandboxed MSBuild IPC hung without
output, so the recorded .NET verification was run with the same executable
outside the sandbox. The locked restore then completed normally.

## Usage acceptance TDD

The checked-in `tests/usage-smoke.sh` acceptance was written first. A minimal
console project and compilable placeholder supplied the executable seam; the
failure was behavioral rather than a missing project or SDK.

RED command:

```sh
DOTNET_COMMAND=/Users/jayaramanvenkatesan/.dotnet/dotnet tests/usage-smoke.sh
```

Relevant result (exit 1):

```text
Usage example not implemented.
Usage example exited unsuccessfully.
```

After implementing the console against the real library, the same command was
GREEN (exit 0):

```text
Basic: usable
LogDomain: usable
Basic zero support: NumericalBreakdown
```

The acceptance would fail if the executable exited nonzero or if any required
line were missing.

## Final local checks

All commands below completed with exit code 0 from the repository root.

| Command | Observed result |
| --- | --- |
| `/Users/jayaramanvenkatesan/.dotnet/dotnet restore --locked-mode` | All three projects restored from lock files |
| `/Users/jayaramanvenkatesan/.dotnet/dotnet test -c Release` | 76 passed, 0 failed, 0 skipped |
| `/Users/jayaramanvenkatesan/.dotnet/dotnet format --verify-no-changes` | No output or formatting differences |
| `/Users/jayaramanvenkatesan/.dotnet/dotnet build -c Release` | Build succeeded with 0 warnings and 0 errors |
| `DOTNET_COMMAND=/Users/jayaramanvenkatesan/.dotnet/dotnet tests/usage-smoke.sh` | Printed all three required lines shown above |
| `/private/tmp/sinkhorn-evidence.iX5GRK/venv/bin/python reference/generate_fixtures.py` followed by `git diff --exit-code -- tests/Sinkhorn.Tests/Fixtures` | Regenerated all three fixture files with no tracked difference |
| `/opt/homebrew/bin/docker build -t sinkhorn-library-check .` | Built final image `sha256:a739d54a9ab4e2d091ffa267dfc0b9c61aaaa174bed9af392bbd51a4003f9025` |
| `/opt/homebrew/bin/docker run --rm sinkhorn-library-check` | Printed all three required lines shown above |

The final native image inspection reported `os=linux architecture=arm64`.
`/app/licenses/LICENSE` and `/app/licenses/THIRD-PARTY-NOTICES.md` were both
present in the distribution image.

## Base-image and architecture evidence

`docker buildx imagetools inspect` resolved these official Microsoft Container
Registry manifest lists before the Dockerfile was written:

| Image | Manifest list | Relevant child manifests |
| --- | --- | --- |
| `mcr.microsoft.com/dotnet/sdk:10.0.201-noble` | `sha256:127d7d4d601ae26b8e04c54efb37e9ce8766931bded0ee59fcd799afd21d6850` | amd64 `sha256:c804b88aaca8d6b76f65998427adec3101dcbbf2e52e705e2509eb81263aab0c`; arm64 `sha256:ba9a05e2c5ce6eec597c6e8fe23815b1f11180d8588cf6877ad329bd8597921d` |
| `mcr.microsoft.com/dotnet/runtime:10.0.5-noble` | `sha256:d899417078f6f2ace195c70bd63c4851f4c2e29c38f50fcfb46f2be5f7e3638f` | amd64 `sha256:c0efc49409dfa448be3a77856d37b148489ff1af6b792f4da9aeb355c2a10dbd`; arm64 `sha256:1a8fd0e4393a5387dcb98a2cbab833cf1cfb298c55906d7f1122e782a8cfc6ce` |

Two Linux architectures were executed from the final Dockerfile:

- `linux/arm64`: built and ran natively on the Linux/aarch64 Docker daemon.
  The final image reported .NET 10.0.5, RID `linux-arm64`, and Ubuntu 24.04.4.
- `linux/amd64`: `docker build --platform linux/amd64` and
  `docker run --platform linux/amd64` both succeeded under Docker Desktop
  emulation on the arm64 host. The final image was
  `sha256:1f63be8f31a354d1a7b6074a44d79c796ad57ea49250550bb126cf2e992de4b9`,
  reported `architecture=amd64`, and .NET reported RID `linux-x64`.

This is native arm64 and emulated amd64 evidence. No native amd64 machine and no
arm/v7 runtime were tested, even though the pinned manifest lists advertise
those platforms.

## Dependency, license, and provenance audit

- The library and usage lock files contain only project references and no
  third-party NuGet runtime package.
- The test lock file resolves 22 NuGet packages. Installed `.nuspec` metadata
  was inspected for every locked version: 12 report MIT and 10 report
  Apache-2.0. The exact package/version groups and upstream repositories are in
  `THIRD-PARTY-NOTICES.md`.
- The final Ubuntu runtime image contains 97 dpkg packages. Docker SBOM 0.6.0
  with Syft cataloged the same 97 Debian packages. It parsed one or more license
  identifiers for 95; the two gaps were checked manually in the image:
  `libcrypt1` records LGPL-2.1-or-later as its overall license, and OpenSSL's
  installed `libssl3t64` copyright file records Apache-2.0 plus file-specific
  exceptions. Package copyright files remain under `/usr/share/doc`.
- The runtime image contains `/usr/share/dotnet/LICENSE.txt` and
  `/usr/share/dotnet/ThirdPartyNotices.txt`. The project and complete POT MIT
  notice are additionally copied to `/app/licenses`.
- POT source/version/commit/blob provenance, upstream file authors, translation
  status, and Cuturi/Feydy citations are retained in
  `THIRD-PARTY-NOTICES.md`. Fixture generation rechecked the pinned source blob.
- CI-only `actions/checkout` 7.0.1 and `actions/setup-dotnet` 6.0.0 were checked
  against their versioned MIT license files. The workflow was not executed on a
  remote GitHub runner during this local ticket.

No NuGet publication, registry push, PR, merge, account creation, or paid
service was performed.
