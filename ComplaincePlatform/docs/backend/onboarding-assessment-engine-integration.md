# Onboarding Assessment Engine Integration

This document describes how onboarding questionnaire answers are persisted into the **Knowledge Base Assessment engine** (`Assessment`, `AssessmentItem`, `AssessmentSummaryCalculator`) while retaining the existing **Compliance wizard JSON audit** trail.

---

## Before / after

### Before

```
Portal → Gateway POST /onboarding/onboard
       → Compliance POST /onboarding/wizard/assessments (JSON audit only)
```

Answers lived in `WizardOnboardingAssessment` as JSON. No KB `Assessment` / `AssessmentItem` rows were created from the wizard.

### After

```
Portal → Gateway POST /onboarding/onboard
       ├→ Compliance POST /onboarding/wizard/assessments (JSON audit — unchanged)
       └→ KB POST /assessments/from-wizard (per matched framework)
            → Assessment + AssessmentItems
            → AssessmentSummaryCalculator
            → assessmentId + summary returned to Gateway
```

Gateway sets `OnboardWizardResponseDto.AssessmentId` from the **primary KB assessment** (first matched framework), not from Compliance.

---

## Entity mapping (questionnaire answer → AssessmentItem)

| Wizard answer | Score | ComplianceStatus | HasActionPlan | DocumentationAdequate | Remarks |
|---------------|-------|------------------|---------------|----------------------|---------|
| `yes` | 5 | `Met` | false | true | question text |
| `no` | 1 | `NotMet` | **true** | false | question text |
| `not_sure` | 3 | `Partial` | **true** | false | question text |

- **`QuestionId`** (OE node GUID string) → **`AssessmentItem.NodeId`**
- **`HasActionPlan = true`** for scores ≤ 3 is a **wizard bootstrap** flag: onboarding has no action-plan UI, but `UpsertItemAsync` rejects low scores without an action plan.

Mapper: `KnowledgeBase/Application/AssessmentSummary/WizardAssessmentAnswerMapper.cs`

---

## APIs invoked during onboard

| Step | Service | Method | Path | Purpose |
|------|---------|--------|------|---------|
| 1 | Compliance | POST | `/api/v1/onboarding/wizard/assessments` | JSON audit (`WizardOnboardingAssessment`) |
| 2 | Knowledge Base | GET | `/api/v1.0/frameworks` | Map `frameworkCodes` → KB `frameworkId` |
| 3 | Knowledge Base | POST | `/api/v1.0/assessments/from-wizard` | Batch create/update assessment + items + summary |

KB batch request body:

```json
{
  "facilityId": "<hospitalId>",
  "frameworkId": "<kb-framework-guid>",
  "answers": [
    { "questionId": "<oe-node-guid>", "question": "...", "answer": "yes|no|not_sure" }
  ]
}
```

KB batch response (`ApiResponse<SubmitWizardAssessmentResponseDto>`):

```json
{
  "data": {
    "assessmentId": "<guid>",
    "summary": { "overallPercent": 85.0, "...": "..." }
  }
}
```

---

## Idempotency

`AssessmentAppService.SubmitWizardAnswersAsync` reuses an **open** assessment for the same `(FacilityId, FrameworkId)` created on the **same UTC calendar day**. On reuse it upserts items and recomputes the summary instead of creating a duplicate header.

Compliance wizard assessment save retains its own idempotent behavior (unchanged).

---

## Multi-framework behavior

When the user selects multiple framework codes:

1. Gateway loads KB frameworks and matches codes (case-insensitive).
2. For **each** matched framework, Gateway calls `POST /assessments/from-wizard` with **all** answers; KB filters to node IDs belonging to that framework.
3. One KB **`Assessment`** per matched framework.
4. Gateway response:
   - `AssessmentId` — first framework’s assessment id
   - `AssessmentIds` — all created/reused ids
   - `AssessmentSummary` — summary from the primary (first) assessment

---

## Gateway response extensions

`OnboardWizardResponseDto` (non-breaking additions):

| Field | Description |
|-------|-------------|
| `AssessmentId` | Primary KB assessment id |
| `AssessmentIds` | All KB assessment ids (one per framework) |
| `AssessmentSummary` | Summary from primary assessment |

Angular can adopt these on the completion step in a follow-up; submit path is unchanged.

---

## Known limitations

1. **503 / empty OE wizard UX** — If questionnaire load fails, the user may submit with no answers; both Compliance and KB saves are skipped when `assessmentAnswers` is empty.
2. **Bootstrap `HasActionPlan`** — `no` and `not_sure` answers set `HasActionPlan = true` without a real action plan; downstream journeys should treat these as “needs follow-up.”
3. **Same-day idempotency only** — Re-running onboard on a later day creates a new open assessment for the same facility + framework.
4. **KB framework code match** — If selected codes do not exist in KB, KB assessment save is skipped (Compliance audit and enroll/journey still proceed if Compliance knows the code).

---

## Verification

### Unit tests

| Test | Location |
|------|----------|
| Answer mapping | `Knowledgebase.Tests/WizardAssessmentAnswerMapperTests.cs` |
| KB HTTP integration | `Knowledgebase.Tests/KnowledgeApiIntegrationTests.cs` — `Post_assessments_from_wizard_persists_items_and_returns_summary` |
| Gateway dual-write | `Gateway.UnitTests/OnboardWizardCommandHandlerTests.cs` |

### Manual smoke (KB direct)

```http
POST /api/v1.0/assessments/from-wizard
Content-Type: application/json

{
  "facilityId": "00000000-0000-0000-0000-000000000001",
  "frameworkId": "<seeded-framework-id>",
  "answers": [
    { "questionId": "<oe-node-id>", "question": "Sample OE", "answer": "yes" }
  ]
}
```

Expect HTTP 200, non-empty `assessmentId`, and `summary.overallPercent` populated.

### Manual smoke (full onboard via Gateway)

Use an authenticated `POST /api/v1/onboarding/onboard` with `assessmentAnswers` and `frameworkCodes`. Confirm:

1. Compliance row in `WizardOnboardingAssessment` (JSON payload).
2. KB `Assessments` + `AssessmentItems` for `facilityId = hospitalId`.
3. Response includes KB `assessmentId` and optional `assessmentSummary`.

---

## Files changed

| Area | Files |
|------|-------|
| KB DTOs | `KnowledgeBase/Application/DTOs/KnowledgebaseDtos.cs` |
| Mapper | `KnowledgeBase/Application/AssessmentSummary/WizardAssessmentAnswerMapper.cs` |
| Service | `KnowledgeBase/Application/Services/AssessmentAppService.cs` |
| Repository | `IAssessmentRepository.cs`, `AssessmentRepository.cs` |
| KB API | `KnowledgeBase/Controllers/V1/AssessmentsController.cs` |
| Gateway | `OnboardWizardCommandHandler.cs`, `OnboardingWizardDtos.cs` |

**Not changed:** Questionnaire generator, Angular assessment UI, Compliance `WizardOnboardingAssessment` schema.
