# PRAMAN Angular Architecture Rules

**Version:** 1.0  
**Last Updated:** 2025-01-20  
**Status:** Official Policy

---

## Constitution Reference

This document is the frontend architecture implementation standard under:

- `V:\DevelopmentRepo\V1.0\docs\PRAMAN-Architecture-Constitution.md`

If any rule here conflicts with the PRAMAN Architecture Constitution, the constitution takes precedence.

---

## 1. Purpose

PRAMAN is a **Healthcare Compliance SaaS Platform** designed to help hospitals manage accreditation, compliance monitoring, and regulatory audits.

The Angular Operator Portal is the primary frontend application that must follow strict architecture patterns to ensure:

- **Scalability** - Support growth from single hospital to enterprise multi-tenant deployments
- **Maintainability** - Enable long-term code maintenance and team collaboration
- **Consistency** - Ensure uniform patterns across all features and modules
- **Microservice Compatibility** - Integrate seamlessly with backend gateway APIs

These rules must be followed by:

- **Developers** - All team members writing Angular code
- **Code Generators** - Any tool that generates Angular code (CLI, scaffolding tools)
- **Cursor Agents** - AI assistants generating code for this project

**Violation of these rules is considered a breaking change and must be addressed before code review approval.**

---

## 2. Angular Architecture Model

### Standalone Architecture

The application uses **Angular Standalone Architecture** (no NgModules).

- All components, directives, and pipes are standalone
- No `@NgModule` declarations
- Imports are declared at the component/service level

### Folder Structure

```
src/app/
├── core/           # Infrastructure layer
├── shared/         # Reusable components and utilities
└── features/       # Domain modules
```

### Layer Responsibilities

**`core/`** - Infrastructure layer
- Singleton services
- Guards, interceptors, and core utilities
- Application-wide configuration
- Layout components (shell, header, sidebar)

**`shared/`** - Reusable components and utilities
- UI components used across multiple features
- Shared services and utilities
- Base classes and interfaces
- Common models and types

**`features/`** - Domain modules
- Business logic and domain-specific functionality
- Feature-specific components, services, and models
- Feature routing configuration
- Self-contained feature modules

---

## 3. Core Layer Rules

### Allowed Contents

The `core/` folder must only contain:

- `auth/` - Authentication services (AuthService, TokenService, SessionService)
- `guards/` - Route guards (authGuard, roleGuard, frameworkGuard)
- `interceptors/` - HTTP interceptors (authInterceptor, apiResponseInterceptor, loadingInterceptor)
- `services/` - Core singleton services (LoggerService, LoadingService, UserContextService, ComplianceContextService, AppContextService)
- `layout/` - Layout components (ShellComponent, HeaderComponent, SidebarComponent)
- `config/` - Application configuration

### Rules

1. **Core services must be singletons**
   - All services in `core/` must use `providedIn: 'root'`
   - No feature-specific services in `core/`

2. **Core must not import feature modules**
   - Core services cannot depend on feature-specific code
   - Core can only import from `shared/` or external packages

3. **Core must not contain business logic**
   - Core services handle infrastructure concerns only
   - Business logic belongs in feature services

4. **Core services are application-wide**
   - Available to all features via dependency injection
   - Must not have feature-specific dependencies

---

## 4. Feature Module Rules

### Structure

All domain functionality must live in the `features/` folder.

Each feature must follow this structure:

```
features/
└── [feature-name]/
    ├── pages/              # Route components
    ├── components/        # Feature-specific components
    ├── services/          # Feature business logic
    ├── models/            # Feature-specific types/interfaces
    └── routes.ts          # Feature routing configuration
```

**Example:**

```
features/journey/
├── pages/
│   └── journey.component.ts
├── components/
│   └── journey-card.component.ts
├── services/
│   └── journey.service.ts
├── models/
│   └── journey.model.ts
└── routes.ts
```

### Rules

