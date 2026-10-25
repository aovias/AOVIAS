# Customer FAQs — AOVIAS Command

## Product

**What is AOVIAS Command?**  
Enterprise healthcare compliance platform (PRAMAN codebase) for accreditation readiness, quality governance, and optional AI assistance.

**Does it guarantee NABH/accreditation success?**  
No. It provides operational control and evidence integrity; accreditation bodies make independent decisions.

**Is AI required?**  
No. Core runs on `core.compliance`. AI requires `ai.assist` / `ace.insights`.

## Licensing & pricing

**Can we buy online with a credit card?**  
Not in v1.0.0—quote-led licensing. Contact sales@aovias.example.

**What are entitlements?**  
Feature codes on the license key that soft-gate modules (e.g., `governance.risk`).

**What happens when a license expires?**  
Status moves through Grace then Expired per licensing platform rules; renew with commercial ops.

## Deployment

**SaaS or on-prem?**  
v1.0.0 commercial packaging supports customer-hosted / compose deployments. Managed SaaS may be offered separately by AOVIAS ops—confirm in the quote.

**Which databases?**  
SQL Server catalogs per service; see deployment guide.

**Do you support HA?**  
Resilience patterns ship (circuit breakers, etc.); full multi-region HA remains an operations responsibility.

## Security & privacy

**Is patient data required?**  
Compliance evidence should avoid unnecessary PHI. AI prompts must not include patient identifiers.

**Where is the security whitepaper?**  
`commercial-launch/sales/security-whitepaper.md`

## Support

**Where are user docs?**  
`docs/commercial-readiness/phase-9/published/` and the website Docs page.

**How do we report a bug?**  
Include VERSION, environment, organization ID, and correlation ID—never PHI.
