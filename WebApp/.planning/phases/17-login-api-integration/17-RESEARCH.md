# Research: Phase 17 - Login API Integration

**Research Date:** 2025-01-20  
**Status:** RESEARCH COMPLETE

## Research Objective

Research how to integrate the existing login UI with the backend authentication API while following PRAMAN Angular architecture rules.

## Current State Analysis

### Existing Infrastructure

1. **Login Component** (`src/app/login/login.component.ts`)
   - UI is fully implemented with email/password form
   - Has password strength indicator
   - Has SSO buttons (Microsoft, Google) - not yet functional
   - Has biometric login button - not yet functional
   - Currently has mock `onSubmit()` that just sets loading state
   - Uses signals for reactive state management

2. **AuthService** (`src/app/core/auth/auth.service.ts`)
   - Exists but needs updates:
     - Uses `username` instead of `email` in LoginRequest
     - LoginResponse missing fields: userId, email, role, organizationId, hospitalId
     - API endpoint is `/auth/login` but should be `/identity/login`
     - Directly uses HttpClient (violates architecture - should extend BaseApiService)

3. **TokenService** (`src/app/core/auth/token.service.ts`)
   - Fully implemented
   - Handles access/refresh token storage in localStorage
   - Has JWT decoding and expiration checking

4. **SessionService** (`src/app/core/auth/session.service.ts`)
   - Fully implemented
   - Manages token expiration
   - Wraps TokenService

5. **AuthInterceptor** (`src/app/core/interceptors/auth.interceptor.ts`)
   - Exists but needs updates:
     - Should exclude `/identity/login` and `/identity/refresh` from auth header attachment
     - Refresh token endpoint should be `/identity/refresh` not `/auth/refresh`

6. **AuthGuard** (`src/app/core/guards/auth.guard.ts`)
   - Exists and functional
   - Checks token validity
   - Redirects to login if not authenticated

7. **ApiResponseInterceptor** (`src/app/core/interceptors/api-response.interceptor.ts`)
   - Exists and handles ApiResponse envelope unwrapping
   - Extracts `data` from response

8. **BaseApiService** (`src/app/shared/services/base-api.service.ts`)
   - Exists and provides HTTP methods
   - All feature services should extend this

### Backend API Specification

**Endpoint:** `POST {{baseUrl}}/api/v1/identity/login`

**Request:**
```json
{
  "email": "admin+acme@example.com",
  "password": "P@ssw0rd!123"
}
```

**Response (ApiResponse envelope):**
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Login successful",
  "data": {
    "token": "eyJhbGc...",
    "refreshToken": "ZDRjMjY...",
    "expiresIn": 3600,
    "userId": "7058c611-53fa-4154-ba5d-581bd237c9f2",
    "email": "admin+acme@example.com",
    "role": "GroupAdmin",
    "organizationId": "107c90c7-f449-48c8-a08a-217b76b90281",
    "hospitalId": "b5c47200-546c-4fe0-8b81-ac498f9193ac"
  },
  "errors": [],
  "meta": {...},
  "error": null,
  "traceId": "",
  "timestamp": "..."
}
```

**Note:** Backend returns `token` (not `accessToken`) and `refreshToken`.

## Architecture Compliance Requirements

### Critical Rules to Follow

1. **API Communication Pattern:**
   - Component → FeatureService → BaseApiService → HttpClient → Gateway API
   - AuthService should extend BaseApiService (currently violates this)

2. **Response Handling:**
   - ApiResponseInterceptor unwraps envelope
   - Components receive only `data` object
   - AuthService should work with unwrapped data

3. **Token Storage:**
   - Use SessionService (not direct localStorage)
   - Store: accessToken, refreshToken, organizationId, hospitalId, tokenExpiry

4. **Context State:**
   - Update UserContextService with user data after login
   - Update ComplianceContextService with organizationId and hospitalId

5. **Error Handling:**
   - Use Material SnackBar for error messages
   - Handle: Invalid Credentials, Server Error, Network Error

6. **Logging:**
   - Use LoggerService (not console.log)
   - Debug logging only in development

## Implementation Gaps

### 1. AuthService Updates Needed

- [ ] Change LoginRequest from `username` to `email`
- [ ] Update LoginResponse interface to match API response
- [ ] Change AuthService to extend BaseApiService
- [ ] Update API endpoint from `/auth/login` to `/identity/login`
- [ ] Update refresh endpoint from `/auth/refresh` to `/identity/refresh`
- [ ] Handle response field name: `token` → `accessToken` mapping

### 2. Login Component Integration

- [ ] Inject AuthService into LoginComponent
- [ ] Call AuthService.login() in onSubmit()
- [ ] Handle loading state
- [ ] Handle success: redirect to /app/dashboard
- [ ] Handle errors: show Material SnackBar messages
- [ ] Update UserContextService after successful login
- [ ] Update ComplianceContextService after successful login

### 3. AuthInterceptor Updates

- [ ] Exclude `/identity/login` from Authorization header
- [ ] Exclude `/identity/refresh` from Authorization header
- [ ] Update refresh token endpoint to `/identity/refresh`

### 4. Token Storage Enhancement

- [ ] Store organizationId in SessionService
- [ ] Store hospitalId in SessionService
- [ ] Store userId in SessionService (or UserContextService)

### 5. Error Handling

- [ ] Create error message mapping:
  - 401 → "Invalid email or password"
  - 500 → "Unable to login. Please try again."
  - Network errors → "Network error. Check connection."

### 6. Material UI Integration

- [ ] Import MatSnackBarModule in LoginComponent
- [ ] Inject MatSnackBar for error messages
- [ ] Create user-friendly error messages

## Security Considerations

1. **Token Storage:**
   - Access token in localStorage (current approach)
   - Refresh token in localStorage (current approach)
   - Consider sessionStorage for access token (more secure)

2. **Token Expiry:**
   - Backend returns `expiresIn: 3600` (seconds)
   - Calculate expiration: `Date.now() + expiresIn * 1000`

3. **Session Restoration:**
   - On page refresh, restore session from storage
   - Check token expiration before restoring

4. **Prevent Infinite Retry:**
   - If refresh token fails once, logout immediately
   - Don't retry refresh token multiple times

## State Management Flow

### After Successful Login

1. AuthService receives LoginResponse data
2. SessionService.saveTokens() stores tokens
3. SessionService stores organizationId, hospitalId
4. UserContextService.setUser() updates user state
5. ComplianceContextService.setOrganizationId() and setHospitalId()
6. Router navigates to /app/dashboard

## Testing Considerations

1. **Happy Path:**
   - Valid credentials → success → redirect to dashboard

2. **Error Cases:**
   - Invalid credentials → show error message
   - Server error → show error message
   - Network error → show error message

3. **Token Refresh:**
   - Expired token → auto-refresh → retry request
   - Refresh fails → logout → redirect to login

## Dependencies

- Angular Material (MatSnackBar)
- Router (for navigation)
- LoggerService (for logging)
- UserContextService (for user state)
- ComplianceContextService (for compliance context)

## Architecture Compliance Checklist

- [x] Follow Component → Service → BaseApiService pattern
- [x] Use ApiResponseInterceptor for envelope unwrapping
- [x] Use SessionService for token storage
- [x] Use LoggerService for logging
- [x] Use Material components for UI
- [x] Update context services after login
- [x] Handle errors gracefully
- [x] Follow Angular Standalone Architecture

## Research Complete

All necessary information gathered. Ready to create execution plan.
