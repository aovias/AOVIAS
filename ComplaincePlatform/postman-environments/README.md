# Postman Environments

This directory contains Postman environment files for the Compliance Platform.

## Environments

### 1. VSDebug
For running services locally from Visual Studio.

**Service URLs:**
- IdentityService: `https://localhost:7135`
- ComplianceService: `https://localhost:7087`
- OrganizationService: `https://localhost:7181`
- AuditService: `https://localhost:7006`
- AIOrchestratorService: `https://localhost:7097`

### 2. Docker
For running services inside Docker containers.

**Service URLs:**
- IdentityService: `http://localhost:5001`
- ComplianceService: `http://localhost:5002`
- OrganizationService: `http://localhost:5004`
- AuditService: `http://localhost:5003`
- AIOrchestratorService: `http://localhost:5005`

## Variables Included

Both environments include the following variables:

- `base_url_IdentityService` - Base URL for Identity Service
- `base_url_ComplianceService` - Base URL for Compliance Service
- `base_url_OrganizationService` - Base URL for Organization Service
- `base_url_AuditService` - Base URL for Audit Service
- `base_url_AIOrchestratorService` - Base URL for AI Orchestrator Service
- `identity_authority` - Identity Service authority URL for JWT validation
- `identity_token_endpoint` - Token endpoint for OAuth2 authentication
- `client_id` - OAuth2 client ID (postman-client)
- `client_secret` - OAuth2 client secret (postman-secret)
- `scope` - OAuth2 scopes (openid profile organization.api compliance.api audit.api ai.api)

## How to Import

1. Open Postman
2. Click on **Environments** in the left sidebar (or press `Ctrl+E`)
3. Click **Import** button
4. Select the environment file you want to import:
   - `VSDebug.postman_environment.json`
   - `Docker.postman_environment.json`
5. The environment will appear in your environments list
6. Select the environment from the dropdown in the top-right corner of Postman

## Usage in Requests

Use the variables in your requests like this:

```
{{base_url_IdentityService}}/connect/token
{{base_url_ComplianceService}}/api/v1/compliance/programs
{{base_url_OrganizationService}}/api/v1/organizations
```

## Notes

- **VSDebug** uses HTTPS URLs (ports 7135, 7087, 7181, 7006, 7097)
- **Docker** uses HTTP URLs (ports 5001-5005)
- Make sure the services are running before making requests
- For Docker environment, ensure containers are up: `docker-compose -f docker-compose.dev.yml up`