1. **Components must not access HttpClient directly**
   - All HTTP calls must go through feature services
   - Components inject feature services, not HttpClient

2. **Feature services must handle business logic**
   - Business rules belong in feature services
   - Components should be thin and delegate to services

3. **Feature services must extend BaseApiService**
   - All feature services extend `BaseApiService` from `shared/services/`
   - API endpoints defined in feature services

4. **Features must be self-contained**
   - Features should not depend on other features
   - Shared functionality goes in `shared/`

5. **Feature routes must be lazy loaded**
   - Use `loadChildren` or `loadComponent` in routing
   - No eager loading of feature modules

---

## 5. API Communication Rules

### Communication Pattern

All API calls must follow this strict pattern:

```
Component → FeatureService → BaseApiService → HttpClient → Gateway API
```

### Rules

1. **Components MUST NEVER call HttpClient directly**
   - ❌ **FORBIDDEN:** `this.http.get(...)` in components
   - ✅ **REQUIRED:** `this.featureService.getData()` in components

2. **All feature services must extend BaseApiService**
   - Feature services inherit HTTP methods from `BaseApiService`
   - BaseApiService handles URL construction and HTTP configuration

3. **API endpoints must be defined in feature services**
   - Endpoints are private to feature services
   - Components do not know API endpoint URLs

4. **BaseApiService provides protected methods**
   - `get<T>(endpoint, params?)`
   - `post<T>(endpoint, body, headers?)`
   - `put<T>(endpoint, body, headers?)`
   - `patch<T>(endpoint, body, headers?)`
   - `delete<T>(endpoint, params?)`

### Example

**✅ CORRECT:**

```typescript
// journey.service.ts
@Injectable({ providedIn: 'root' })
export class JourneyService extends BaseApiService {
  getJourneys(): Observable<Journey[]> {
    return this.get<Journey[]>('/journey');
  }
}

// journey.component.ts
export class JourneyComponent {
  private journeyService = inject(JourneyService);
  
  ngOnInit() {
    this.journeyService.getJourneys().subscribe(...);
  }
}
```

**❌ FORBIDDEN:**

```typescript
// journey.component.ts
export class JourneyComponent {
  private http = inject(HttpClient); // ❌ FORBIDDEN
  
  ngOnInit() {
    this.http.get('/api/v1/journey').subscribe(...); // ❌ FORBIDDEN
  }
}
```

---

## 6. API Response Envelope

### Backend Response Format

All backend responses follow this envelope structure:

```typescript
{
  success: boolean;
  code: string;
  message: string;
  data: T;           // Actual response data
  errors: string[];
  meta?: any;
  traceId?: string;
  timestamp?: string;
}
```

### Rules

1. **Components must receive only the `data` object**
   - ApiResponseInterceptor extracts `data` from envelope
   - Components never see the envelope structure

2. **Errors must be handled centrally**
   - ApiResponseInterceptor handles error extraction
   - Components receive typed data or errors

3. **Components must not parse ApiResponse envelopes**
   - Interceptor handles envelope parsing
   - Components work with typed data only

4. **Unsuccessful responses throw HttpErrorResponse**
   - `success: false` responses are converted to errors
   - Error handling in components via RxJS operators

### Example

**Backend Response:**
```json
{
  "success": true,
  "code": "OK",
  "message": "Journeys retrieved successfully",
  "data": [
    { "id": "1", "name": "Journey 1" }
  ],
  "errors": []
}
```

**Component Receives:**
```typescript
// After ApiResponseInterceptor processing
[
  { "id": "1", "name": "Journey 1" }
]
```

---

## 7. Interceptor Rules

### Required Interceptors

The following interceptors must exist and be registered in `app.config.ts`:

1. **LoadingInterceptor**
   - Shows/hides loading indicators
   - Tracks concurrent requests
   - Injects LoadingService

2. **AuthInterceptor**
   - Adds Authorization header with JWT token
   - Adds X-Hospital-Id header from ComplianceContextService
   - Handles 401 errors and token refresh
   - Redirects to login on refresh failure

