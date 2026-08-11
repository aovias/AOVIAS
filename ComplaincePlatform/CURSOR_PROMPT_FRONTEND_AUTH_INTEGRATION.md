# Cursor Prompt: Frontend Authentication Integration

Copy this entire prompt into a new Cursor instance working on your Angular frontend project.

---

## Task: Implement Frontend Authentication Integration with Standardized Backend API

### Context
The backend authentication API has been refactored to return standardized JSON responses with:
- Success responses: `{ success: true, status: "success", data: {...}, message: "...", timestamp: "..." }`
- Error responses: `{ success: false, status: "error", errorCode: "...", message: "...", errors: [...], timestamp: "..." }`
- Comprehensive error codes (AUTH_INVALID_CREDENTIALS, AUTH_RATE_LIMIT_EXCEEDED, etc.)

### Backend API Endpoints

**Base URL:** `https://localhost:7135/api/v1/auth`

1. **POST /login**
   - Request: `{ email: string, password: string }`
   - Success Response: `{ success: true, data: { token, refreshToken, expiresIn, userId, email, role, organizationId } }`
   - Error Codes: `AUTH_MISSING_CREDENTIALS`, `AUTH_INVALID_CREDENTIALS`, `AUTH_ACCOUNT_LOCKED`, `AUTH_RATE_LIMIT_EXCEEDED`

2. **POST /register**
   - Request: `{ email: string, password: string, role?: string }`
   - Success Response: `{ success: true, data: { userId, email, message } }`
   - Error Codes: `AUTH_EMAIL_ALREADY_REGISTERED`, `AUTH_WEAK_PASSWORD`, `AUTH_VALIDATION_ERROR`

3. **POST /refresh**
   - Request: `{ refreshToken: string }`
   - Success Response: `{ success: true, data: { token, expiresIn } }`
   - Error Codes: `AUTH_INVALID_REFRESH_TOKEN`, `AUTH_USER_NOT_FOUND`

4. **POST /logout**
   - Request: `{ refreshToken?: string }` (optional)
   - Success Response: `{ success: true, data: { message } }`

### Error Codes Reference

| Error Code | HTTP Status | User-Friendly Message |
|------------|-------------|----------------------|
| `AUTH_MISSING_CREDENTIALS` | 400 | "Please enter both email and password." |
| `AUTH_INVALID_EMAIL_FORMAT` | 400 | "Please enter a valid email address." |
| `AUTH_INVALID_CREDENTIALS` | 401 | "Invalid email or password. Please try again." |
| `AUTH_USER_NOT_FOUND` | 401 | "No account found with this email address." |
| `AUTH_WEAK_PASSWORD` | 400 | "Password does not meet security requirements. Please use a stronger password." |
| `AUTH_EMAIL_ALREADY_REGISTERED` | 409 | "An account with this email already exists. Please log in instead." |
| `AUTH_RATE_LIMIT_EXCEEDED` | 429 | "Too many attempts. Please wait a moment before trying again." |
| `AUTH_ACCOUNT_LOCKED` | 403 | "Your account has been temporarily locked. Please try again later." |
| `AUTH_INVALID_TOKEN` | 401 | "Your session has expired. Please log in again." |
| `AUTH_INVALID_REFRESH_TOKEN` | 401 | "Your session has expired. Please log in again." |
| `AUTH_INTERNAL_SERVER_ERROR` | 500 | "A server error occurred. Please try again later." |

### Requirements

1. **Create TypeScript Interfaces**
   - Create `src/app/models/auth.models.ts` with interfaces matching backend responses:
     - `ApiResponse<T>`, `ApiErrorResponse`
     - `LoginRequest`, `LoginResponse`
     - `RegisterRequest`, `RegisterResponse`
     - `RefreshTokenRequest`, `RefreshTokenResponse`
     - `LogoutRequest`, `LogoutResponse`

2. **Create Authentication Service**
   - Create `src/app/services/auth.service.ts` with:
     - `login(email, password): Observable<ApiResponse<LoginResponse>>`
     - `register(email, password, role?): Observable<ApiResponse<RegisterResponse>>`
     - `refreshToken(): Observable<ApiResponse<RefreshTokenResponse>>`
     - `logout(): Observable<ApiResponse<any>>`
     - `getCurrentUser()`, `isAuthenticated()`, `getAccessToken()`
     - Error handling that converts error codes to user-friendly messages
     - Token storage in localStorage
     - RxJS error handling to prevent UI freezes

