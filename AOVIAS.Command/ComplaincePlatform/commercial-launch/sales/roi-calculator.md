# ROI Calculator — AOVIAS Command (Worksheet)

**Purpose:** Estimate annual value for a single hospital accreditation program. Replace assumptions with customer numbers in discovery.

## Inputs (fill with customer)

| Input | Symbol | Example |
|-------|--------|---------|
| QM / coordinator FTE fully loaded cost | C_qm | $60,000 |
| Hours/week on spreadsheet coordination | H_coord | 12 |
| Department leads impacted | N_dept | 8 |
| Hours/month each lead spends on evidence chase | H_dept | 4 |
| Avg loaded hourly for clinical leads | R_dept | $40 |
| External consultant days/year avoided | D_cons | 10 |
| Consultant day rate | R_cons | $800 |
| Cost of one delayed accreditation event (estimate) | L_delay | $50,000 |
| Probability delay avoided with better readiness | P_delay | 0.15 |
| AOVIAS annual license + support (quote) | Cost_aov | *(sales quote)* |

## Formulas

```
Annual_coord_save = (H_coord * 52) * (C_qm / 2080)
Annual_dept_save  = N_dept * H_dept * 12 * R_dept
Annual_cons_save  = D_cons * R_cons
Risk_value        = L_delay * P_delay

Gross_annual_benefit = Annual_coord_save + Annual_dept_save + Annual_cons_save + Risk_value
Net_annual_benefit   = Gross_annual_benefit - Cost_aov
Payback_months       = Cost_aov / (Gross_annual_benefit / 12)
```

## Example (illustrative only)

Using the example inputs and `Cost_aov = $45,000`:

| Component | Approx. |
|-----------|---------|
| Coord save | ~$18,000 |
| Dept save | ~$15,360 |
| Consultant save | $8,000 |
| Risk value | $7,500 |
| **Gross** | **~$48,860** |
| **Net** | **~$3,860** |
| Payback | **~11 months** |

## Talking points

- Soft benefits (audit stress, leadership visibility) sit outside the formula—call them out separately.
- AI entitlements may reduce drafting time; do not double-count with coordinator hours.
- Never promise a specific accreditation outcome.

## Output for proposal

Paste Gross / Net / Payback into [proposal-template.md](./proposal-template.md) with customer-confirmed inputs.
