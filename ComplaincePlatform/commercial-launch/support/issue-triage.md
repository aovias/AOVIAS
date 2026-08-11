# Issue Triage

**Product:** AOVIAS Command v1.0.0  
**Audience:** L1 Support

## Severity model

| Severity | Definition | Response target | Examples |
|----------|------------|-----------------|----------|
| S1 | Production down / data loss risk | 15m acknowledge | Gateway 503 unexpected; DB corrupt; auth totally down |
| S2 | Major feature unusable for many users | 1h | License expired fleet-wide; evidence upload failing org-wide |
| S3 | Impaired feature / workaround exists | Next business day | Single user seat; AI citations flaky |
| S4 | How-to / cosmetic | Queue | Training questions; UI preference |

## Intake checklist

1. Customer org ID, environment (pilot/prod), VERSION (`1.0.0`)  
2. UTC time of first occurrence + Correlation ID  
3. Role of reporter + number of users impacted  
4. Steps to reproduce  
5. Already tried: KB article? diagnostics downloaded?  
6. License status (Active / Grace / Expired)  

## First-line routing

| Symptom class | Start with |
|---------------|------------|
| Login / 401 | KB-001 |
| License / entitlement | KB-002 + activation guide |
| Backup / upgrade | KB-003 + ops scripts |
| AI | KB-004 |
| Planned 503 | KB-005 |
| Journey / evidence / CAPA | Compliance SME / Eng  
| Security incident | Security on-call (do not use public channel) |

## Diagnostics before escalate

Require or attach output of `GET /api/v1/commercial/diagnostics?organizationId=` when admin credentials available — see [diagnostic-collection.md](diagnostic-collection.md).

## Close criteria

- Root cause or durable workaround documented  
- Customer confirms  
- Tag ticket with module (Licensing, Compliance, AI, Identity, Ops)
