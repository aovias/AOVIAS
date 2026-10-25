# Sprint 17 — Clinical Indicators & Quality Performance Intelligence Report

## Executive Summary

Sprint 17 delivers the **Quality Intelligence** module for PRAMAN — a continuous clinical measurement platform that transforms operational data into measurable quality outcomes. Unlike Patient Safety (Sprint 16), which captures discrete incidents, clinical indicators are **ongoing measurements** with threshold bands, trend analysis, department scorecards, and executive intelligence.

The module aligns with NABH by default and remains extensible for JCI, DNV, ISO, and other frameworks. All analytics, CAPA, readiness, and Copilot capabilities reuse existing platform services — no duplicate engines were introduced.

---

## Architecture

### Measurement Flow

```
Operational Data (Incidents, Findings, Manual Entry)
    ↓
Indicator Measurement (IndicatorMeasurement)
    ↓
Clinical Indicator (aggregate with current value)
    ↓
Threshold Evaluation (ClinicalIndicatorTrendEngine)
    ↓
Trend Direction (Improving / Stable / Declining / Volatile)
    ↓
Department Scorecard / Executive Dashboard
    ↓
Investigation Recommendations (Advisory only)
    ↓
Leadership Action
```

**Indicators are measurements, not incidents.** Threshold breaches generate recommendations — never automatic remediation or investigation creation.

### Backend Structure

| Layer | Location |
|-------|----------|
| Domain aggregates | `ComplianceService/Domain/ClinicalIndicator.cs`, `IndicatorMeasurement.cs`, `IndicatorThresholdHistory.cs`, `IndicatorAuditTrail.cs` |
| Enums | `ComplianceService/Domain/Enums/IndicatorCategory.cs`, `IndicatorEnums.cs` |
| DTOs | `ComplianceService/Application/DTOs/QualityIntelligenceDtos.cs` |
| Services | `ClinicalIndicatorQueryService`, `ClinicalIndicatorCommandService`, `ClinicalIndicatorMetricsService`, `ClinicalIndicatorTrendEngine`, `ClinicalIndicatorCatalogSeeder` |
| API | `Controllers/V1/QualityController.cs` |
| Gateway | `Gateway/Controllers/QualityV1ProxyController.cs` |
| Copilot rule | `Application/Copilot/Rules/Quality/IndicatorThresholdBreachRule.cs` |
| Migration | `Migrations/*Sprint17_ClinicalIndicators*` |

### Frontend Structure

| Layer | Location |
|-------|----------|
| Feature module | `WebApp/src/app/features/quality-intelligence/` |
| Route | `/app/quality-intelligence` |
| Shell nav | Dashboard, Clinical Indicators, Department Scorecards, Quality Trends, Benchmarking, Threshold Management, Investigations, Analytics, Executive Summary, Settings |

---

## Business Rules

1. **Indicator ≠ Incident** — indicators measure rates, scores, and compliance percentages; incidents remain in Patient Safety
2. **Threshold breaches are advisory** — recommendations only; no auto-investigation or auto-CAPA
3. **Threshold history is immutable** — `PATCH /quality/thresholds` appends to `IndicatorThresholdHistory`; prior configs are never overwritten
4. **Trend calculations are auditable** — centralized in `ClinicalIndicatorTrendEngine` (SSOT)
5. **Department scorecards are projections** — composite scores derived from indicators + delegated Analytics Hub metrics
6. **Benchmark history is immutable** — comparisons use stored measurement periods
7. **Cross-module linkage** — indicators link to documentation, incidents, findings, deficiencies, CAPA, and compliance objective elements via deep links
8. **Reuse platform engines** — Analytics Hub, Copilot Recommendation Engine, Documentation, Patient Safety, Compliance Maturity

---

## Clinical Indicator Domain

### Aggregate Fields

| Field | Purpose |
|-------|---------|
| IndicatorCode | Unique org-scoped code (e.g. `QI-MED-ERR`) |
| Name / Description | Human-readable definition |
| Category | Taxonomy (Medication Safety, Patient Falls, HAI, etc.) |
| Department | Owning department |
| MeasurementUnit | Percent, Rate, Score, Ratio |
| CalculationMethod | Formula, Manual, SystemDerived |
| Numerator / Denominator | Definition strings + current values |
| CurrentValue / TargetValue | Latest computed measurement vs goal |
| Green / Yellow / Red / Critical Threshold | Configurable bands |
| LowerIsBetter | Inverts threshold breach logic for rate indicators |
| Trend | Improving, Stable, Declining, Volatile, Unknown |
| Frequency | Daily, Weekly, Monthly, Quarterly, Yearly |
| Owner | User accountability |
| FrameworkMapping | NABH (default), extensible |
| Status / RiskLevel / ThresholdStatus | Computed lifecycle state |
| DataSource | Origin system or manual |
| LastCalculatedAt | Measurement timestamp |
| RelatedDocumentationJson | Controlled document links |
| RelatedIncidentCategoriesJson | Category mapping for Patient Safety correlation |

