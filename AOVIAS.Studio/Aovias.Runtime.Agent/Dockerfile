# syntax=docker/dockerfile:1
# Build context must be the AOVIAS platform root (repository root).

ARG IMAGE_VERSION=1.0.0
ARG BUILD_CONFIGURATION=Release
ARG SOURCE_REVISION=unknown
ARG BUILD_UTC=unknown

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src

COPY AOVIAS.Studio/Aovias.Configuration/ AOVIAS.Studio/Aovias.Configuration/

COPY AOVIAS.Studio/Aovias.Deployment.Contracts/ AOVIAS.Studio/Aovias.Deployment.Contracts/

COPY AOVIAS.Studio/Aovias.DeploymentValidation/ AOVIAS.Studio/Aovias.DeploymentValidation/

COPY AOVIAS.Studio/Aovias.Orchestration/Directory.Build.props AOVIAS.Studio/Aovias.Orchestration/
COPY AOVIAS.Studio/Aovias.Orchestration/src/ AOVIAS.Studio/Aovias.Orchestration/src/

COPY AOVIAS.Studio/Aovias.Release/Directory.Build.props AOVIAS.Studio/Aovias.Release/
COPY AOVIAS.Studio/Aovias.Release/src/ AOVIAS.Studio/Aovias.Release/src/

COPY AOVIAS.Studio/Aovias.Publishing/Directory.Build.props AOVIAS.Studio/Aovias.Publishing/
COPY AOVIAS.Studio/Aovias.Publishing/src/ AOVIAS.Studio/Aovias.Publishing/src/

COPY AOVIAS.Studio/Aovias.Runtime.Agent/Directory.Build.props AOVIAS.Studio/Aovias.Runtime.Agent/
COPY AOVIAS.Studio/Aovias.Runtime.Agent/src/ AOVIAS.Studio/Aovias.Runtime.Agent/src/

WORKDIR /src/AOVIAS.Studio/Aovias.Runtime.Agent
RUN dotnet restore src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj
RUN dotnet publish src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
ARG IMAGE_VERSION
ARG SOURCE_REVISION
ARG BUILD_UTC
LABEL org.opencontainers.image.title="AOVIAS Runtime Agent"
LABEL org.opencontainers.image.version="${IMAGE_VERSION}"
LABEL org.opencontainers.image.source="https://aovias.local/runtime-agent"
LABEL org.opencontainers.image.revision="${SOURCE_REVISION}"
LABEL aovias.service="runtime-agent"
LABEL aovias.build.utc="${BUILD_UTC}"
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:7080
ENV DOTNET_ENVIRONMENT=Production
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=build /app/publish .
RUN printf '{"serviceId":"runtime-agent","sourceRevision":"%s","buildUtc":"%s","deploymentApi":true,"pathPolicyVersion":2,"signalRDiscovery":true,"assembly":"Aovias.Runtime.Agent.Api.dll"}\n' \
    "$SOURCE_REVISION" "$BUILD_UTC" > /app/aovias-build-manifest.json
USER appuser
EXPOSE 7080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:7080/health || exit 1
ENTRYPOINT ["dotnet", "Aovias.Runtime.Agent.Api.dll"]
