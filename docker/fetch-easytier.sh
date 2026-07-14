#!/usr/bin/env sh
# Fetch easytier-core / easytier-cli into ${DEST_DIR:-/opt/easytier}.
#
# Sources (EASYTIER_SOURCE):
#   actions  – GitHub Actions artifact from a parent EasyTier repo workflow run
#   release  – GitHub Release asset (official or fork published release)
#   url      – arbitrary zip/tarball URL (EASYTIER_DOWNLOAD_URL required)
#
# Common build-args / env (all optional unless noted):
#   EASYTIER_SOURCE          actions|release|url   (default: actions)
#   EASYTIER_GITHUB_REPO     owner/name            (default: SwingCosmic/EasyTier)
#   EASYTIER_BRANCH          branch for "latest" Actions run (default: main)
#   EASYTIER_WORKFLOW_FILE   workflow file name    (default: core.yml)
#   EASYTIER_RUN_ID          pin a specific Actions run id (skips branch lookup)
#   EASYTIER_ARTIFACT_NAME   artifact name override (default: easytier-linux-<arch>)
#   EASYTIER_VERSION         release version w/o "v" (default: 2.6.4; source=release)
#                            use "latest" for the fork rolling Release tag (stable asset names)
#   EASYTIER_RELEASE_TAG     Git tag for release downloads (default: v$VERSION, or "latest" when VERSION=latest)
#   EASYTIER_DOWNLOAD_URL    full URL when source=url (or override any source)
#   EASYTIER_GITHUB_API      API base (default: https://api.github.com)
#   GITHUB_TOKEN             optional; recommended for Actions API rate limits /
#                            private repos. Prefer Docker BuildKit secret
#                            id=github_token over baking the token into layers.
#   TARGETARCH               docker/buildx arch: amd64|arm64|...
#   DEST_DIR                 install directory (default: /opt/easytier)

set -eu

DEST_DIR="${DEST_DIR:-/opt/easytier}"
EASYTIER_SOURCE="${EASYTIER_SOURCE:-actions}"
EASYTIER_GITHUB_REPO="${EASYTIER_GITHUB_REPO:-SwingCosmic/EasyTier}"
EASYTIER_BRANCH="${EASYTIER_BRANCH:-main}"
EASYTIER_WORKFLOW_FILE="${EASYTIER_WORKFLOW_FILE:-core.yml}"
EASYTIER_RUN_ID="${EASYTIER_RUN_ID:-}"
EASYTIER_ARTIFACT_NAME="${EASYTIER_ARTIFACT_NAME:-}"
EASYTIER_VERSION="${EASYTIER_VERSION:-2.6.4}"
EASYTIER_RELEASE_TAG="${EASYTIER_RELEASE_TAG:-}"
EASYTIER_DOWNLOAD_URL="${EASYTIER_DOWNLOAD_URL:-}"
EASYTIER_GITHUB_API="${EASYTIER_GITHUB_API:-https://api.github.com}"
TARGETARCH="${TARGETARCH:-amd64}"
GITHUB_TOKEN="${GITHUB_TOKEN:-}"

# Prefer BuildKit secret if present (not stored in image history as ARG).
if [ -z "${GITHUB_TOKEN}" ] && [ -f /run/secrets/github_token ]; then
  GITHUB_TOKEN="$(cat /run/secrets/github_token)"
fi

arch_from_target() {
  case "${1}" in
    amd64|x86_64) echo x86_64 ;;
    arm64|aarch64) echo aarch64 ;;
    arm/v7|armv7) echo armv7hf ;;
    arm/v6|armhf) echo armhf ;;
    riscv64) echo riscv64 ;;
    *)
      echo "Unsupported TARGETARCH=${1} (set EASYTIER_ARTIFACT_NAME or EASYTIER_DOWNLOAD_URL)" >&2
      exit 1
      ;;
  esac
}

ARCH="$(arch_from_target "${TARGETARCH}")"
if [ -z "${EASYTIER_ARTIFACT_NAME}" ]; then
  EASYTIER_ARTIFACT_NAME="easytier-linux-${ARCH}"
fi

api_curl() {
  # usage: api_curl <url> [extra curl args...]
  url="$1"
  shift
  set -- -fsSL \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "$@"
  if [ -n "${GITHUB_TOKEN}" ]; then
    set -- -H "Authorization: Bearer ${GITHUB_TOKEN}" "$@"
  fi
  curl "$@" "${url}"
}

download_file() {
  url="$1"
  out="$2"
  echo "Downloading ${url}"
  if [ -n "${GITHUB_TOKEN}" ] && echo "${url}" | grep -q 'github.com\|githubusercontent.com\|api.github.com'; then
    curl -fsSL -H "Authorization: Bearer ${GITHUB_TOKEN}" -o "${out}" -L "${url}"
  else
    curl -fsSL -o "${out}" -L "${url}"
  fi
}

