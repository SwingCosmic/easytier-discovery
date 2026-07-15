#!/usr/bin/env sh
set -eu

# Container entrypoint for EtDiscovery.Runtime.
# Configuration precedence (ASP.NET + bootstrap):
#   1) CLI args after --
#   2) ETDISCOVERY_ROLES / ETDISCOVERY_roles (required unless --roles)
#   3) ETDISCOVERY_MODE (required unless --mode or config;
#      registry image Dockerfile defaults to embedded)
#   4) ETDISCOVERY_CONFIG_FILE or --config-file (optional ConfigMap mount)
#   5) Hierarchical env: EtDiscovery__*, EasyTier__*
#   6) /app/appsettings.json + appsettings.Docker.json
#
# Mode (sidecar|daemon|embedded) is required for lifecycle / EasyTier policy:
#   daemon   = do NOT host EasyTier (external shared tunnel)
#   sidecar/embedded = host/bundle EasyTier (else nothing starts it)
# "daemon" is business sharing semantics, NOT Kubernetes DaemonSet.
# "standalone" removed (old name for embedded).
# Role×mode allow/deny is documentation-only; no topology validation here.

if [ -z "${ETDISCOVERY_ROLES:-${ETDISCOVERY_roles:-}}" ]; then
  # Allow bare ROLES for compact K8s env blocks
  if [ -n "${ROLES:-}" ]; then
    export ETDISCOVERY_ROLES="${ROLES}"
  fi
fi

if [ -z "${ETDISCOVERY_MODE:-${ETDISCOVERY_mode:-}}" ]; then
  if [ -n "${MODE:-}" ]; then
    export ETDISCOVERY_MODE="${MODE}"
  fi
fi

# Compat: old env names → Mode
if [ -z "${ETDISCOVERY_MODE:-}" ]; then
  if [ -n "${ETDISCOVERY_RUNTIME_MODE:-}" ]; then
    export ETDISCOVERY_MODE="${ETDISCOVERY_RUNTIME_MODE}"
  elif [ -n "${RUNTIME_MODE:-}" ]; then
    export ETDISCOVERY_MODE="${RUNTIME_MODE}"
  fi
fi

# Compat: standalone → embedded
case "${ETDISCOVERY_MODE:-}" in
  standalone|STANDALONE)
    export ETDISCOVERY_MODE=embedded
    ;;
esac

if [ -n "${ETDISCOVERY_CONFIG_FILE:-}" ]; then
  export ETDISCOVERY_config_file="${ETDISCOVERY_CONFIG_FILE}"
elif [ -n "${CONFIG_FILE:-}" ]; then
  export ETDISCOVERY_config_file="${CONFIG_FILE}"
elif [ -f /config/appsettings.json ]; then
  export ETDISCOVERY_config_file=/config/appsettings.json
fi

if [ -z "${ETDISCOVERY_ROLES:-${ETDISCOVERY_roles:-}}" ]; then
  # If user passed --roles on CMD, bootstrap will pick it up from argv.
  # Only fail early when argv also has no --roles.
  has_roles_arg=0
  for arg in "$@"; do
    case "${arg}" in
      --roles|--roles=*) has_roles_arg=1 ;;
    esac
  done
  if [ "${has_roles_arg}" -eq 0 ]; then
    echo "ERROR: set ETDISCOVERY_ROLES (or ROLES / --roles). Examples: registry | worker | registry,worker" >&2
    exit 1
  fi
fi

if [ -z "${ETDISCOVERY_MODE:-${ETDISCOVERY_mode:-}}" ]; then
  has_mode_arg=0
  for arg in "$@"; do
    case "${arg}" in
      --mode|--mode=*) has_mode_arg=1 ;;
      # compat old flag
      --runtime-mode|--runtime-mode=*) has_mode_arg=1 ;;
    esac
  done
  if [ "${has_mode_arg}" -eq 0 ]; then
    # Registry image sets ETDISCOVERY_MODE=embedded in Dockerfile.
    echo "ERROR: set ETDISCOVERY_MODE (or MODE / --mode)." >&2
    echo "  Values: embedded | sidecar | daemon" >&2
    echo "  Registry image default: embedded. Workers: sidecar or daemon." >&2
    echo "  Note: standalone is removed (use embedded). daemon != K8s DaemonSet." >&2
    exit 1
  fi
fi

# Keep hierarchical env in sync when only ETDISCOVERY_MODE is set.
if [ -n "${ETDISCOVERY_MODE:-}" ] && [ -z "${EtDiscovery__Mode:-}" ]; then
  export EtDiscovery__Mode="${ETDISCOVERY_MODE}"
fi

# Default binary paths when not overridden by ConfigMap
export EasyTier__CorePath="${EasyTier__CorePath:-/usr/local/bin/easytier-core}"
export EasyTier__CliPath="${EasyTier__CliPath:-/usr/local/bin/easytier-cli}"

exec dotnet /app/EtDiscovery.Runtime.dll "$@"