3. **Create Login Component**
   - Create `src/app/components/auth/login/login.component.ts` with:
     - Reactive form with email and password validation
     - Loading state (`isLoading` flag)
     - Error message display
     - Success message display
     - Form validation feedback
     - Submit handler that calls `authService.login()`
     - Navigation to dashboard on success
   - Create `src/app/components/auth/login/login.component.html` with:
     - Form with email and password inputs
     - Loading spinner during API call
     - Error message display area
     - Success message display area
     - Disabled submit button during loading
     - Form validation error messages

4. **Create Register Component** (similar to login)
   - Form with email, password, confirm password
   - Same error handling and loading states

5. **Create HTTP Interceptor**
   - Create `src/app/interceptors/auth.interceptor.ts` that:
     - Adds `Authorization: Bearer <token>` header to requests
     - Handles 401 responses by attempting token refresh
     - Redirects to login on refresh failure
     - Prevents infinite refresh loops

6. **Update App Module/Standalone Components**
   - Register `HttpClientModule` or `provideHttpClient()`
   - Register the auth interceptor
   - Configure routes for login/register components

7. **Error Handling Best Practices**
   - Always check `response.success` before accessing `data`
   - Use error codes for conditional logic
   - Show user-friendly messages (never expose technical errors)
   - Implement loading states to prevent UI freezes
   - Handle network errors gracefully
   - Show appropriate messages for rate limiting

8. **Token Management**
   - Store tokens in localStorage (or httpOnly cookies for production)
   - Implement automatic token refresh before expiration
   - Clear tokens on logout
   - Handle token expiration gracefully

### Implementation Guidelines

1. **Prevent UI Freezes:**
   - Always use `isLoading` flags
   - Use async/await or RxJS operators properly
   - Never block the UI thread
   - Show loading indicators during API calls

2. **Error Handling:**
   ```typescript
   this.authService.login(email, password).subscribe({
     next: (response) => {
       if (response.success && response.data) {
         // Handle success
       } else {
         // Handle failure
       }
     },
     error: (error) => {
       // Error is already formatted with user-friendly message
       this.errorMessage = error.message;
       this.isLoading = false;
     }
   });
   ```

3. **User-Friendly Error Messages:**
   Create a mapping function:
   ```typescript
   private getUserFriendlyMessage(errorCode: string, defaultMessage: string): string {
     const errorMessages: { [key: string]: string } = {
       'AUTH_INVALID_CREDENTIALS': 'Invalid email or password. Please try again.',
       'AUTH_RATE_LIMIT_EXCEEDED': 'Too many attempts. Please wait a moment before trying again.',
       // ... add all error codes
     };
     return errorMessages[errorCode] || defaultMessage || 'An error occurred. Please try again.';
   }
   ```

4. **Loading States:**
   ```typescript
   onSubmit(): void {
     this.isLoading = true;
     this.errorMessage = null;
     
     this.authService.login(email, password).subscribe({
       next: (response) => {
         this.isLoading = false;
         // Handle response
       },
       error: (error) => {
         this.isLoading = false;
         // Handle error
       }
     });
   }
   ```

### Testing Checklist

- [ ] Login with valid credentials shows success message and navigates
- [ ] Login with invalid credentials shows user-friendly error
- [ ] Login with missing fields shows validation errors
- [ ] Rate limiting shows appropriate message
- [ ] Loading states prevent multiple submissions
- [ ] Token is stored in localStorage after login
- [ ] Token refresh works automatically
- [ ] Logout clears tokens and redirects
- [ ] Network errors are handled gracefully
- [ ] UI never freezes during API calls

### Files to Create

1. `src/app/models/auth.models.ts` - TypeScript interfaces
2. `src/app/services/auth.service.ts` - Authentication service
3. `src/app/components/auth/login/login.component.ts` - Login component
4. `src/app/components/auth/login/login.component.html` - Login template
5. `src/app/components/auth/register/register.component.ts` - Register component
6. `src/app/components/auth/register/register.component.html` - Register template
7. `src/app/interceptors/auth.interceptor.ts` - HTTP interceptor
8. Update `app.config.ts` or `app.module.ts` for providers

### Additional Notes

- Use Angular Reactive Forms for form validation
- Implement proper TypeScript typing throughout
- Use RxJS operators (`catchError`, `tap`, `finalize`) for proper error handling
- Ensure all error scenarios are handled end-to-end
- Test with actual backend API (ensure CORS is configured)
- Consider implementing a global error handler for unhandled errors
- Add retry logic for network failures (optional)
- Implement token expiration checking (optional)

---

**Start implementing these changes step by step, ensuring each component handles errors gracefully and prevents UI freezes.**
