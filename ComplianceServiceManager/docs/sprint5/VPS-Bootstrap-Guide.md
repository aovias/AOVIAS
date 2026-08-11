# VPS Bootstrap Guide — Sprint 5

## Prerequisites

- Ubuntu VPS with Docker, nginx, and HTTPS certificates for `demo.praman.cloud`
- SSH access for a deploy user with Docker group membership
- AOVIAS config root deployed to the VPS (for example `/opt/aovias-config`)
- Published `RuntimeAgent.aovdeploy` from AOVIAS Command Publishing workspace

## One-time bootstrap

```powershell
dotnet run --project V:\DevelopmentRepo\V1.0\Aovias.Bootstrap\src\Aovias.Bootstrap.Host\Aovias.Bootstrap.Host.csproj -- deploy `
  --package V:\path\to\runtime-agent-1.0.0.aovdeploy `
  --profile demo `
  --target demo.praman.cloud `
  --ssh-user deploy `
  --ssh-key C:\Users\you\.ssh\id_ed25519 `
  --config-root V:\DevelopmentRepo\V1.0\aovias-config
```

The bootstrap host tool will:

1. Load the `demo` profile configuration
2. Upload the package to `{incomingFolder}`
3. Extract, `docker load`, and `docker compose up` on the VPS
4. Render nginx site config from `templates/nginx/runtime-agent.site.conf.template`
5. Verify `https://demo.praman.cloud/runtime/health`
6. Write the initial runtime manifest

## Verification checklist

- [ ] `https://demo.praman.cloud/runtime/health` returns HTTP 200
- [ ] `https://demo.praman.cloud/runtime/swagger` loads
- [ ] SignalR hub reachable at `/runtime/hubs/runtime`
- [ ] Runtime manifest exists at profile `manifestPath`
- [ ] AOVIAS Command connects using the **Demo** profile

## After bootstrap

All upgrades flow through **Release Center → Runtime Agent API → Orchestration**. Do not reuse the bootstrap tool for routine deployments.
