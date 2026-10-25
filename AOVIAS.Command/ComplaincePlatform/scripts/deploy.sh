#!/usr/bin/env bash
# Deploy AOVIAS commercial stack via existing docker-compose plane.
set -euo pipefail

PLATFORM_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PLATFORM_ROOT"

BUILD=0
DETACH=0
PROJECT_NAME="aovias"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --build) BUILD=1; shift ;;
    -d|--detach) DETACH=1; shift ;;
    -p|--project-name) PROJECT_NAME="$2"; shift 2 ;;
    *) echo "Unknown arg: $1"; exit 1 ;;
  esac
done

if [[ -f .env ]]; then
  # shellcheck disable=SC1091
  set -a
  source .env
  set +a
fi

if [[ -z "${SA_PASSWORD:-}" ]]; then
  echo "ERROR: SA_PASSWORD is required (export or set in .env)."
  exit 1
fi

echo "=== AOVIAS env validation ==="
echo "[OK] SA_PASSWORD (set)"
[[ -n "${ASPNETCORE_CERT_PASSWORD:-}" ]] && echo "[OK] ASPNETCORE_CERT_PASSWORD (set)" || echo "[WARN] ASPNETCORE_CERT_PASSWORD not set"

ARGS=(-f docker-compose.dev.yml -f docker-compose.commercial.yml -p "$PROJECT_NAME" up)
[[ $DETACH -eq 1 ]] && ARGS+=(-d)
[[ $BUILD -eq 1 ]] && ARGS+=(--build)

echo "Deploying: docker compose ${ARGS[*]}"
docker compose "${ARGS[@]}"

cat <<EOF

Deploy started.
  Gateway:  http://localhost:5000  (https://localhost:7216)
  Identity: https://localhost:7135
  Org:      https://localhost:7181
  Compliance: https://localhost:7087
  AI:       https://localhost:7097
EOF