3. **ApiResponseInterceptor**
   - Extracts `data` from ApiResponse envelope
   - Converts unsuccessful responses to errors
   - Handles error transformation

### Interceptor Order

Interceptors must be registered in this order:

```typescript
provideHttpClient(
  withInterceptors([
    loadingInterceptor,      // First: Track loading state
    authInterceptor,        // Second: Add auth headers
    apiResponseInterceptor  // Third: Transform responses
  ])
)
```

### Rules

1. **All HTTP requests must pass through interceptors**
   - No bypassing interceptors
   - Interceptors are applied globally

2. **Components must not manually add Authorization headers**
   - AuthInterceptor handles all auth headers
   - Components never set Authorization directly

3. **Interceptors must be functional interceptors**
   - Use `HttpInterceptorFn` (not class-based)
   - Use `inject()` for dependency injection

4. **Error handling in interceptors**
   - Interceptors handle cross-cutting concerns
   - Components handle business-specific errors

---

## 8. Authentication Rules

### Authentication Services

Authentication must use these services:

- **AuthService** - Login, logout, token refresh
- **TokenService** - Token storage and validation
- **SessionService** - Session management and expiration

### Token Storage

1. **JWT tokens must be stored in localStorage**
   - Access token: `access_token`
   - Refresh token: `refresh_token`
   - Expiration: `token_expiration`

2. **TokenService manages token storage**
   - Components must not access localStorage directly
   - Use TokenService methods: `getAccessToken()`, `setAccessToken()`, etc.

3. **Token validation**
   - TokenService provides `isTokenExpired()` method
   - Tokens are validated before API calls

### Route Protection

1. **AuthGuard must protect all routes under `/app`**
   - AuthGuard checks token validity
   - Redirects to `/login` if not authenticated
   - Preserves return URL in query params

2. **RoleGuard protects role-based routes**
   - RoleGuard checks user roles from UserContextService
   - Redirects to dashboard if unauthorized

3. **FrameworkGuard protects compliance routes**
   - FrameworkGuard checks framework selection
   - Ensures compliance context is initialized

### Token Refresh

1. **Refresh token flow must be handled by AuthInterceptor**
   - 401 errors trigger automatic token refresh
   - Original request is retried with new token
   - User is redirected to login if refresh fails

2. **Refresh token is automatic**
   - Components do not handle token refresh
   - AuthInterceptor manages the entire flow

---

## 9. Context State Management

### State Management Approach

State management must use **RxJS BehaviorSubjects** (not NgRx).

### Context Services

The following context services must exist:

1. **UserContextService**
   - Manages current user state
   - User, organization, hospitals, roles
   - Observable streams: `user$`, `organization$`, `hospitals$`, `roles$`

2. **ComplianceContextService**
   - Manages compliance context
   - Hospital ID, framework ID
   - Observable streams: `hospitalId$`, `frameworkId$`

3. **AppContextService**
   - Orchestrates context initialization
   - Hospital switching logic
   - Framework loading

### Rules

1. **NgRx must NOT be introduced unless architecture review approves**
   - Current architecture uses RxJS BehaviorSubjects
   - NgRx requires explicit approval and migration plan

2. **Components must subscribe to context services**
   - Use async pipe or manual subscriptions
   - Components react to context changes

3. **Context services are singletons**
   - All context services use `providedIn: 'root'`
   - Single source of truth for application state

4. **State updates must go through context services**
   - Components call service methods to update state
   - Direct state manipulation is forbidden

### Example

```typescript
// Component usage
export class DashboardComponent {
  private userContext = inject(UserContextService);
  
  user$ = this.userContext.user$;
  
  ngOnInit() {
    this.user$.subscribe(user => {
      // React to user changes
    });
  }
}
```

---

## 10. Routing Architecture

### Top-Level Routes

```
/login              # Login page
/signup             # Signup page
/register           # Registration page
/forgot-password    # Password recovery
/reset-password     # Password reset
/app                # Protected application shell
```

