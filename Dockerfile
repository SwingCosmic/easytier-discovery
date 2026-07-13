# syntax=docker/dockerfile:1

# Build and run on real Linux (amd64/arm64). Preferred deployment target: Kubernetes
# with TUN + NET_ADMIN so EasyTier virtual IPs can be validated with kube-proxy
# (kernel proxy / iptables or ipvs) on the cluster nodes.

ARG DOTNET_VERSION=10.0
ARG EASYTIER_VERSION=2.6.4

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
ARG EASYTIER_VERSION
ARG TARGETARCH

RUN apk add --no-cache curl ca-certificates unzip \
    && case "${TARGETARCH}" in \
         amd64) ARCH=x86_64 ;; \
         arm64) ARCH=aarch64 ;; \
         *) echo "Unsupported TARGETARCH=${TARGETARCH}" >&2; exit 1 ;; \
       esac \
    && ASSET="easytier-linux-${ARCH}-v${EASYTIER_VERSION}.zip" \
    && URL="https://github.com/EasyTier/EasyTier/releases/download/v${EASYTIER_VERSION}/${ASSET}" \
    && echo "Downloading ${URL}" \
    && curl -fsSL -o /tmp/easytier.zip "${URL}" \
    && mkdir -p /opt/easytier \
    && unzip -o /tmp/easytier.zip -d /tmp/easytier-extract \
    && find /tmp/easytier-extract -type f \( -name 'easytier-core' -o -name 'easytier-cli' \) -exec cp {} /opt/easytier/ \; \
    && chmod +x /opt/easytier/easytier-core /opt/easytier/easytier-cli \
    && rm -rf /tmp/easytier.zip /tmp/easytier-extract

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
ARG EASYTIER_VERSION

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
