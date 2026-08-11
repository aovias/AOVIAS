# Support Escalation Path

## Ladder

| Level | Owner | Scope |
|-------|-------|-------|
| L1 | Support desk | KB, license activate, password / seat basics, diagnostics collection |
| L2 | Product support engineer | Logs, Gateway/proxy, known defects, config |
| L3 | Engineering on-call | Code defects, data repair, state machine / migration issues |
| Executive | CS lead + Eng manager | S1 > 2h, customer exec complaints, security |

## Escalation triggers

- S1 not mitigated within response target  
- Suspected data integrity / wrong-tenant leakage  
- License revoked incorrectly / commercial dispute blocking care path  
- Security vulnerability or credential leak  
- Repeated S2 same root cause (> 2 occurrences / week)

## Package for L2/L3

1. Triage form fields from [issue-triage.md](issue-triage.md)  
2. Diagnostics JSON (redacted)  
3. Correlation IDs  
4. Exact API paths + HTTP status  
5. Whether maintenance mode / recent upgrade occurred  
6. Customer business impact statement  

## Engineering boundaries (architecture)

- Do not ask L3 to “just update journey status in SQL” — transitions via ComplianceJourneyService only.  
- Do not bypass license soft gates without commercial + Eng approval.  
- RabbitMQ debugging stays in Gateway / messaging infra — not ComplianceService.

## CS parallel track

For relationship / renewal risk, also page CS per [../customer-success/escalation-procedures.md](../customer-success/escalation-procedures.md).
