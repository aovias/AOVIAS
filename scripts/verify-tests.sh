#!/usr/bin/env bash
# Sprint M3.2 — Deterministic test verification.
#
# Purpose: single, repeatable entry point for "do all suites pass," producing
# a TRX log per run so results are diffable across repeated executions
# (determinism evidence, not just a pass/fail bit). Per the Execution
# Honesty Rule (IMPLEMENTATION_GUIDELINES.md §11), this script has NOT been
# executed in the authoring sandbox — no dotnet SDK is available there. It
# is prepared for execution in a real .NET SDK environment, not verified by
# a run.
#
# Usage: scripts/verify-tests.sh [Debug|Release]

set -euo pipefail

CONFIGURATION="${1:-Release}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="${REPO_ROOT}/AOVIAS.Studio/Aovias.Platform.slnx"
RESULTS_DIR="${REPO_ROOT}/_build_out/test-results/$(date -u +%Y%m%dT%H%M%SZ)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK not found on PATH. This check requires Runtime Verification" >&2
  echo "(see IMPLEMENTATION_GUIDELINES.md §11) and cannot be faked. Install the .NET SDK" >&2
  echo "matching the repo's net10.0 target and re-run this script." >&2
  exit 2
fi

mkdir -p "${RESULTS_DIR}"

echo "== dotnet test (Configuration=${CONFIGURATION}), results -> ${RESULTS_DIR} =="
dotnet test "${SOLUTION}" \
  --configuration "${CONFIGURATION}" \
  --nologo \
  --logger "trx;LogFileName=results.trx" \
  --results-directory "${RESULTS_DIR}" \
  -p:ContinuousIntegrationBuild=true

echo "== Test run complete. TRX log: ${RESULTS_DIR}/results.trx =="
echo "For determinism evidence, run this script twice and diff the two TRX"
echo "outcome summaries (pass/fail counts and test names must match exactly)."
