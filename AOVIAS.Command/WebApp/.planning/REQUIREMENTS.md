# Requirements

## System Overview
PRAMAN Operator Portal is a Healthcare Compliance SaaS platform used by hospitals to manage accreditation frameworks like NABH.

## Technical Requirements

### Architecture
- Angular Standalone Architecture (no NgModules)
- Enterprise scalable design
- Feature-based folder structure
- Gateway API pattern (frontend ONLY interacts with Gateway API)

### API Response Format
All backend responses follow standard envelope:
```json
{
    "success": true,
    "code": "SUCCESS",
    "message": "Login successful",
    "data": {},
    "errors": [],
    "meta": {
        "requestId": null,
        "timestamp": "2026-03-14T13:21:39.2180767Z"
    },
    "error": null,
    "traceId": "",
    "timestamp": "2026-03-14T13:21:39.2177317Z"
}
```

### State Management
- Use RxJS BehaviorSubjects (NO NgRx)
- Context services for user, compliance, and app state

### Authentication
- JWT-based authentication
- Tokens stored in localStorage
- Refresh token flow
- 401 handling with automatic retry

### Routing
- Top-level routes: /login, /register, /forgot-password, /reset-password
- App routes under /app with ShellComponent
- Lazy loading for all feature modules
- Route guards: AuthGuard, FrameworkGuard, RoleGuard

### UI Framework
- Angular Material for UI components
- Global SCSS design tokens
- Reusable shared components

### Error Handling
- Global error handling strategy
- Error pages: 403, 404, 500, maintenance

### Performance
- Lazy loading enabled
- OnPush change detection
- Server pagination readiness

### Developer Experience
- ESLint configured
- Prettier configured
- Logging infrastructure with environment-based debug control

## Constraints
- Do NOT generate feature screens yet (infrastructure only)
- Keep code modular and clean
- Follow Angular standalone best practices
- All APIs must be typed