### Application Routes (`/app`)

All protected routes are children of `/app`:

```
/app
├── /dashboard      # Dashboard (default redirect)
├── /journey        # Journey management
├── /tasks          # Task management
├── /evidence       # Evidence management
├── /audit          # Audit management
├── /reports        # Reporting
└── /ccc            # Compliance Control Center
```

### Compliance Control Center Routes (`/app/ccc`)

```
/app/ccc
├── /risk-escalation
├── /integration-system
├── /access-accountability
├── /regulatory-governance
├── /organizational-control
├── /monitoring-traceability
└── /compliance-infrastructure
```

### Rules

1. **All feature routes must be lazy loaded**
   - Use `loadChildren: () => import('./features/.../routes').then(m => m.routes)`
   - Use `loadComponent: () => import(...).then(m => m.Component)`

2. **Routing must use standalone route files**
   - Each feature has a `routes.ts` file
   - Routes are exported as `Routes` array

3. **Route guards must be functional guards**
   - Use `CanActivateFn` (not class-based guards)
   - Guards are defined in `core/guards/`

4. **Default redirects**
   - Root path (`''`) redirects to `/login`
   - `/app` redirects to `/app/dashboard`

### Example

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'app',
    loadComponent: () => import('./core/layout/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/pages/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'journey',
        loadChildren: () => import('./features/journey/routes').then(m => m.routes),
      },
    ],
  },
];
```

---

## 11. Shared Component Rules

### Location

Reusable UI components must live in:

```
shared/ui/
├── table/
├── dialog/
├── file-upload/
├── status-badge/
├── confirmation-dialog/
├── form-field/
└── progress-indicator/
```

### Rules

1. **Shared components must be UI-only**
   - No business logic in shared components
   - Components are presentational

2. **Shared components must not contain business logic**
   - Business logic belongs in feature services
   - Shared components accept data via `@Input()`

3. **Shared components must be standalone**
   - All shared components are standalone
   - Components declare their own imports

4. **Shared components must be reusable**
   - Components should work across multiple features
   - Feature-specific components belong in `features/`

5. **Shared components must use design tokens**
   - Colors, spacing, typography from design tokens
   - No hardcoded styles

### Example

```typescript
// shared/ui/status-badge/status-badge.component.ts
@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `...`,
})
export class StatusBadgeComponent {
  @Input() label = '';
  @Input() status: StatusType = 'neutral';
  // No business logic, only presentation
}
```

---

## 12. Design System

### Design Tokens Location

Global design tokens must exist in:

```
src/styles/design-tokens.scss
```

### Rules

1. **Colors must not be hardcoded in components**
   - Use SCSS variables from design tokens
   - Colors defined in `design-tokens.scss`

2. **All components must use design tokens**
   - Spacing, typography, colors from tokens
   - Consistent styling across application

3. **Design tokens are the single source of truth**
   - Changes to tokens affect entire application
   - No duplicate color/spacing definitions

4. **Components reference tokens via SCSS**
   - Import tokens in component styles
   - Use `@use` or `@import` for token access

### Example

```scss
// design-tokens.scss
$primary-color: #2563EB;
$spacing-unit: 0.5rem;

// component.scss
@use '../styles/design-tokens' as tokens;

