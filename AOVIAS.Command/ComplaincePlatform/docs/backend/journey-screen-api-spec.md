# Journey screen API (backend notes)

This document supplements the WebApp journey-screen contract with **implementation-specific** details maintained in this repository.

## Evidence upload

### JSON register (supported)

`POST /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence` with `Content-Type: application/json` accepts `RegisterOeEvidenceRequest`: `evidenceType`, `fileUrl` (reference URL), optional `title`, `metadata`. The service creates an evidence record and links it to the OE.

### Multipart file upload (supported)

The same path with `Content-Type: multipart/form-data` accepts a part named **`file`** plus form fields: **`evidenceType`** (required), optional **`title`**, **`metadata`**. The service stores the file under `App_Data/evidence_uploads/...` (ContentRoot) and sets `FileUrl` to a `local://...` reference.

### Limits

Maximum request body size for multipart evidence is **10 MB** on ComplianceService and Gateway. Payloads over the limit receive **413 Payload Too Large** from the host.

### DELETE

`DELETE /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}` removes the link and deletes the evidence row when it is not referenced elsewhere.
