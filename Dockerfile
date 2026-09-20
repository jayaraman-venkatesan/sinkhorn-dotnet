FROM mcr.microsoft.com/dotnet/sdk:10.0.201-noble@sha256:127d7d4d601ae26b8e04c54efb37e9ce8766931bded0ee59fcd799afd21d6850 AS build

WORKDIR /source

COPY global.json Directory.Build.props ./
COPY src/Sinkhorn/Sinkhorn.csproj src/Sinkhorn/packages.lock.json src/Sinkhorn/
COPY examples/Usage/Usage.csproj examples/Usage/packages.lock.json examples/Usage/
RUN dotnet restore examples/Usage/Usage.csproj --locked-mode

COPY src/Sinkhorn/ src/Sinkhorn/
COPY examples/Usage/ examples/Usage/
RUN dotnet publish examples/Usage/Usage.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0.5-noble@sha256:d899417078f6f2ace195c70bd63c4851f4c2e29c38f50fcfb46f2be5f7e3638f

WORKDIR /app
COPY --from=build /out/ ./
COPY LICENSE THIRD-PARTY-NOTICES.md /app/licenses/

ENTRYPOINT ["dotnet", "Usage.dll"]
