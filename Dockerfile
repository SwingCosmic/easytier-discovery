# syntax=docker/dockerfile:1
#
# Build and run on real Linux (amd64/arm64). Preferred deployment target: Kubernetes
# with TUN + NET_ADMIN so EasyTier virtual IPs can be validated with kube-proxy
# (kernel proxy / iptables or ipvs) on the cluster nodes.
#
# EasyTier binaries are NOT compiled here. They are fetched from the parent
# EasyTier repository (or any override URL) at image build time.
#
# --- Fetch modes (build-arg EASYTIER_SOURCE) ---
#   actions  (default)  Latest successful GitHub Actions artifact of EasyTier Core
#   release             GitHub Release zip (fork or upstream published assets)
#   url                 Explicit EASYTIER_DOWNLOAD_URL (zip containing the binaries)
#
# --- Typical independent CI / local builds (this repo does not use GHA) ---
#
#   # 1) Recommended: rolling "latest" Release from parent fork main (always overwritten)
#   docker build -t etdiscovery:local \
#     --build-arg EASYTIER_SOURCE=release \
#     --build-arg EASYTIER_GITHUB_REPO=SwingCosmic/EasyTier \
#     --build-arg EASYTIER_VERSION=latest \
#     -f Dockerfile .
#
#   # 2) Latest Core Actions artifact from a branch (needs token; artifacts expire)
#   docker build -t etdiscovery:local \
#     --build-arg EASYTIER_SOURCE=actions \
#     --build-arg EASYTIER_GITHUB_REPO=SwingCosmic/EasyTier \
#     --build-arg EASYTIER_BRANCH=feature/node-type-flags \
#     --secret id=github_token,env=GITHUB_TOKEN \
#     -f Dockerfile .
#
#   # 3) Pin a known Actions run (stable reproduce)
#   docker build -t etdiscovery:local \
#     --build-arg EASYTIER_RUN_ID=1234567890 \
#     --secret id=github_token,env=GITHUB_TOKEN \
#     -f Dockerfile .
#
#   # 4) Official versioned release asset
#   docker build -t etdiscovery:local \
#     --build-arg EASYTIER_SOURCE=release \
#     --build-arg EASYTIER_GITHUB_REPO=EasyTier/EasyTier \
#     --build-arg EASYTIER_VERSION=2.6.4 \
#     -f Dockerfile .
#
#   # 5) Pre-downloaded zip (air-gapped / custom cache)
#   docker build -t etdiscovery:local \
#     --build-arg EASYTIER_SOURCE=url \
#     --build-arg EASYTIER_DOWNLOAD_URL=https://example.com/easytier-linux-x86_64.zip \
#     -f Dockerfile .

ARG DOTNET_VERSION=10.0

# EasyTier fetch defaults (overridable via --build-arg)
ARG EASYTIER_SOURCE=actions
ARG EASYTIER_GITHUB_REPO=SwingCosmic/EasyTier
ARG EASYTIER_BRANCH=main
ARG EASYTIER_WORKFLOW_FILE=core.yml
ARG EASYTIER_RUN_ID=
ARG EASYTIER_ARTIFACT_NAME=
ARG EASYTIER_VERSION=2.6.4
ARG EASYTIER_DOWNLOAD_URL=
ARG EASYTIER_GITHUB_API=https://api.github.com

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY EtDiscovery.Core/EtDiscovery.Core.csproj EtDiscovery.Core/
COPY EtDiscovery.Web/EtDiscovery.Web.csproj EtDiscovery.Web/
RUN dotnet restore EtDiscovery.Web/EtDiscovery.Web.csproj

COPY EtDiscovery.Core/ EtDiscovery.Core/
COPY EtDiscovery.Web/ EtDiscovery.Web/
RUN dotnet publish EtDiscovery.Web/EtDiscovery.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM alpine:3.21 AS easytier
ARG EASYTIER_SOURCE
ARG EASYTIER_GITHUB_REPO
ARG EASYTIER_BRANCH
ARG EASYTIER_WORKFLOW_FILE
ARG EASYTIER_RUN_ID
ARG EASYTIER_ARTIFACT_NAME
ARG EASYTIER_VERSION
ARG EASYTIER_DOWNLOAD_URL
ARG EASYTIER_GITHUB_API
ARG TARGETARCH

