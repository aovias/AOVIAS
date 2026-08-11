# CRP Phase 7 — Workforce Onboarding Pipeline

| Step | Owner | Idempotency |
|------|-------|-------------|
| CreateInvitation | Org | Pending unique email |
| SendEmail | Org | Token hash stored |
| Redeem | Gateway | Token single-use |
| IdentityEnsure | Identity | Register or authenticated user |
| PersonEnsure | Person | Create/upsert by email |
| MembershipCreate | Org AcceptForUser | MembershipId on invite |
| EmploymentCreate | Workforce internal | EmploymentId on invite |
| Dept/Position/Manager | Workforce | Best-effort |
| SeatAssign | License | SeatAssigned flag |
| MarkAccepted | Org pipeline | Clears token |
