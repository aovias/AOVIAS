# AccredaOne CI scripts

Supporting scripts for `.github/workflows/accredaone-quality.yml`.

| Script | Purpose |
| --- | --- |
| `npm-audit-gate.mjs` | Enforces npm high/critical policy with PostCSS exception |
| `npm-audit-policy.json` | Time-bounded allowed GHSA ids |
| `nuget-audit-gate.mjs` | Fails on high/critical NuGet vulnerabilities |

Run tests:

```bash
node --test AOVIAS.AccredaOne/scripts/ci/npm-audit-gate.test.mjs
```
