# T3 — AI Section Overview

**Date:** 2026-07-20  
**Sprint:** T3 — Artificial Intelligence

---

## Purpose

The Artificial Intelligence section explains how AOVIAS applies AI as a **platform capability** — assisting people, preserving accountability, and reinforcing institutional trust. It is philosophy-driven, not marketing-driven.

## Guiding line

> Artificial Intelligence should amplify responsible human judgment, not replace it.

## Pages

| Page | Path |
| --- | --- |
| AI at AOVIAS | `/technology/ai` |
| AI Principles | `/technology/ai/principles` |
| AI Capabilities | `/technology/ai/capabilities` |
| Human-in-the-Loop | `/technology/ai/human-in-the-loop` |
| Responsible AI | `/technology/ai/responsible` |
| AI Roadmap | `/technology/ai/roadmap` |

## Architecture

- Feature module: `apps/website/src/features/technology/ai/`
- Routes: `apps/website/src/app/(technology)/technology/ai/**`
- Registry: Intelligence group marked `live` with `hrefOverride`s
- Layout: existing `TechnologyLayout` + marketing / brand components
- Diagrams: `PlatformIntelligence`, `HumanAiCollaboration`

## Positioning

AI is nested under Technology alongside Platform. It is not a standalone product story. No models, vendors, or infrastructure details are exposed.
