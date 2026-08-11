# Phase 18: Signup Wizard API Integration - Research

**Generated:** 2025-01-20  
**Phase:** 18-signup-wizard-api-integration

## Research Summary

The signup wizard UI is already implemented with a 3-step flow:
1. Organization Details
2. Hospital Details  
3. Admin User Details

The component currently uses template-driven forms and has mock submission logic. Integration with the backend onboarding API is required.

## Current State Analysis

### Existing Signup Component
- **Location:** `src/app/signup/signup.ts`
- **Form Type:** Template-driven (FormsModule)
- **Validation:** Basic validation with `canProceedStep*()` methods
- **Error Handling:** Uses `alert()` dialogs (needs upgrade to NotificationService)
- **Submission:** Mock `setTimeout()` - needs real API integration

### Architecture Patterns to Follow
- **BaseApiService:** All feature services must extend this (see `src/app/shared/services/base-api.service.ts`)
- **NotificationService:** Use for Material SnackBar messages (already created in Phase 17)
- **Reactive Forms:** Convert from template-driven to reactive forms (as done in login component)
- **State Management:** Use Angular Signals for wizard state (as per architecture rules)

## Backend API Requirements

### Endpoint
- **URL:** `POST /api/v1/onboarding/hospital`
- **Base URL:** From `environment.apiBaseUrl` (currently `https://localhost:7216/api/v1`)

### Request Payload Structure
```typescript
{
  organization: {
    name: string;
    type: string;  // e.g., "HospitalGroup"
    address: string;
  },
  hospital: {
    name: string;
    code: string;  // e.g., "VCORP-CITY" (uppercase, no spaces, max 20 chars)
    address: string;
    nabhLevel: string;  // e.g., "NABH-LEVEL-1"
  },
  adminUser: {
    email: string;
    password: string;
  }
}
```

### Expected Error Cases
- **Duplicate Email:** "Admin email already exists"
- **Duplicate Hospital Code:** "Hospital code already registered"
- **API Failure:** Generic error handling needed

## Implementation Strategy

### 1. Feature Module Structure
Create `src/app/features/onboarding/` with:
- `models/` - TypeScript interfaces
- `services/onboarding.service.ts` - API service extending BaseApiService
- `state/onboarding.store.ts` - Signal-based state management

### 2. Validation Requirements

**Step 1 - Organization:**
- Organization Name: Required, min 3 chars
- Organization Type: Required (dropdown selection)
- Address: Required, min 3 chars

**Step 2 - Hospital:**
- Hospital Name: Required, min 3 chars
- Hospital Code: Required, uppercase, no spaces, max 20 chars
- Address: Required, min 3 chars
- NABH Level: Required (dropdown selection)

**Step 3 - Admin User:**
- Email: Required, valid email format
- Password: Required, min 8 characters
- Confirm Password: Required, must match password

### 3. State Management Approach
- Use Angular Signals for wizard state
- Store form data in OnboardingStore
- Preserve data when navigating between steps
- Optional: Use sessionStorage for persistence on refresh

### 4. Success Flow
- Show success message via NotificationService
- Redirect to `/login` with optional email prefilling
- Clear wizard state

### 5. Error Handling
- Use NotificationService (Material SnackBar)
- Map backend errors to user-friendly messages
- Handle network errors gracefully
- Never log passwords

## Key Dependencies

- **BaseApiService:** Already exists, extends for OnboardingService
- **NotificationService:** Already created in Phase 17
- **ReactiveFormsModule:** Available, need to convert signup component
- **MatProgressSpinnerModule:** Available for loading states
- **Router:** Available for navigation after success

## Architecture Compliance

All implementation must follow:
- `.cursor/rules/angular_architecture.mdc`
- Component → Service → BaseApiService pattern
- No direct HttpClient usage in components
- Proper error handling via interceptors
- LoggerService for logging (no console.log)

## Research Complete

Ready to proceed with planning.
