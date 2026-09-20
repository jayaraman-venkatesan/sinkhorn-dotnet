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

## Development and test packages

These packages are used for building or testing and are not runtime dependencies
of the Sinkhorn class library:

| Package | Pinned version | License | Source |
| --- | --- | --- | --- |
| Microsoft.NET.Test.Sdk | 18.5.1 | MIT | https://github.com/microsoft/vstest |
| xunit.v3 | 3.2.2 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0 | https://github.com/xunit/visualstudio.xunit |

Package versions, supported frameworks, repositories, and license expressions
were checked against their NuGet package pages before pinning.