.button {
  background-color: tokens.$primary-color;
  padding: tokens.$spacing-unit * 2;
}
```

---

## 13. Error Handling

### Global Error Pages

The following error pages must exist:

- `/error/403` - Forbidden (unauthorized access)
- `/error/404` - Not Found (page not found)
- `/error/500` - Internal Server Error
- `/error/maintenance` - Maintenance Mode

### Error Handling Strategy

1. **Errors must be handled centrally in interceptors**
   - ApiResponseInterceptor handles API errors
   - Global error handler catches unhandled errors

2. **Components handle business-specific errors**
   - Components catch errors from service calls
   - Display user-friendly error messages

3. **Error pages are route components**
   - Error pages are lazy loaded
   - Routes defined in `app.routes.ts`

4. **HTTP errors are transformed**
   - ApiResponseInterceptor transforms error responses
   - Components receive typed error objects

### Example

```typescript
// Component error handling
this.journeyService.getJourneys().pipe(
  catchError((error: HttpErrorResponse) => {
    this.logger.error('Failed to load journeys', error);
    // Display user-friendly message
    return of([]);
  })
).subscribe();
```

---

## 14. Logging Rules

### LoggerService

A `LoggerService` must exist in `core/services/logger.service.ts`.

### Rules

1. **Console logging must not be used directly**
   - ❌ **FORBIDDEN:** `console.log()`, `console.error()`, etc.
   - ✅ **REQUIRED:** `loggerService.info()`, `loggerService.error()`, etc.

2. **Use LoggerService methods**
   - `debug(message, ...args)` - Debug logging (only in development)
   - `info(message, ...args)` - Informational messages
   - `warn(message, ...args)` - Warning messages
   - `error(message, error?, ...args)` - Error messages

3. **LoggerService respects environment**
   - Debug logging only when `environment.enableDebugLogging === true`
   - Production logging goes to error tracking service (when implemented)

4. **Error logging includes context**
   - Pass error objects to `loggerService.error()`
   - Include relevant context in error messages

### Example

```typescript
// ❌ FORBIDDEN
console.log('User logged in');
console.error('API error', error);

// ✅ REQUIRED
private logger = inject(LoggerService);

this.logger.info('User logged in');
this.logger.error('API error', error);
```

---

## 15. Performance Rules

### Lazy Loading

1. **All major modules must be lazy loaded**
   - Features loaded on-demand
   - Reduces initial bundle size

2. **Route-based code splitting**
   - Each feature route is a separate chunk
   - Angular CLI handles code splitting automatically

### Data Loading

1. **Tables must support server-side pagination**
   - Large datasets paginated on server
   - Components request page data only

2. **Large lists must use virtual scrolling**
   - CDK Virtual Scrolling for long lists
   - Render only visible items

3. **Images must be lazy loaded**
   - Use `loading="lazy"` attribute
   - Optimize image sizes

### Change Detection

1. **Use OnPush change detection strategy**
   - Components use `ChangeDetectionStrategy.OnPush`
   - Reduces change detection cycles

2. **Use signals for reactive state**
   - Prefer Angular signals over observables where appropriate
   - Signals provide fine-grained reactivity

### Example

```typescript
@Component({
  selector: 'app-journey-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  // ...
})
export class JourneyListComponent {
  // Component implementation
}
```

---

## 16. Development Rules

### Component Design

1. **Components must be small and focused**
   - Single responsibility principle
   - Components should do one thing well

2. **Components should be presentational when possible**
   - Business logic in services
   - Components handle UI concerns

### Service Design

1. **Services must contain business logic**
   - Business rules belong in services
   - Services are testable units

2. **Services should be focused**
   - One service per domain concern
   - Avoid god services

### Code Quality

1. **Avoid code duplication**
   - Extract common logic to services
   - Use shared components for UI patterns

2. **Follow TypeScript strict typing**
   - Enable strict mode in `tsconfig.json`
   - Use explicit types, avoid `any`

3. **Use interfaces for data structures**
   - Define interfaces for API responses
   - Type all service methods

### Example

```typescript
// ✅ Good: Focused service
@Injectable({ providedIn: 'root' })
export class JourneyService extends BaseApiService {
  getJourneys(): Observable<Journey[]> {
    return this.get<Journey[]>('/journey');
  }
}

// ❌ Bad: God service
@Injectable({ providedIn: 'root' })
export class DataService {
  // Handles journeys, tasks, evidence, audits, etc.
}
```

---

## 17. Forbidden Practices

The following practices are **explicitly forbidden** and must be rejected in code review:

### ❌ Direct HttpClient Usage in Components

```typescript
// ❌ FORBIDDEN
export class JourneyComponent {
  private http = inject(HttpClient);
  