extract_binaries() {
  archive="$1"
  extract_dir="/tmp/easytier-extract"
  rm -rf "${extract_dir}"
  mkdir -p "${extract_dir}" "${DEST_DIR}"

  case "${archive}" in
    *.zip)
      unzip -o "${archive}" -d "${extract_dir}" >/dev/null
      ;;
    *.tar.gz|*.tgz)
      tar -xzf "${archive}" -C "${extract_dir}"
      ;;
    *.tar.xz)
      tar -xJf "${archive}" -C "${extract_dir}"
      ;;
    *)
      # Actions artifact download is always a zip without suffix in URL; sniff magic.
      if unzip -t "${archive}" >/dev/null 2>&1; then
        unzip -o "${archive}" -d "${extract_dir}" >/dev/null
      else
        echo "Unknown archive format: ${archive}" >&2
        exit 1
      fi
      ;;
  esac

  # Release zips nest under easytier-linux-*; Actions artifacts are flat.
  core_path="$(find "${extract_dir}" -type f -name 'easytier-core' | head -n 1)"
  cli_path="$(find "${extract_dir}" -type f -name 'easytier-cli' | head -n 1)"

  if [ -z "${core_path}" ] || [ -z "${cli_path}" ]; then
    echo "easytier-core / easytier-cli not found in archive. Contents:" >&2
    find "${extract_dir}" -type f | head -n 50 >&2
    exit 1
  fi

  cp "${core_path}" "${DEST_DIR}/easytier-core"
  cp "${cli_path}" "${DEST_DIR}/easytier-cli"
  chmod +x "${DEST_DIR}/easytier-core" "${DEST_DIR}/easytier-cli"
  rm -rf "${extract_dir}" "${archive}"
  echo "Installed:"
  ls -la "${DEST_DIR}/easytier-core" "${DEST_DIR}/easytier-cli"
}

resolve_actions_download_url() {
  repo="${EASYTIER_GITHUB_REPO}"
  api="${EASYTIER_GITHUB_API}/repos/${repo}"
  run_id="${EASYTIER_RUN_ID}"

  if [ -z "${run_id}" ]; then
    # Latest successful EasyTier Core run on the given branch.
    query="branch=${EASYTIER_BRANCH}&status=success&per_page=5"
    runs_json="$(api_curl "${api}/actions/workflows/${EASYTIER_WORKFLOW_FILE}/runs?${query}")"
    run_id="$(printf '%s' "${runs_json}" | jq -r '
      .workflow_runs
      | map(select(.conclusion == "success"))
      | .[0].id // empty
    ')"
    if [ -z "${run_id}" ] || [ "${run_id}" = "null" ]; then
      echo "No successful Actions run found for workflow=${EASYTIER_WORKFLOW_FILE} branch=${EASYTIER_BRANCH} repo=${repo}" >&2
      echo "Pass EASYTIER_RUN_ID, or use EASYTIER_SOURCE=release|url." >&2
      exit 1
    fi
    echo "Using latest successful run id=${run_id} (branch=${EASYTIER_BRANCH})"
  else
    echo "Using pinned run id=${run_id}"
  fi

  arts_json="$(api_curl "${api}/actions/runs/${run_id}/artifacts?per_page=100")"
  artifact_id="$(printf '%s' "${arts_json}" | jq -r --arg n "${EASYTIER_ARTIFACT_NAME}" '
    .artifacts
    | map(select(.name == $n and .expired == false))
    | .[0].id // empty
  ')"

  if [ -z "${artifact_id}" ] || [ "${artifact_id}" = "null" ]; then
    echo "Artifact '${EASYTIER_ARTIFACT_NAME}' not found (or expired) on run ${run_id}." >&2
    echo "Available artifacts:" >&2
    printf '%s' "${arts_json}" | jq -r '.artifacts[] | "  - \(.name) (expired=\(.expired))"' >&2
    exit 1
  fi

  # API zip endpoint; requires auth for many cases — token recommended.
  printf '%s' "${api}/actions/artifacts/${artifact_id}/zip"
}

resolve_release_download_url() {
  # Official / versioned: easytier-linux-x86_64-v2.6.4.zip under tag v2.6.4
  # Rolling main (fork):   easytier-linux-x86_64.zip under tag "latest" (overwritten each main build)
  ver="${EASYTIER_VERSION#v}"
  if [ -n "${EASYTIER_RELEASE_TAG}" ]; then
    tag="${EASYTIER_RELEASE_TAG}"
  elif [ "${ver}" = "latest" ] || [ "${ver}" = "main" ]; then
    tag="latest"
  else
    tag="v${ver}"
  fi

  if [ "${tag}" = "latest" ] || [ "${ver}" = "latest" ] || [ "${ver}" = "main" ]; then
    asset="${EASYTIER_ARTIFACT_NAME}.zip"
  else
    asset="${EASYTIER_ARTIFACT_NAME}-v${ver}.zip"
  fi
  printf '%s' "https://github.com/${EASYTIER_GITHUB_REPO}/releases/download/${tag}/${asset}"
}

main() {
  archive="/tmp/easytier-download.bin"
  url="${EASYTIER_DOWNLOAD_URL}"

  if [ -z "${url}" ]; then
    case "${EASYTIER_SOURCE}" in
      actions)
        url="$(resolve_actions_download_url)"
        ;;
      release)
        url="$(resolve_release_download_url)"
        ;;
      url)
        echo "EASYTIER_SOURCE=url requires EASYTIER_DOWNLOAD_URL" >&2
        exit 1
        ;;
      *)
        echo "Unknown EASYTIER_SOURCE=${EASYTIER_SOURCE} (use actions|release|url)" >&2
        exit 1
        ;;
    esac
  fi

  download_file "${url}" "${archive}"
  # Normalize extension for extract heuristic when URL has no filename suffix.
  if [ "${EASYTIER_SOURCE}" = "actions" ] || echo "${url}" | grep -q '/actions/artifacts/.*/zip$'; then
    mv "${archive}" /tmp/easytier.zip
    archive=/tmp/easytier.zip
  fi
  extract_binaries "${archive}"
}

main
