#!/usr/bin/env node
/**
 * AccredaOne NuGet vulnerability gate — fails on high/critical vulnerable packages.
 */
import { spawnSync } from "node:child_process";

const projectPath = process.argv[2];
if (!projectPath) {
  console.error("Usage: node nuget-audit-gate.mjs <path-to-csproj>");
  process.exit(2);
}

const result = spawnSync(
  "dotnet",
  ["list", projectPath, "package", "--vulnerable", "--include-transitive", "--format", "json"],
  { encoding: "utf8", maxBuffer: 10 * 1024 * 1024 },
);

if (result.error) {
  console.error(`dotnet list failed: ${result.error.message}`);
  process.exit(1);
}

if (result.status !== 0) {
  console.error(result.stderr || result.stdout);
  process.exit(result.status ?? 1);
}

let payload;
try {
  payload = JSON.parse(result.stdout);
} catch {
  console.error("Invalid JSON from dotnet list --vulnerable");
  process.exit(1);
}

/** @type {{ packageId: string, severity: string }[]} */
const findings = [];

for (const project of payload.projects ?? []) {
  for (const framework of project.frameworks ?? []) {
    for (const pkg of framework.vulnerablePackages ?? []) {
      findings.push({
        packageId: pkg.name ?? pkg.packageId ?? "unknown",
        severity: String(pkg.severity ?? "unknown"),
      });
    }
  }
}

console.log(`NuGet vulnerable package count: ${findings.length}`);
for (const finding of findings) {
  console.log(`  - ${finding.packageId}: ${finding.severity}`);
}

const blocking = findings.filter((f) => {
  const severity = f.severity.toLowerCase();
  return severity === "high" || severity === "critical";
});

if (blocking.length > 0) {
  console.error("NuGet audit gate failed: high or critical vulnerabilities present.");
  process.exit(1);
}

if (findings.length > 0) {
  console.log("Non-blocking NuGet vulnerabilities reported (moderate/low). Review required.");
}

console.log("NuGet audit gate passed.");
process.exit(0);
