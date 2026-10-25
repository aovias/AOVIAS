# 09 — AI Workflow Assistant + ACE

**Phase:** 5.8 + ACE  
**Sprint:** 5  
**Date:** 2026-07-26

## Workflow assistant

| Item | Implementation |
|------|----------------|
| Service | `Application/AI/Workflow/AiWorkflowHookService.cs` |
| Hooks | EvidenceUploaded, AuditClosed, IncidentClosed, TrainingCompleted |
| Side effects | Creates ACE insight + `ProactiveSuggestionStore` entry |
| Guarantee | Never mutates regulatory state |
| API | `POST /api/v1/ai/workflow/hooks/{eventType}` |

## ACE (Autonomous Compliance Engine)

| Item | Implementation |
|------|----------------|
| Engine | `Application/AI/Ace/AceEngine.cs` |
| Store | `IAceInsightStore` / EF `AceInsights` |
| Monitor | `AceMonitorHostedService` (interval via `AIAce:MonitorIntervalMinutes`) |
| Notify | `AceNotificationBridge` → CommunicationService `internal/communication/events` (best-effort) |
| APIs | `GET /api/v1/ai/ace/insights`, `POST /api/v1/ai/ace/insights/{id}/acknowledge` |

## Feature flags

- `EnableAIProactive` remains **false** by default.
- ACE list/acknowledge work when **Read** is enabled (insights are advisory).
- Proactive suggestion entity endpoint still gated by proactive flag on the AIOrchestrator proactive route; ACE does not require it.

## Communication bridge

If `Services:CommunicationService:BaseUrl` is empty or notify fails, insights are stored and logged only.

## Exit criteria

Hooks create advisory suggestions; ACE monitor can seed prioritized insights; list + acknowledge APIs work under Read.