---

## Indicator Catalog

Default NABH-aligned catalog (18 indicators) seeded per organization on first API access via `ClinicalIndicatorCatalogSeeder`:

| Code | Indicator | Category |
|------|-----------|----------|
| QI-MED-ERR | Medication Error Rate | Medication Errors |
| QI-FALL | Patient Fall Rate | Patient Falls |
| QI-HAI | Hospital Acquired Infection Rate | HAI |
| QI-SSI | Surgical Site Infection Rate | SSI |
| QI-PI | Pressure Injury Rate | Pressure Injury |
| QI-READMIT | 30-Day Readmission Rate | Readmissions |
| QI-MORT | Risk-Adjusted Mortality Index | Mortality |
| QI-NSI | Needle Stick Injury Rate | Needle Stick Injuries |
| QI-BMW | Biomedical Waste Compliance | Biomedical Waste |
| QI-EQUIP | Critical Equipment Uptime | Equipment Downtime |
| QI-EMR | Emergency Response Time Compliance | Emergency Response |
| QI-PSAT | Patient Satisfaction Score | Patient Satisfaction |
| QI-COMP | Complaint Resolution Within SLA | Complaint Resolution |
| QI-LAB | Laboratory Error Rate | Laboratory Errors |
| QI-RAD | Radiology Report Turnaround Compliance | Radiology Errors |
| QI-HH | Hand Hygiene Compliance | Hand Hygiene Compliance |
| QI-BTR | Blood Transfusion Reaction Rate | Blood Transfusion Reactions |
| QI-MED-SAF | Medication Safety Composite Score | Medication Safety |

Future categories are configurable via `IndicatorCategory` enum extension or admin catalog management (roadmap).

---

## Calculation Model

### Threshold Evaluation (`ClinicalIndicatorTrendEngine.EvaluateThreshold`)

1. If no current value → `Unknown`
2. Check Critical → Red → Yellow bands (respecting `LowerIsBetter`)
3. If within green threshold or within 5% of target → `Green`
4. Fallback tolerance check against target → `Green` or `Yellow`

### Trend Direction (`ClinicalIndicatorTrendEngine.ComputeTrend`)

- Requires ≥2 measurement points
- **Volatile**: high variance in period-over-period deltas
- **Stable**: change ≤3% of first value (min 0.01)
- **Improving / Declining**: directional change beyond threshold
- Inverted when `LowerIsBetter` is true

### Moving Average

`MovingAverage(values, window=3)` — used in trend series for smoothing.

### Quality KPIs (`ClinicalIndicatorMetricsService`)

| KPI | Formula |
|-----|---------|
| Overall Quality Score | `(green×100 + yellow×60) / active indicators` |
| Patient Safety Score | % of safety-category indicators in Green band |
| Accreditation Risk Score | `(critical×12) + (declining×5) + (readinessGap×0.4)` capped at 100 |
| CAPA Effectiveness | Delegated to `IAnalyticsAggregationService.GetCapaMetricsAsync` |

---

## Threshold Model

| Band | Meaning |
|------|---------|
| Green | Within target / acceptable performance |
| Yellow | Warning — trending toward breach |
| Red | Breached — requires leadership attention |
| Critical | Severe breach — accreditation risk |

### PATCH Workflow

`PATCH /quality/thresholds` with `PatchIndicatorThresholdsRequestDto`:

1. Append snapshot to `IndicatorThresholdHistories` (immutable)
2. Update current values on `ClinicalIndicator`
3. Re-evaluate `ThresholdStatus` via trend engine
4. Append `IndicatorAuditTrail` entry
5. Refresh computed fields across org indicators

---

## Trend Engine

Supported periods: Daily, Weekly, Monthly, Quarterly, Yearly.

Output directions: Improving, Stable, Declining, Volatile, Unknown.

Trend series exposed via `GET /quality/trends?period=Monthly&indicatorId={optional}`.

No duplicate analytics calculations — dashboard KPIs and trend engine share `ClinicalIndicatorMetricsService` and `ClinicalIndicatorTrendEngine`.