  getData() {
    this.http.get('/api/journey').subscribe(...);
  }
}
```

### ❌ Hardcoded API URLs

```typescript
// ❌ FORBIDDEN
this.http.get('https://api.example.com/v1/journey');

// ✅ REQUIRED
// Use environment.apiBaseUrl in BaseApiService
```

### ❌ Direct localStorage Usage Outside SessionService

```typescript
// ❌ FORBIDDEN
localStorage.setItem('token', token);

// ✅ REQUIRED
this.tokenService.setAccessToken(token);
```

### ❌ Mixing UI Frameworks

- Do not mix Angular Material with other UI libraries
- Use Angular Material components consistently
- Custom components must follow Material Design principles

### ❌ Introducing New State Libraries Without Approval

- NgRx, Akita, or other state management libraries require architecture review
- Current architecture uses RxJS BehaviorSubjects
- Any deviation requires explicit approval

### ❌ Bypassing Interceptors

- All HTTP calls must go through registered interceptors
- No direct HttpClient usage that bypasses interceptors

### ❌ Business Logic in Components

```typescript
// ❌ FORBIDDEN
export class JourneyComponent {
  processData(data: any) {
    // Complex business logic here
  }
}

// ✅ REQUIRED
export class JourneyService {
  processData(data: any) {
    // Business logic in service
  }
}
```

---

## 18. Cursor Code Generation Rules

This section applies specifically to **AI code generation tools** (Cursor, GitHub Copilot, etc.).

### When Generating Angular Code

1. **Follow the architecture rules in this document**
   - Read this document before generating code
   - Ensure generated code complies with all rules

2. **Never bypass:**
   - **Interceptors** - All HTTP calls must use feature services
   - **Feature services** - Components must not call HttpClient directly
   - **Context services** - Use UserContextService, ComplianceContextService for state

3. **Always:**
   - Extend `BaseApiService` for feature services
   - Use lazy loading for feature routes
   - Use `LoggerService` instead of console methods
   - Use design tokens for styling
   - Follow the folder structure (core/shared/features)

4. **If a generated change violates architecture rules:**
   - The change must be rejected
   - Code must be refactored to comply
   - Architecture violations are blocking issues

### Code Generation Checklist

Before generating code, verify:

- [ ] Component does not inject HttpClient
- [ ] Feature service extends BaseApiService
- [ ] Routes are lazy loaded
- [ ] LoggerService is used (not console)
- [ ] Design tokens are used (not hardcoded colors)
- [ ] Component uses OnPush change detection
- [ ] Context services are used for state (not direct state)
- [ ] Interceptors are not bypassed

### Example Generation Pattern

**✅ CORRECT Generation:**

```typescript
// Feature service
@Injectable({ providedIn: 'root' })
export class NewFeatureService extends BaseApiService {
  getData(): Observable<Data[]> {
    return this.get<Data[]>('/new-feature');
  }
}

// Component
@Component({
  selector: 'app-new-feature',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewFeatureComponent {
  private service = inject(NewFeatureService);
  private logger = inject(LoggerService);
  
  data$ = this.service.getData().pipe(
    catchError(error => {
      this.logger.error('Failed to load data', error);
      return of([]);
    })
  );
}
```

---

## Document Maintenance

### Updates

This document must be updated when:

- Architecture patterns change
- New rules are established
- Existing rules are modified or removed

### Approval Process

Changes to this document require:

1. Architecture review
2. Team consensus
3. Update to version number
4. Communication to all developers

### Questions

For questions about these rules:

1. Review this document first
2. Consult with architecture team
3. Propose changes through proper channels

---

**This document is the official Angular architecture policy for PRAMAN Operator Portal.**

**All code must comply with these rules.**

---

*Last Updated: 2025-01-20*  
*Version: 1.0*
