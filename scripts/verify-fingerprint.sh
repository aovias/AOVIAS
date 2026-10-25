#!/usr/bin/env bash
# Sprint M3.2 — Package fingerprint / double-publish reproducibility gate.
#
# ROADMAP.md's M3.2 entry calls for "double-publish gate 10/10" — proving
# Milestone 1's determinism guarantee (same inputs -> same package
# fingerprint) holds under repeated execution. This script does not add new
# production code: it repeatedly runs the existing determinism-focused test
# suites (Aovias.Publishing.Core.Tests: DeterministicPackageFingerprintTests,
# ReleaseLibraryRegressionTests) and diffs the outcome summary of each run
# against the first. If every run's pass/fail set is identical, the gate is
# satisfied; any divergence is a determinism regression.
#
# Per the Execution Honesty Rule (IMPLEMENTATION_GUIDELINES.md §11), this
# script has NOT been executed in the authoring sandbox — no dotnet SDK is
# available there. It is prepared for execution in a real .NET SDK
# environment, not verified by a run.
#
# Usage: scripts/verify-fingerprint.sh [run-count (default 10)]

set -euo pipefail

RUN_COUNT="${1:-10}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT="${REPO_ROOT}/AOVIAS.Studio/Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj"
FILTER="FullyQualifiedName~DeterministicPackageFingerprint|FullyQualifiedName~ReleaseLibraryRegression"
RESULTS_DIR="${REPO_ROOT}/_build_out/fingerprint-gate/$(date -u +%Y%m%dT%H%M%SZ)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK not found on PATH. This check requires Runtime Verification" >&2
  echo "(see IMPLEMENTATION_GUIDELINES.md §11) and cannot be faked. Install the .NET SDK" >&2
  echo "matching the repo's net10.0 target and re-run this script." >&2
  exit 2
fi

if [ ! -f "${TEST_PROJECT}" ]; then
  echo "ERROR: expected test project not found at ${TEST_PROJECT}" >&2
  exit 2
fi

mkdir -p "${RESULTS_DIR}"
FIRST_SUMMARY=""
FAILED=0

for i in $(seq 1 "${RUN_COUNT}"); do
  echo "== Fingerprint determinism run ${i}/${RUN_COUNT} =="
  LOG_NAME="run-${i}.trx"
  dotnet test "${TEST_PROJECT}" \
    --configuration Release \
    --nologo \
    --filter "${FILTER}" \
    --logger "trx;LogFileName=${LOG_NAME}" \
    --results-directory "${RESULTS_DIR}" \
    -p:ContinuousIntegrationBuild=true

  # Reduce each TRX to a stable outcome fingerprint: sorted list of
  # "testName:outcome" pairs, ignoring timestamps/durations/machine name.
  SUMMARY=$(grep -oE '<UnitTestResult[^>]*testName="[^"]*"[^>]*outcome="[^"]*"' \
      "${RESULTS_DIR}/${LOG_NAME}" \
    | sed -E 's/.*testName="([^"]*)".*outcome="([^"]*)".*/\1:\2/' \
    | sort)

  if [ -z "${FIRST_SUMMARY}" ]; then
    FIRST_SUMMARY="${SUMMARY}"
  elif [ "${SUMMARY}" != "${FIRST_SUMMARY}" ]; then
    echo "DETERMINISM FAILURE: run ${i} outcome set differs from run 1" >&2
    diff <(echo "${FIRST_SUMMARY}") <(echo "${SUMMARY}") >&2 || true
    FAILED=1
  fi
done

if [ "${FAILED}" -ne 0 ]; then
  echo "== Fingerprint gate FAILED: not all ${RUN_COUNT} runs produced identical outcomes ==" >&2
  exit 1
fi

echo "== Fingerprint gate PASSED: ${RUN_COUNT}/${RUN_COUNT} runs produced identical outcomes =="
echo "TRX logs: ${RESULTS_DIR}"
