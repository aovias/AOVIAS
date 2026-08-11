# 04 — Configuration Platform

**Phase:** 6.4  
**Sprint:** 3  
**Date:** 2026-07-26

## Schema (setup wizard)

Written to `Gateway/App_Data/aovias.installation.json` (camelCase JSON):

| Section | Keys | Notes |
|---------|------|--------|
| Database | `ConnectionStrings:DefaultConnection`, `Database:Provider` | Validated non-empty connection string |
| Smtp | `host`, `port`, `username`, `password`, `fromAddress`, `useSsl` | Port 1–65535; fromAddress email when set |
| Storage | `provider`, `rootPath`, `connectionString`, `containerName` | Provider required when section present |
| Ai | `provider`, `endpoint`, `apiKey`, `enabled` | Endpoint required when enabled |
| Auth | maps into `JwtSettings` (`Issuer`, `Audience`, `SecretKey`, `ExpirationMinutes`) | Secret ≥ 32 chars when provided |
| Installation | `ConfiguredAtUtc`, `Completed`, `CompletedAtUtc` | Updated by configure/complete |

Validation: `SetupConfigureRequestValidator` (FluentValidation). At least one section required per configure call.

## Layering

1. Source `appsettings.json` / environment-specific files (checked in, no customer secrets).
2. Environment variables / secret stores (preferred for production secrets).
3. Optional `App_Data/aovias.installation.json` overlay (wizard output, reloadOnChange).

Never use the wizard to overwrite repository appsettings.

## Markers

| Marker | Meaning |
|--------|---------|
| `Installation:Completed` in config | Soft completed flag |
| Memory key `Installation:Completed` | In-process after `POST complete` |
| `App_Data/installation.lock` | Hard lock; timestamp UTC inside file |

## Exit

Customer can configure Database / SMTP / Storage / AI / Auth through the wizard with validation, without committing secrets to source control.
