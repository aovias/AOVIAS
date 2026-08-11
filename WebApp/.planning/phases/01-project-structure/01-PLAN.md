---
phase: 01-project-structure
plan: 01
type: execute
wave: 1
depends_on: []
files_modified: []
autonomous: true

must_haves:
  truths:
    - All core directories exist (auth, guards, interceptors, services, layout, config)
    - All shared directories exist (components, ui, models, directives, pipes, utils)
    - All feature directories exist with subdirectories (pages, components, services, models, routes)
    - Compliance control center nested structure created
    - Existing login component preserved
    - Directory structure matches Angular standalone architecture best practices
  artifacts:
    - src/app/core/auth/
    - src/app/core/guards/
    - src/app/core/interceptors/
    - src/app/core/services/
    - src/app/core/layout/
    - src/app/core/config/
    - src/app/shared/components/
    - src/app/shared/ui/
    - src/app/shared/models/
    - src/app/shared/directives/
    - src/app/shared/pipes/
    - src/app/shared/utils/
    - src/app/features/auth/
    - src/app/features/dashboard/
    - src/app/features/journey/
    - src/app/features/tasks/
    - src/app/features/evidence/
    - src/app/features/audit/
    - src/app/features/reporting/
    - src/app/features/compliance-control-center/regulatory-governance/
    - src/app/features/compliance-control-center/organizational-control/
    - src/app/features/compliance-control-center/access-accountability/
    - src/app/features/compliance-control-center/compliance-infrastructure/
    - src/app/features/compliance-control-center/risk-escalation/
    - src/app/features/compliance-control-center/monitoring-traceability/
    - src/app/features/compliance-control-center/integration-system/
  key_links:
    - Each feature directory contains pages/, components/, services/, models/, routes.ts structure
    - Core directories ready for infrastructure components (Phase 3+)
    - Shared directories ready for reusable components (Phase 9+)
---

<objective>
Create the complete folder architecture for PRAMAN Operator Portal following Angular standalone architecture best practices.

Purpose: Establish the foundation for enterprise-scale Angular application with clear separation between core infrastructure, shared utilities, and feature modules.

Output: Complete directory structure with core/, shared/, and features/ hierarchies, including all nested compliance-control-center sub-features.
</objective>

<execution_context>
@.planning/REQUIREMENTS.md
@.planning/STATE.md
@.planning/phases/01-project-structure/01-RESEARCH.md
</execution_context>

<context>
@.planning/ROADMAP.md

Current project structure:
- src/app/login/ (existing - preserve)
- src/app/app.config.ts
- src/app/app.routes.ts
- src/app/app.ts

**Important:** Preserve existing login component. Do not move or modify it during structure creation.
</context>

<tasks>

<task type="auto">
  <name>Create Core Directory Structure</name>
  <files>
    src/app/core/auth/
    src/app/core/guards/
    src/app/core/interceptors/
    src/app/core/services/
    src/app/core/layout/
    src/app/core/config/
  </files>
  <action>
    Create all core infrastructure directories:
    1. Create `src/app/core/` directory
    2. Create subdirectories: `auth/`, `guards/`, `interceptors/`, `services/`, `layout/`, `config/`
    3. Each directory should be empty (no placeholder files needed)
    4. Verify directories exist using list_dir or terminal command
  </action>
  <verify>
    Run: `ls -la src/app/core/` and verify all 6 subdirectories exist
  </verify>
  <done>
    All core directories created and accessible. Structure ready for Phase 3 (layout), Phase 5 (guards), Phase 6 (auth), Phase 7 (interceptors), Phase 8 (services).
  </done>
</task>

<task type="auto">
  <name>Create Shared Directory Structure</name>
  <files>
    src/app/shared/components/
    src/app/shared/ui/
    src/app/shared/models/
    src/app/shared/directives/
    src/app/shared/pipes/
    src/app/shared/utils/
  </files>
  <action>
    Create all shared utility directories:
    1. Create `src/app/shared/` directory
    2. Create subdirectories: `components/`, `ui/`, `models/`, `directives/`, `pipes/`, `utils/`
    3. Each directory should be empty (no placeholder files needed)
    4. Verify directories exist
  </action>
  <verify>
    Run: `ls -la src/app/shared/` and verify all 6 subdirectories exist
  </verify>
  <done>
    All shared directories created. Structure ready for Phase 9 (UI components), Phase 10 (API services), and shared utilities.
  </done>
