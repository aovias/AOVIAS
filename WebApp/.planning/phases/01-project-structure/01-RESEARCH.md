# Phase 01: Project Structure - Research

## Research Objective
Understand best practices for Angular standalone architecture folder structure in enterprise applications to create a scalable, maintainable codebase foundation.

## Key Findings

### 1. Angular Standalone Architecture Pattern
- **Modern Approach**: Angular 16+ uses standalone components, eliminating NgModules boilerplate
- **Direct Imports**: Components import dependencies directly rather than through module declarations
- **Functional Interceptors**: Angular 20+ uses `HttpInterceptorFn` functional pattern instead of class-based interceptors
- **Registration**: Interceptors registered via `provideHttpClient(withInterceptors([...]))` in `app.config.ts`

### 2. Folder Structure Best Practices

**Core/Shared/Features Pattern** (validated against Angular official guidance):
- **Core** — Singleton services, interceptors, guards (auth, HTTP handling)
  - Should contain: auth, guards, interceptors, services, layout, config
  - Purpose: Application-wide infrastructure, loaded once
- **Shared** — Reusable UI components, pipes, directives (no business logic)
  - Should contain: components, ui, models, directives, pipes, utils
  - Purpose: Cross-feature reusable code
- **Features** — Domain-specific components (dashboard, journey, tasks, etc.)
  - Should contain: pages, components, services, models, routes
  - Purpose: Business domain functionality

### 3. Feature Module Structure
Each feature should be self-contained with:
- `pages/` — Route components (lazy-loaded)
- `components/` — Feature-specific UI components
- `services/` — Feature business logic
- `models/` — TypeScript interfaces/types
- `routes.ts` — Feature routing configuration

### 4. Compliance Control Center Structure
The nested structure for `compliance-control-center` with sub-features (regulatory-governance, organizational-control, etc.) is appropriate for domain-driven design. Each sub-feature follows the same pattern as top-level features.

### 5. Current Project State
- Angular 21.1.0 standalone application initialized
- Basic routing with login component exists at `src/app/login/`
- No infrastructure components yet
- Need to preserve existing login component during structure creation

## Implementation Considerations

### Directory Creation Strategy
1. Create empty directory structure first (all folders)
2. Add index.ts barrel exports where appropriate
3. Preserve existing `login/` component (will move to `features/auth/` later or keep as-is)
4. Ensure TypeScript path mappings in `tsconfig.json` if needed

### Naming Conventions
- **Directories**: kebab-case (e.g., `compliance-control-center`)
- **Files**: kebab-case for components/services (e.g., `auth.service.ts`)
- **Classes**: PascalCase (e.g., `AuthService`)
- **Barrel Exports**: `index.ts` files for clean imports

### Angular Material Integration
- Will be added in Phase 9, but structure should anticipate Material component placement
- `shared/ui/` directory ready for Material-based components

## Verification Points
- All specified directories created
- Existing login component preserved
- Structure aligns with Angular standalone best practices
- Ready for subsequent phases (environment config, routing, etc.)

## Research Complete
