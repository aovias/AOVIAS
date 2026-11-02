import assert from "node:assert/strict";
import test from "node:test";
import {
  advisoryIdFromUrl,
  collectAdvisoryFindings,
  evaluateFindings,
  runGate,
} from "./npm-audit-gate.mjs";

const POLICY = JSON.stringify({
  schemaVersion: 1,
  exceptionReviewDate: "2026-12-02",
  allowedAdvisories: [
    "GHSA-qx2v-qp2m-jg93",
    "GHSA-6g55-p6wh-862q",
    "GHSA-fxqj-rqcc-2cmp",
    "GHSA-r28c-9q8g-f849",
  ],
});

const TODAY_VALID = new Date("2026-11-02T12:00:00.000Z");
const TODAY_EXPIRED = new Date("2026-12-03T12:00:00.000Z");

test("advisoryIdFromUrl extracts GHSA id", () => {
  assert.equal(
    advisoryIdFromUrl("https://github.com/advisories/GHSA-qx2v-qp2m-jg93"),
    "GHSA-QX2V-QP2M-JG93",
  );
});

test("accepted PostCSS high advisories pass before expiry", () => {
  const audit = {
    auditReportVersion: 2,
    vulnerabilities: {
      postcss: {
        severity: "high",
        via: [
          {
            url: "https://github.com/advisories/GHSA-6g55-p6wh-862q",
            severity: "high",
          },
          {
            url: "https://github.com/advisories/GHSA-r28c-9q8g-f849",
            severity: "high",
          },
        ],
      },
    },
  };

  const findings = collectAdvisoryFindings(audit);
  const result = evaluateFindings(findings, JSON.parse(POLICY), TODAY_VALID);
  assert.equal(result.errors.length, 0);
});

test("new high vulnerability fails", () => {
  const audit = {
    auditReportVersion: 2,
    vulnerabilities: {
      example: {
        severity: "high",
        via: [
          {
            url: "https://github.com/advisories/GHSA-xxxx-yyyy-zzzz",
            severity: "high",
          },
        ],
      },
    },
  };

  const findings = collectAdvisoryFindings(audit);
  const result = evaluateFindings(findings, JSON.parse(POLICY), TODAY_VALID);
  assert.ok(result.errors.some((e) => e.includes("GHSA-XXXX-YYYY-ZZZZ")));
});

test("new critical vulnerability fails", () => {
  const audit = {
    auditReportVersion: 2,
    vulnerabilities: {
      pkg: {
        severity: "critical",
        via: [
          {
            url: "https://github.com/advisories/GHSA-abcd-efgh-ijkl",
            severity: "critical",
          },
        ],
      },
    },
  };

  const result = runGate(JSON.stringify(audit), POLICY, TODAY_VALID);
  assert.ok(result.errors.length > 0);
});

test("moderate-only finding does not fail gate", () => {
  const audit = {
    auditReportVersion: 2,
    vulnerabilities: {
      eslint: {
        severity: "moderate",
        via: [
          {
            url: "https://github.com/advisories/GHSA-g3hv-75wg-m249",
            severity: "moderate",
          },
        ],
      },
    },
  };

  const result = runGate(JSON.stringify(audit), POLICY, TODAY_VALID);
  assert.equal(result.errors.length, 0);
});

test("expired exception fails for allowed PostCSS advisories", () => {
  const audit = {
    auditReportVersion: 2,
    vulnerabilities: {
      postcss: {
        severity: "high",
        via: [
          {
            url: "https://github.com/advisories/GHSA-6g55-p6wh-862q",
            severity: "high",
          },
        ],
      },
    },
  };

  const result = runGate(JSON.stringify(audit), POLICY, TODAY_EXPIRED);
  assert.ok(result.errors.some((e) => e.includes("expired")));
});

test("malformed audit JSON fails closed", () => {
  assert.throws(() => runGate("{not-json", POLICY, TODAY_VALID), /Invalid audit JSON/);
});

test("no findings passes", () => {
  const audit = {
    auditReportVersion: 2,
    vulnerabilities: {},
  };

  const result = runGate(JSON.stringify(audit), POLICY, TODAY_VALID);
  assert.equal(result.errors.length, 0);
});
