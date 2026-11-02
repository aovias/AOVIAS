#!/usr/bin/env node
/**
 * AccredaOne npm audit gate — fail-closed on malformed input and new high/critical advisories.
 * Allowed PostCSS advisories are time-bounded per npm-audit-policy.json.
 */
import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DEFAULT_POLICY_PATH = join(__dirname, "npm-audit-policy.json");

/**
 * @param {string} url
 * @returns {string | null}
 */
export function advisoryIdFromUrl(url) {
  if (typeof url !== "string") {
    return null;
  }

  const match = url.match(/GHSA-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}/i);
  return match ? match[0].toUpperCase() : null;
}

/**
 * @param {unknown} auditReport
 * @returns {{ id: string, severity: string, packageName: string }[]}
 */
export function collectAdvisoryFindings(auditReport) {
  if (!auditReport || typeof auditReport !== "object") {
    throw new Error("Invalid audit report: expected object");
  }

  if (auditReport.auditReportVersion !== 2) {
    throw new Error("Invalid audit report: unsupported auditReportVersion");
  }

  const vulnerabilities = auditReport.vulnerabilities;
  if (!vulnerabilities || typeof vulnerabilities !== "object") {
    throw new Error("Invalid audit report: missing vulnerabilities");
  }

  /** @type {{ id: string, severity: string, packageName: string }[]} */
  const findings = [];

  for (const [packageName, entry] of Object.entries(vulnerabilities)) {
    if (!entry || typeof entry !== "object") {
      continue;
    }

    const via = entry.via;
    if (!Array.isArray(via)) {
      continue;
    }

    for (const item of via) {
      if (!item || typeof item !== "object" || !item.url) {
        continue;
      }

      const id = advisoryIdFromUrl(item.url);
      if (!id) {
        continue;
      }

      findings.push({
        id: id.toUpperCase(),
        severity: String(item.severity ?? entry.severity ?? "unknown").toLowerCase(),
        packageName,
      });
    }
  }

  return findings;
}

/**
 * @param {unknown} policy
 * @param {Date} today
 */
export function isExceptionExpired(policy, today) {
  const reviewDate = policy?.exceptionReviewDate;
  if (typeof reviewDate !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(reviewDate)) {
    throw new Error("Invalid policy: exceptionReviewDate must be YYYY-MM-DD");
  }

  const expiry = new Date(`${reviewDate}T23:59:59.999Z`);
  return today.getTime() > expiry.getTime();
}

/**
 * @param {{ id: string, severity: string, packageName: string }[]} findings
 * @param {unknown} policy
 * @param {Date} today
 */
export function evaluateFindings(findings, policy, today = new Date()) {
  if (!policy || typeof policy !== "object") {
    throw new Error("Invalid policy file");
  }

  const allowed = new Set(
    (policy.allowedAdvisories ?? []).map((id) => String(id).toUpperCase()),
  );

  const expired = isExceptionExpired(policy, today);
  const highOrCritical = findings.filter(
    (f) => f.severity === "high" || f.severity === "critical",
  );

  /** @type {string[]} */
  const errors = [];

  if (expired && highOrCritical.some((f) => allowed.has(f.id))) {
    errors.push(
      `PostCSS npm audit exception expired on ${policy.exceptionReviewDate}. Remediate or renew acceptance.`,
    );
  }

  for (const finding of highOrCritical) {
    if (expired || !allowed.has(finding.id)) {
      errors.push(
        `Disallowed ${finding.severity} advisory ${finding.id} on ${finding.packageName}`,
      );
    }
  }

  const moderateCount = findings.filter((f) => f.severity === "moderate").length;
  const summary = {
    totalAdvisories: findings.length,
    highOrCritical: highOrCritical.length,
    moderate: moderateCount,
    allowedHighOrCritical: highOrCritical.filter((f) => allowed.has(f.id)).length,
    exceptionExpired: expired,
  };

  return { errors: [...new Set(errors)], summary, findings };
}

/**
 * @param {string} auditJsonText
 * @param {string} policyJsonText
 * @param {Date} today
 */
export function runGate(auditJsonText, policyJsonText, today = new Date()) {
  let auditReport;
  let policy;

  try {
    auditReport = JSON.parse(auditJsonText);
  } catch {
    throw new Error("Invalid audit JSON");
  }

  try {
    policy = JSON.parse(policyJsonText);
  } catch {
    throw new Error("Invalid policy JSON");
  }

  const findings = collectAdvisoryFindings(auditReport);
  return evaluateFindings(findings, policy, today);
}

function loadText(path) {
  return readFileSync(path, "utf8");
}

function main() {
  const auditPath = process.argv[2];
  const policyPath = process.argv[3] ?? DEFAULT_POLICY_PATH;

  if (!auditPath) {
    console.error("Usage: node npm-audit-gate.mjs <audit.json> [policy.json]");
    process.exit(2);
  }

  try {
    const result = runGate(loadText(resolve(auditPath)), loadText(resolve(policyPath)));
    console.log("npm audit summary:", JSON.stringify(result.summary, null, 2));

    if (result.findings.length > 0) {
      console.log("Advisory findings:");
      for (const finding of result.findings) {
        console.log(`  - ${finding.id} (${finding.severity}) via ${finding.packageName}`);
      }
    }

    if (result.errors.length > 0) {
      console.error("npm audit gate failed:");
      for (const error of result.errors) {
        console.error(`  - ${error}`);
      }
      process.exit(1);
    }

    console.log("npm audit gate passed.");
  } catch (error) {
    console.error(`npm audit gate error: ${error instanceof Error ? error.message : error}`);
    process.exit(1);
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  main();
}
