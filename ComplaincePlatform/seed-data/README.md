# KnowledgeBase seed data

## NABH Hospital Accreditation Standard — 6th Edition (January 2025)

### Source inputs

- **CSV extracts** (working copies): `seed-data/source/*.csv` (synced from `v:\DevelopmentRepo\New folder\`).
- **Official PDF** (reference for text / hierarchy):  
[NABH Hospital Accreditation Standard 6th Edition (Jan 2025)](https://nabh-portal-live.s3.ap-south-1.amazonaws.com/wp-content/uploads/2025/07/13111206/NABH-Hospital-Accreditation-Standard-6th-Edition-January-2025.pdf)  
Download manually to `seed-data/source/nabh-hospital-6e.pdf` if you need offline review (add to `.gitignore` if large).

### Configuration

- `seed-data/config/framework-mapping.json` — target framework Id (`0FE8A9C0-9EA0-4970-9097-D4F73D45DCA7`) and remap note for CSV hierarchy that referenced Entry Level.

### Generated SQL


| File                            | Purpose                                                                                                      |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `sql/seed-nabh-hospital-6e.sql` | Idempotent seed (Framework → Scoring → Classifications → Nodes → RequirementDetails/Scores → KnowledgeItems) |
| `sql/verify-rowcounts.sql`      | Row counts + orphan `ParentId` check                                                                         |
| `sql/rollback-seed.sql`         | Deletes seed by framework Id (**set `@Confirm = 1`**)                                                        |


### KnowledgeItem text columns

- **Explanation** — `{Code}: {Title}. {Description}` from the OE row (so each row is distinct when codes/titles differ).
- **WhyItMatters / HowToComply / AuditChecklist** — templates in `seed-data/config/knowledge-item-templates.json` plus **parent standard** (`Standards.csv` via `StandardId`) and **chapter** (`Masterchapters.csv` via the standard’s `ChapterId`): requirement text and chapter name change per OE.
- **EvidenceRequired** — intro names `{Code}` and `{StandardCode}`; the **body** is chosen by **`EvidenceType`** (`Document`, `Interview`, `Observation`, …) with a `default` fallback.
- **RiskLevel** — from **`ItemWeight`**: ≥ 2 → `High`, > 0 → `Medium`, else `Low`.
- **ImpactLevel** — from **`ItemWeight`**: ≥ 2 → `Critical`, > 0 → `High`, else `Medium` (no longer a single constant for all rows).
- **Tags** — `NABH;6E;{ChapterCode};OE;{Code};{EvidenceType};{StandardCode}`.

References for template tone: [NABH standards explorer](https://portal.nabh.co/NABHStandards.aspx); confirm OE tier (CORE / COMMITMENT / …) in the **official PDF** if you need exact classification per element.

To change tone or wording, edit `knowledge-item-templates.json` and regenerate the SQL.

### Regenerate

```powershell
pwsh ./seed-data/tools/Generate-NabhSeed.ps1
```

To reload `KnowledgeItems` after changing templates, delete existing rows for that framework (or use your maintenance window), then re-run `seed-nabh-hospital-6e.sql`.

### Apply (example)

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d KnowledgeBase -E -i ./seed-data/sql/seed-nabh-hospital-6e.sql
```

Requires `KnowledgeBase` database with schema from EF migrations applied.

### Planning

Phase `.planning/phases/03-nabh-knowledgebase-seed/` — `column-mapping.md`, `03-RESEARCH.md`.