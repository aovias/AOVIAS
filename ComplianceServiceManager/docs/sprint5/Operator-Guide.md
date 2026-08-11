# Operator Guide — Sprint 5 Deployments

## Publish Runtime Agent

1. Open **Publishing** workspace in AOVIAS Command
2. Select **Runtime Agent** from discovered services
3. Publish and validate — the package auto-registers in the Release Library

## Deploy from Release Center

1. Switch profile to **Demo** in Settings (restart Command if prompted)
2. Open **Release Center**
3. Wizard steps:
   - Select runtime target (defaults from active profile)
   - Select release from library
   - Analyze package and review inspection details
   - Check **I approve this deployment** on the Review step
   - Deploy and monitor live pipeline steps
4. Verify health and history entries after completion

## Upgrade workflow

1. Publish a new `RuntimeAgent.aovdeploy` version
2. Repeat Release Center deploy workflow against the Demo runtime
3. Confirm orchestration steps through **Register Runtime** and **Audit**
4. Review deployment history detail for correlation ID and step timeline

## Rollback

If deployment acceptance fails and automatic rollback is enabled, the rollback workflow restores the previous release and nginx configuration. Monitor **RollbackProgress** events in the live console.

## Connection profiles

| Profile | Base URL |
|---------|----------|
| Local | `http://localhost:7080` |
| Demo | `https://demo.praman.cloud/runtime` |
| UAT | `https://uat.praman.cloud/runtime` |
| Production | `https://production.praman.cloud/runtime` |

Shell footer shows runtime connection status after profile activation.