---

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/quality/dashboard` | Enterprise KPI cards |
| GET | `/quality/indicators` | Filtered, sorted, paginated indicator grid |
| GET | `/quality/indicators/{id}` | Detail with history, linkages, recommendations |
| PATCH | `/quality/thresholds` | Threshold configuration (append-only history) |
| GET | `/quality/scorecards` | Department quality scorecards |
| GET | `/quality/trends` | Trend series with moving averages |
| GET | `/quality/benchmarking` | Period comparisons (CurrentMonth, PreviousMonth, PreviousQuarter, PreviousYear) |
| GET | `/quality/executive` | Executive intelligence (read-only) |
| GET | `/quality/analytics` | Analytics Hub integration surface |

All proxied via Gateway `QualityV1ProxyController`.

---

## Integrations

### Patient Safety (Phase 12)
- Detail view shows related incidents by department
- Deep links to `/app/patient-safety/command-center?selected={id}`
- Patient Safety Score derived from safety-category indicators

### Documentation (Phase 11)
- `RelatedDocumentationJson` on indicator aggregate
- Investigation recommendations link to `/app/documentation/register`
- Documentation revision suggestions when trend is Declining (advisory)

### Findings, Deficiencies, CAPA
- Related findings by department → `/app/audit/findings`
- Related deficiencies → `/app/audit/deficiency-command-center`
- Related CAPAs → `/app/audit/capa/{id}`
- Volatile trend triggers "Review CAPA effectiveness" recommendation

### Compliance Maturity (Phase 13)
- `ObjectiveElementId` linkage with readiness impact scoring
- Compliance context in indicator detail (clause reference, verification status)

### Copilot (Phase 14)
- `IndicatorThresholdBreachRule` — advisory when Red/Critical indicators detected
- Deep link: `/app/quality-intelligence/indicators?selected={id}`
- Persona routing: `QualityIntelligence` → QualityManagerAssistant; executive views → ExecutiveAdvisor
- `AiPageContext`: `QualityIntelligence`

### Analytics Hub (Phase 17)
- `GET /quality/analytics` delegates CAPA, SLA, readiness to `IAnalyticsAggregationService`
- Exposes indicator health, department heatmap, quality trends, journey readiness
- No separate KPI calculation engine

### External Auditor (Phase 16)
- Read-only access via existing `EXTERNAL_AUDITOR` role
- Indicator history, thresholds, investigations, CAPA/documentation linkage visible through standard compliance scope APIs

---

## Department Scorecards (Phase 8)

Each department receives projected scores:

- Quality Score (indicator green-band ratio)
- Incident Rate (related incidents)
- CAPA Completion (Analytics Hub)
- SLA Health (Analytics Hub)
- Evidence Completeness (readiness proxy)
- Documentation Health (placeholder)
- Clinical Indicator Score (department-filtered indicators)
- Executive summary narrative

---

## Investigation Recommendations (Phase 9)

Generated in `BuildInvestigationRecommendations` when:

| Trigger | Recommendation |
|---------|----------------|
| Red/Critical threshold | Investigate breach, Review related incidents |
| Declining trend | Review related SOPs |
| Volatile trend | Review CAPA effectiveness |

All recommendations include `IsAdvisory = true`. Copilot rule `quality.indicator_threshold_breach` surfaces org-level breach summary.

---

## Security

| Role | Access |
|------|--------|
| Quality Manager | Full indicator workspace, threshold management |
| Department Head | Department scorecards, assigned indicators |
| Medical Superintendent / Executive | Executive summary, analytics (read-only) |
| External Auditor | Read-only indicator history and linkages |

All threshold changes recorded in `IndicatorThresholdHistory` and `IndicatorAuditTrail`.

---

## Validation

- Backend builds with Sprint 17 migration (`Sprint17_ClinicalIndicators`)
- Gateway proxy routes all quality endpoints
- Frontend Quality Intelligence workspace at `/app/quality-intelligence`
- Angular build succeeds with enterprise shell navigation
- KPI cards reuse existing dashboard patterns
- CSV export on Clinical Indicators grid
- Threshold PATCH enforces append-only history
- Copilot persona routes to QualityManagerAssistant on Quality Intelligence pages

---

## Future Roadmap

- Configurable indicator catalog admin UI (add/edit categories without enum change)
- Hospital-to-hospital benchmarking network
- Training Readiness on department scorecards
- Automated measurement ingestion from EMR/LIS interfaces
- Copilot monthly quality report generation via AIOrchestrator
- Documentation revision draft creation from deteriorating indicators
- Forecast readiness using historical trend regression
- Consolidate Patient Safety dashboard score with `ClinicalIndicatorMetricsService` SSOT

---

## Success Criteria

✓ Quality Intelligence workspace implemented  
✓ Clinical Indicator domain established  
✓ Enterprise indicator workspace with search, filters, export  
✓ Threshold management with immutable history  
✓ Trend engine (SSOT)  
✓ Department scorecards  
✓ Investigation recommendations (advisory)  
✓ Documentation integration  
✓ Patient Safety integration  
✓ Compliance integration  
✓ Compliance Copilot integration  
✓ Analytics Hub integration  
✓ Executive intelligence dashboard  
✓ External Auditor read-only access pattern  
✓ No duplicated business logic  
