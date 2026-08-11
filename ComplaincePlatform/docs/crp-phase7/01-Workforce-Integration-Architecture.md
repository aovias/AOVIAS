# CRP Phase 7 — Workforce Integration Architecture

Email-invite default (`UseEmailInvites=true`). Pipeline: Invite → Token email → Redeem → Identity ensure → Person ensure → Membership → Employment → Dept/Position/Manager → Seat → Roles → Activate.

**CRP change control:** Phase 0 BR-W3 superseded for invitation accept — Employment is created and linked. Signup still does not create Employment.

Domains: Identity (auth), Organization (membership/invites), Workforce (employment), Licensing (seats), Gateway (orchestration).
