# Runtime Agent Deployment

The Runtime Agent Docker image is environment-neutral. It does not contain machine-specific configuration paths or profile names.

Deployment settings are resolved in this order:

1. Environment variables
2. Bootstrap `appsettings.json`
3. Safe defaults

## Required environment variables

| Variable | Description | Example |
|----------|-------------|---------|
| `AOVIAS_CONFIG_ROOT` | Mounted configuration root | `/opt/aovias/runtime/config` |
| `AOVIAS_ACTIVE_PROFILE` | Profile directory name under the config root | `demo` |
| `AOVIAS_PROFILES_DIRECTORY` | Profiles folder name relative to config root | `profiles` |
| `AOVIAS_SECRETS_DIRECTORY` | Secrets folder name relative to config root | `secrets` |
| `DOTNET_ENVIRONMENT` | ASP.NET Core environment name | `Production` |

Optional runtime paths used by compose templates and deployment packages:

| Variable | Description | Example |
|----------|-------------|---------|
| `AOVIAS_PACKAGES_ROOT` | Release package storage | `/opt/aovias/runtime/packages` |
| `AOVIAS_LOGS_ROOT` | Runtime log output | `/opt/aovias/runtime/logs` |

## Example `.env`

```env
DOTNET_ENVIRONMENT=Production

AOVIAS_CONFIG_ROOT=/opt/aovias/runtime/config
AOVIAS_ACTIVE_PROFILE=demo
AOVIAS_PROFILES_DIRECTORY=profiles
AOVIAS_SECRETS_DIRECTORY=secrets

AOVIAS_PACKAGES_ROOT=/opt/aovias/runtime/packages
AOVIAS_LOGS_ROOT=/opt/aovias/runtime/logs
```

Copy `.env.example` to `.env` and adjust values for the target environment.

## Expected config layout

```text
/opt/aovias/runtime/config/
  profiles/
    demo/
      settings.api.json
      settings.docker.json
      ...
  secrets/
    demo/
      settings.secrets.json
```

## Docker Compose

In this repository, tracked publishing and deployment templates live under `AOVIAS.Infrastructure/aovias-config/templates/` (for example `templates/publishing/` for service Dockerfiles and related publish outputs, and `templates/nginx/runtime-agent.site.conf.template` used when rendering nginx site config). The publishing pipeline resolves templates from the configured AOVIAS config root (`AOVIAS_CONFIG_ROOT` on the host or the equivalent `ConfigRoot` in development settings), not from a root-level `aovias-config/` directory.

Mount the config root read-only and provide the environment variables above. The same image can be reused across Local, Demo, UAT, and Production by changing only the mounted config tree and environment variables.

Compose templates use fixed in-container paths (`/opt/aovias/runtime/...`) and separate `AOVIAS_HOST_*` variables for host-side volume mounts.

## Local development

Local development continues to use `appsettings.Development.json` with developer-specific `ConfigRoot` and `ActiveProfile` values when `DOTNET_ENVIRONMENT=Development`.

Production containers use the neutral bootstrap `appsettings.json` plus deployment environment variables.

## Startup diagnostics

On startup the Runtime Agent prints resolved bootstrap values before loading profile configuration:

```text
Bootstrapping Runtime Agent...

Configuration Root:
/opt/aovias/runtime/config

Active Profile:
demo

Profiles Directory:
/opt/aovias/runtime/config/profiles

Loading profile...
```

If validation fails, startup stops with an actionable message describing the missing path.