</task>

<task type="auto">
  <name>Create Features Directory Structure</name>
  <files>
    src/app/features/auth/
    src/app/features/dashboard/
    src/app/features/journey/
    src/app/features/tasks/
    src/app/features/evidence/
    src/app/features/audit/
    src/app/features/reporting/
    src/app/features/compliance-control-center/regulatory-governance/
    src/app/features/compliance-control-center/organizational-control/
    src/app/features/compliance-control-center/access-accountability/
    src/app/features/compliance-control-center/compliance-infrastructure/
    src/app/features/compliance-control-center/risk-escalation/
    src/app/features/compliance-control-center/monitoring-traceability/
    src/app/features/compliance-control-center/integration-system/
  </files>
  <action>
    Create all feature directories with required subdirectories:
    1. Create `src/app/features/` directory
    2. For each top-level feature (auth, dashboard, journey, tasks, evidence, audit, reporting):
       - Create feature directory (e.g., `src/app/features/auth/`)
       - Create subdirectories: `pages/`, `components/`, `services/`, `models/`
       - Create `routes.ts` file (empty file, will be populated in Phase 4)
    3. For compliance-control-center:
       - Create `src/app/features/compliance-control-center/` directory
       - For each sub-feature (regulatory-governance, organizational-control, access-accountability, compliance-infrastructure, risk-escalation, monitoring-traceability, integration-system):
         - Create sub-feature directory
         - Create subdirectories: `pages/`, `components/`, `services/`, `models/`
         - Create `routes.ts` file
    4. Verify all directories and routes.ts files exist
    5. Ensure existing `src/app/login/` component is NOT modified or moved
  </action>
  <verify>
    Run: `find src/app/features -type d` and verify:
    - All 7 top-level features exist with pages/, components/, services/, models/ subdirectories
    - compliance-control-center exists with all 7 sub-features
    - Each feature and sub-feature has routes.ts file
    - Existing login/ directory still exists at src/app/login/
  </verify>
  <done>
    All feature directories created with proper structure. Each feature has pages/, components/, services/, models/, and routes.ts. Compliance control center nested structure complete. Ready for Phase 4 (routing) and subsequent feature development.
  </done>
</task>

</tasks>

<verification>
After all tasks complete:

1. **Directory Structure Verification:**
   ```bash
   # Verify core structure
   ls -la src/app/core/
   # Should show: auth, guards, interceptors, services, layout, config
   
   # Verify shared structure
   ls -la src/app/shared/
   # Should show: components, ui, models, directives, pipes, utils
   
   # Verify features structure
   find src/app/features -maxdepth 2 -type d | sort
   # Should show all top-level features
   
   # Verify compliance-control-center nested structure
   find src/app/features/compliance-control-center -type d | sort
   # Should show all 7 sub-features
   ```

2. **Feature Subdirectory Verification:**
   ```bash
   # Check one feature has required structure
   ls -la src/app/features/dashboard/
   # Should show: pages/, components/, services/, models/, routes.ts
   ```

3. **Preservation Check:**
   ```bash
   # Verify existing login component still exists
   ls -la src/app/login/
   # Should show: login.component.ts, login.component.html, login.component.css
   ```

4. **No Placeholder Files:**
   - All directories should be empty except routes.ts files in features
   - No .gitkeep or placeholder files needed
</verification>

<success_criteria>
- [x] All core directories created (6 subdirectories)
- [x] All shared directories created (6 subdirectories)
- [x] All top-level feature directories created (7 features)
- [x] Each feature has pages/, components/, services/, models/ subdirectories
- [x] Each feature has routes.ts file
- [x] Compliance-control-center created with all 7 sub-features
- [x] Each sub-feature has pages/, components/, services/, models/ subdirectories
- [x] Each sub-feature has routes.ts file
- [x] Existing login component preserved and unchanged
- [x] Directory structure follows Angular standalone architecture best practices
- [x] Structure ready for subsequent phases (routing, guards, services, etc.)
</success_criteria>

<output>
After completion, create `.planning/phases/01-project-structure/01-01-SUMMARY.md` documenting the created structure.
</output>
