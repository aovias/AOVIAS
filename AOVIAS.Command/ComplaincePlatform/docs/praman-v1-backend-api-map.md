# PRAMAN Backend API Map

## Identity APIs

POST /identity/login
POST /identity/register
POST /identity/refresh

---

## Tenant APIs

POST /onboarding/hospital        # Gateway → Onboard hospital (org + hospital + admin + JWT)
GET /tenant/{id}/organization-detail

---

## User Management

GET /users
POST /users/invite
PATCH /users/{id}/role
DELETE /users/{id}

---

## Framework Enrollment

GET /frameworks/catalog
POST /frameworks/enroll
GET /frameworks/enrolled

---

## Dashboard

GET /dashboard/summary
GET /dashboard/hospital

---

## Compliance Journey

GET /journeys/{journeyId}
GET /journeys/{journeyId}/chapters
POST /journeys/{journeyId}/transitions

---

## Evidence

POST /evidence/upload
GET /evidence/library
GET /evidence/{id}

---

## Reporting

GET /reports/compliance-score
GET /reports/audit-findings
GET /reports/nabh-readiness