# Optional token as build-arg (discouraged: visible in history). Prefer:
#   --secret id=github_token,env=GITHUB_TOKEN
ARG GITHUB_TOKEN=

ENV EASYTIER_SOURCE=${EASYTIER_SOURCE} \
    EASYTIER_GITHUB_REPO=${EASYTIER_GITHUB_REPO} \
    EASYTIER_BRANCH=${EASYTIER_BRANCH} \
    EASYTIER_WORKFLOW_FILE=${EASYTIER_WORKFLOW_FILE} \
    EASYTIER_RUN_ID=${EASYTIER_RUN_ID} \
    EASYTIER_ARTIFACT_NAME=${EASYTIER_ARTIFACT_NAME} \
    EASYTIER_VERSION=${EASYTIER_VERSION} \
    EASYTIER_DOWNLOAD_URL=${EASYTIER_DOWNLOAD_URL} \
    EASYTIER_GITHUB_API=${EASYTIER_GITHUB_API} \
    TARGETARCH=${TARGETARCH} \
    GITHUB_TOKEN=${GITHUB_TOKEN} \
    DEST_DIR=/opt/easytier

COPY docker/fetch-easytier.sh /fetch-easytier.sh

# github_token secret is optional; required=false so release/url builds work offline of GH auth.
RUN --mount=type=secret,id=github_token,required=false \
    apk add --no-cache curl ca-certificates unzip tar jq \
    && sed -i 's/\r$//' /fetch-easytier.sh \
    && chmod +x /fetch-easytier.sh \
    && /fetch-easytier.sh

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends tini ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish/ /app/
COPY --from=easytier /opt/easytier/easytier-core /usr/local/bin/easytier-core
COPY --from=easytier /opt/easytier/easytier-cli /usr/local/bin/easytier-cli
COPY docker/entrypoint.sh /entrypoint.sh
COPY docker/appsettings.docker.json /app/appsettings.Docker.json

RUN sed -i 's/\r$//' /entrypoint.sh \
    && chmod +x /entrypoint.sh /usr/local/bin/easytier-core /usr/local/bin/easytier-cli \
    && mkdir -p /config

# Registry-oriented image defaults. Mode is required (see docs):
#   sidecar | daemon | embedded  (standalone removed -> embedded)
# Registry in cluster: mode=embedded AND bundles EasyTier in-process.
# daemon: does NOT manage EasyTier lifecycle (shared external tunnel).
# Workers: sidecar|daemon + ETDISCOVERY_ROLES. daemon != K8s DaemonSet.
ENV ASPNETCORE_ENVIRONMENT=Docker \
    DOTNET_EnableDiagnostics=0 \
    ETDISCOVERY_MODE=embedded \
    EasyTier__CorePath=/usr/local/bin/easytier-core \
    EasyTier__CliPath=/usr/local/bin/easytier-cli \
    EtDiscovery__Mode=embedded \
    EtDiscovery__ListenUrl=http://0.0.0.0:8080 \
    EtDiscovery__DiscoveryPort=8080

# Discovery HTTP
EXPOSE 8080/tcp
# EasyTier default listeners
EXPOSE 11010/tcp
EXPOSE 11010/udp
EXPOSE 11011/udp
EXPOSE 11011/tcp
EXPOSE 11012/tcp

# K8s / Linux host: mount /dev/net/tun and grant NET_ADMIN (often privileged or
# cap-add NET_ADMIN + SYS_ADMIN depending on node policy). Prefer validating VIP
# reachability on a real Linux cluster with kube-proxy kernel mode enabled.

VOLUME ["/config"]

ENTRYPOINT ["/usr/bin/tini", "--", "/entrypoint.sh"]
CMD []
