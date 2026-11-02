# AccredaOne documentation

Documentation in this repository is an engineering record and traceability layer. **Canonical** product and architecture documentation is in Confluence (space **ACCREDAONE**).

- **Index:** [traceability/CONFLUENCE-INDEX.md](traceability/CONFLUENCE-INDEX.md)
- **Register:** [traceability/REGISTER.md](traceability/REGISTER.md)
- **Gates:** [../GOVERNANCE-STATUS.md](../GOVERNANCE-STATUS.md)

G1 — Programme readiness gate has **not passed**. Phase 1 is **not authorized**. **D1-01** and **D2** are **approved in Confluence**; local folders may hold pointers or mirrors only.

## Areas

| Path | Contents |
| --- | --- |
| [product/](product/README.md) | Canonical **D1-01** pointer; optional local notes. |
| [requirements/](requirements/README.md) | Canonical **D2** pointer; optional local REQ mirrors. |
| [decisions/](decisions/README.md) | Local **DEC** series (engineering decisions not in Confluence ADRs). |
| [domain/](domain/README.md) | Domain modelling (empty until assigned). |
| [architecture/](architecture/README.md) | Architecture area; canonical **ADR-002–ADR-011** in Confluence. |
| [security/](security/README.md) | Identity and security pointers; **CI-01/CI-02** draft. |
| [content-governance/](content-governance/README.md) | Source rights and content-use rules. |
| [delivery/](delivery/README.md) | Setup, tasks, test plans, DEV-01 reports. |
| [traceability/](traceability/CONVENTION.md) | Citation rules, Confluence index, register. |

## Separation

| Concern | Location | Rule |
| --- | --- | --- |
| Product documentation | Confluence + `docs/product/` | Confluence is canonical. |
| Prototype experimentation | `prototypes/` | Synthetic-data boundary only. |
| Future production application | `application/` | No production code while Phase 1 is not authorized. |

Templates live in [`../templates/`](../templates/README.md). A file created from a template is not approved unless it matches canonical Confluence status and gates allow the work.
