# Customer Communication Templates

Replace bracketed fields. Keep tone calm and specific. No blame.

## T1 — Incident acknowledge (S1/S2)

**Subject:** AOVIAS Command — We are investigating [brief symptom]

Hello [Name],

We have received your report regarding [symptom] affecting [org / environment] as of [UTC time]. Ticket: [ID].

Current status: Investigating.  
Next update by: [UTC time].

Please keep [Correlation ID] handy. If you can, download diagnostics from Settings → Platform → Commercial Ops (or `GET /api/v1/commercial/diagnostics`) and attach to the ticket.

— AOVIAS Support

## T2 — Maintenance window

**Subject:** Scheduled maintenance — AOVIAS Command [date]

We will perform [upgrade / backup / infra] maintenance on [date] from [start UTC] to [end UTC].

During this window the platform may return HTTP 503 (maintenance mode).  
Expected impact: [login / all features].  
Rollback plan: available via standard ops rollback procedure.

— AOVIAS Operations

## T3 — Resolved

**Subject:** Resolved — [ticket ID]

The issue [summary] is resolved as of [UTC].  
Root cause (customer-safe): [one sentence].  
Preventive action: [one sentence].

Please confirm access. Contact us if symptoms return with a new Correlation ID.

## T4 — License expiry / grace

**Subject:** Action required — AOVIAS license entering grace / expiry

Your AOVIAS license for organization [ID/name] enters [Grace/Expiry] on [date].

Please contact your AOVIAS CSM for a renewal key (manual issue — no in-app payment). Activation guide: provided in your onboarding pack.

## T5 — Request more info

We need the following to continue: organization ID, environment URL, VERSION, Correlation ID, and a diagnostics pack. KB: login / license articles in your support pack.
