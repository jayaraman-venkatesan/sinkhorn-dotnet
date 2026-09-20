# Third-party notices

## Python Optimal Transport (POT)

The solver work in this repository is based on Python Optimal Transport (POT)
0.9.6.post1, pinned at commit
`85113e9a380f5fcf684c50c73c1ff6a164a7366e`. The relevant upstream file is
`ot/bregman/_sinkhorn.py` (Git blob
`cf5efadfc0f33300899d9b8a20f762e5f96a2759`), credited upstream to Remi
Flamary, Nicolas Courty, Titouan Vayer, Alexander Tong, and Quang Huy Tran.
The C# implementation is a translation with explicit validation, result
assessment, and observation behavior specified by this project.

POT is distributed under the MIT License:

> MIT License
>
> Copyright (c) 2016-2023 POT contributors
>
> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all
> copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.

Upstream source and license:

- https://github.com/PythonOT/POT/tree/85113e9a380f5fcf684c50c73c1ff6a164a7366e
- https://github.com/PythonOT/POT/blob/85113e9a380f5fcf684c50c73c1ff6a164a7366e/LICENSE
- Cuturi (2013): https://arxiv.org/abs/1306.0895
- Feydy et al. (2019): https://proceedings.mlr.press/v89/feydy19a.html

## NuGet dependency audit

The class library and console example lock files contain only project
references: they have no third-party NuGet runtime dependencies. The test
project's complete resolved graph was read from
`tests/Sinkhorn.Tests/packages.lock.json` and each installed package's `.nuspec`
license expression was inspected on 2026-09-20.

These packages are used for building or testing and are not runtime dependencies
of the Sinkhorn class library:

| License | Locked packages |
| --- | --- |
| MIT | Microsoft.NET.Test.Sdk 18.5.1; Microsoft.ApplicationInsights 2.23.0; Microsoft.Bcl.AsyncInterfaces 6.0.0; Microsoft.CodeCoverage 18.5.1; Microsoft.Testing.Extensions.Telemetry 1.9.1; Microsoft.Testing.Extensions.TrxReport.Abstractions 1.9.1; Microsoft.Testing.Platform 1.9.1; Microsoft.Testing.Platform.MSBuild 1.9.1; Microsoft.TestPlatform.ObjectModel 18.5.1; Microsoft.TestPlatform.TestHost 18.5.1; Microsoft.Win32.Registry 5.0.0; Newtonsoft.Json 13.0.3 |
| Apache-2.0 | xunit.runner.visualstudio 3.1.5; xunit.v3 3.2.2; xunit.analyzers 1.27.0; xunit.v3.assert 3.2.2; xunit.v3.common 3.2.2; xunit.v3.core.mtp-v1 3.2.2; xunit.v3.extensibility.core 3.2.2; xunit.v3.mtp-v1 3.2.2; xunit.v3.runner.common 3.2.2; xunit.v3.runner.inproc.console 3.2.2 |

Package provenance is retained in the lock file and NuGet package metadata.
Representative upstream repositories are:

- VSTest: https://github.com/microsoft/vstest
- Microsoft Testing Platform: https://github.com/microsoft/testfx
- .NET runtime packages: https://github.com/dotnet/runtime
- Newtonsoft.Json: https://github.com/JamesNK/Newtonsoft.Json
- xUnit: https://github.com/xunit/xunit
- xUnit Visual Studio runner: https://github.com/xunit/visualstudio.xunit

## Container bases

The distribution image uses the official Microsoft .NET images listed below.
The tags identify the selected releases and the digests pin the multi-platform
manifest lists observed from Microsoft Container Registry on 2026-09-20.

| Build role | Image | Manifest-list digest | Advertised Linux platforms |
| --- | --- | --- | --- |
| Build only | `mcr.microsoft.com/dotnet/sdk:10.0.201-noble` | `sha256:127d7d4d601ae26b8e04c54efb37e9ce8766931bded0ee59fcd799afd21d6850` | amd64, arm/v7, arm64 |
| Final runtime | `mcr.microsoft.com/dotnet/runtime:10.0.5-noble` | `sha256:d899417078f6f2ace195c70bd63c4851f4c2e29c38f50fcfb46f2be5f7e3638f` | amd64, arm/v7, arm64 |

The .NET container repository is MIT licensed:
https://github.com/dotnet/dotnet-docker/blob/main/LICENSE. The images are based
on Ubuntu 24.04 (Noble); included operating-system packages retain their own
licenses and copyright files under `/usr/share/doc/*/copyright`. Exact final
image package inventory and observed architecture are recorded in
`docs/verification.md` rather than inferred from the manifest list.

## Continuous-integration actions

The CI workflow uses `actions/checkout` 7.0.1 and `actions/setup-dotnet` 6.0.0.
Both repositories publish an MIT license:

- https://github.com/actions/checkout/blob/v7.0.1/LICENSE
- https://github.com/actions/setup-dotnet/blob/v6.0.0/LICENSE

These actions are CI-only and are not copied into the library or runtime image.
