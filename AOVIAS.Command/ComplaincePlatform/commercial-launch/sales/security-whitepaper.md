# Security Whitepaper — AOVIAS Command (Commercial)

**Version:** 1.0.0 · **Audience:** CISO / hospital security reviewers

## Scope

This paper summarizes security-relevant controls for AOVIAS Command commercial deployments. It is not a substitute for a customer-specific threat model or a paid penetration test report (pen-test engagement deferred in v1.0.0 release notes; ASVS-lite / header scan practices exist in engineering packs).

## Trust boundaries

- External clients terminate at **Gateway**
- Backend services are not public
- Domain services do not publish directly to RabbitMQ
- Secrets via environment / Key Vault—not source control

## Identity & access

- Authenticated access via Identity integration
- Organization membership and role claims drive authorization
- License seats and entitlements constrain feature surface
- Prefer least privilege for Auditor vs Department Head vs Admin

## Data protection

- Customer databases hold operational compliance records—include in backup/DR
- Hospitals define PHI policy; AI prompts must exclude patient identifiers
- Evidence integrity enforced by journey locking rules
- Transport TLS expected at the edge (customer/infra owned certificates)

## Application hardening (Phase 7 highlights)

- Security headers  
- Optional Redis / Key Vault  
- Polly circuit breakers for resilience  
- Maintenance mode for controlled windows  
- OpenTelemetry hooks for observability  

## Auditability

- Correlation IDs (`X-Correlation-ID`) across Gateway and services  
- License activation/heartbeat history  
- Lifecycle transitions through service APIs (not silent DB edits)  

## Secure deployment checklist

- [ ] Secrets rotated; `.env` not committed  
- [ ] Swagger disabled or protected in production  
- [ ] SQL access least privilege per service catalog  
- [ ] Backup encryption per hospital policy  
- [ ] SBOM + checksums retained for installed VERSION  

## Responsible disclosure

Route vulnerability reports to security contact designated in MSA (placeholder: sales@aovias.example for commercial intake until security alias is published).
