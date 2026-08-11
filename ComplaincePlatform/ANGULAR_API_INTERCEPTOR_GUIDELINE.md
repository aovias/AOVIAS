# Angular API Interceptor Guideline

This document provides guidelines for implementing Angular HTTP interceptors to work with the standardized `ApiResponse<T>` format used by all microservices.

## Table of Contents

1. [Response Format Overview](#response-format-overview)
2. [Interceptor Implementation](#interceptor-implementation)
3. [Error Handling](#error-handling)
4. [Correlation ID Handling](#correlation-id-handling)
5. [TypeScript Interfaces](#typescript-interfaces)
6. [Usage Examples](#usage-examples)
7. [Best Practices](#best-practices)

---

## Response Format Overview

All API responses follow this standardized format:

### Success Response (200 OK, 201 Created)

```json
{
  "success": true,
  "data": {
    // Your actual response data here
  },
  "error": null,
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### Error Response (400, 401, 403, 404, 409, 422, 500)

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "SNAPSHOT_NOT_FOUND",
    "message": "No snapshot found for hospital {hospitalId}",
    "status": 404,
    "details": null,
    "validationErrors": null
  },
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### Validation Error Response (400 BadRequest)

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "status": 400,
    "details": null,
    "validationErrors": {
      "hospitalId": [
        "HospitalId is required",
        "HospitalId cannot be empty"
      ],
      "email": [
        "Email format is invalid"
      ]
    }
  },
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

---

## Interceptor Implementation

### TypeScript Interfaces

First, define the TypeScript interfaces matching the API response structure:

```typescript
// api-response.interface.ts

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: ApiError | null;
  traceId: string;
  timestamp: string;
}

export interface ApiError {
  code: string;
  message: string;
  status: number;
  details?: string | null;
  validationErrors?: { [key: string]: string[] } | null;
}

export interface ValidationError {
  field: string;
  messages: string[];
}
```

### HTTP Interceptor

Create an interceptor that:
1. Extracts data from `ApiResponse<T>` wrapper
2. Handles errors consistently
3. Forwards correlation IDs
4. Provides user-friendly error messages

```typescript
// api-response.interceptor.ts

import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
  HttpResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, ApiError } from './api-response.interface';

@Injectable()
export class ApiResponseInterceptor implements HttpInterceptor {
  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // Add correlation ID to request headers if available
    const correlationId = this.getOrGenerateCorrelationId();
    const modifiedRequest = request.clone({
      setHeaders: {
        'X-Correlation-ID': correlationId
      }
    });

    return next.handle(modifiedRequest).pipe(
      // Transform successful responses
      map((event: HttpEvent<any>) => {
        if (event instanceof HttpResponse) {
          return this.handleSuccessResponse(event);
        }
        return event;
      }),
      // Handle errors
      catchError((error: HttpErrorResponse) => {
        return this.handleErrorResponse(error);
      })
    );
  }

  /**
   * Handles successful HTTP responses.
   * Extracts data from ApiResponse<T> wrapper.
   */
  private handleSuccessResponse(response: HttpResponse<any>): HttpResponse<any> {
    const body = response.body;

    // Check if response follows ApiResponse<T> format
    if (this.isApiResponse(body)) {
      const apiResponse = body as ApiResponse<any>;

      // Log correlation ID for debugging
      if (apiResponse.traceId) {
        console.debug(`Request traceId: ${apiResponse.traceId}`);
      }

      // For 204 No Content, return as-is (no body)
      if (response.status === 204) {
        return response;
      }

      // Extract and return the data
      // If success is false, treat as error (shouldn't happen with 2xx status)
      if (!apiResponse.success) {
        console.warn('Received 2xx status but success=false', apiResponse);
      }

      // Return new HttpResponse with unwrapped data
      return response.clone({
        body: apiResponse.data
      });
    }

    // If response doesn't follow ApiResponse format, return as-is
    // (for backward compatibility during migration)
    return response;
  }

  /**
   * Handles error HTTP responses.
   * Extracts error information from ApiResponse<ApiError> format.
   */
  private handleErrorResponse(error: HttpErrorResponse): Observable<never> {
    let apiError: ApiError | null = null;

    // Try to extract error from ApiResponse format
    if (error.error && this.isApiResponse(error.error)) {
      const apiResponse = error.error as ApiResponse<any>;
      apiError = apiResponse.error;
      
      // Log correlation ID for debugging
      if (apiResponse.traceId) {
        console.error(`Error traceId: ${apiResponse.traceId}`, apiError);
      }
    } else if (error.error && this.isApiError(error.error)) {
      // Direct ApiError (shouldn't happen, but handle gracefully)
      apiError = error.error as ApiError;
    }

    // Create user-friendly error message
    const userMessage = this.getUserFriendlyErrorMessage(apiError, error);

    // Create enhanced error with additional context
    const enhancedError = {
      ...error,
      apiError: apiError,
      userMessage: userMessage,
      validationErrors: apiError?.validationErrors || null
    };

    return throwError(() => enhancedError);
  }

  /**
   * Checks if response body follows ApiResponse<T> format.
   */
  private isApiResponse(body: any): boolean {
    return (
      body &&
      typeof body === 'object' &&
      typeof body.success === 'boolean' &&
      ('data' in body || body.data === null) &&
      ('error' in body || body.error === null) &&
      typeof body.traceId === 'string'
    );
  }

  /**
   * Checks if object is ApiError.
   */
  private isApiError(obj: any): boolean {
    return (
      obj &&
      typeof obj === 'object' &&
      typeof obj.code === 'string' &&
      typeof obj.message === 'string' &&
      typeof obj.status === 'number'
    );
  }

  /**
   * Generates user-friendly error message from ApiError.
   */
  private getUserFriendlyErrorMessage(
    apiError: ApiError | null,
    httpError: HttpErrorResponse
  ): string {
    if (apiError) {
      // Use error message from API
      return apiError.message;
    }

    // Fallback to HTTP error message
    if (httpError.error && typeof httpError.error === 'string') {
      return httpError.error;
    }

    // Generic error messages based on status code
    switch (httpError.status) {
      case 0:
        return 'Unable to connect to server. Please check your internet connection.';
      case 400:
        return 'Invalid request. Please check your input and try again.';
      case 401:
        return 'Authentication required. Please log in.';
      case 403:
        return 'You do not have permission to perform this action.';
      case 404:
        return 'The requested resource was not found.';
      case 409:
        return 'A conflict occurred. The resource may have been modified.';
      case 422:
        return 'The request could not be processed. Please check your input.';
      case 500:
        return 'An unexpected server error occurred. Please try again later.';
      case 502:
      case 503:
        return 'Service is temporarily unavailable. Please try again later.';
      default:
        return 'An error occurred. Please try again.';
    }
  }

  /**
   * Gets or generates correlation ID for request tracking.
   */
  private getOrGenerateCorrelationId(): string {
    // Try to get from session storage (persists across page reloads)
    let correlationId = sessionStorage.getItem('correlationId');
    
    if (!correlationId) {
      // Generate new correlation ID
      correlationId = this.generateCorrelationId();
      sessionStorage.setItem('correlationId', correlationId);
    }
    
    return correlationId;
  }

  /**
   * Generates a unique correlation ID.
   */
  private generateCorrelationId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}
```

---

## Error Handling

### Error Service

Create a service to handle errors consistently across the application:

```typescript
// error-handler.service.ts

import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, ValidationError } from './api-response.interface';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  /**
   * Extracts validation errors from API error response.
   */
  getValidationErrors(error: any): ValidationError[] {
    const apiError = this.getApiError(error);
    
    if (apiError?.validationErrors) {
      return Object.entries(apiError.validationErrors).map(([field, messages]) => ({
        field,
        messages: Array.isArray(messages) ? messages : [messages]
      }));
    }
    
    return [];
  }

  /**
   * Gets user-friendly error message.
   */
  getUserMessage(error: any): string {
    if (error?.userMessage) {
      return error.userMessage;
    }

    const apiError = this.getApiError(error);
    if (apiError?.message) {
      return apiError.message;
    }

    if (error instanceof HttpErrorResponse) {
      return error.message || 'An error occurred';
    }

    return 'An unexpected error occurred';
  }

  /**
   * Gets error code for programmatic handling.
   */
  getErrorCode(error: any): string | null {
    const apiError = this.getApiError(error);
    return apiError?.code || null;
  }

  /**
   * Checks if error is a validation error.
   */
  isValidationError(error: any): boolean {
    const apiError = this.getApiError(error);
    return apiError?.code === 'VALIDATION_ERROR' || 
           (apiError?.validationErrors !== null && apiError?.validationErrors !== undefined);
  }

  /**
   * Checks if error is a specific error code.
   */
  isErrorCode(error: any, code: string): boolean {
    return this.getErrorCode(error) === code;
  }

  /**
   * Extracts ApiError from error object.
   */
  private getApiError(error: any): ApiError | null {
    if (error?.apiError) {
      return error.apiError;
    }
    
    if (error?.error && typeof error.error === 'object' && error.error.code) {
      return error.error;
    }
    
    return null;
  }
}
```

### Global Error Handler

```typescript
// global-error-handler.ts

import { ErrorHandler, Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandlerService } from './error-handler.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  constructor(private errorHandlerService: ErrorHandlerService) {}

  handleError(error: Error | HttpErrorResponse): void {
    if (error instanceof HttpErrorResponse) {
      // HTTP error - already handled by interceptor
      console.error('HTTP Error:', error);
      
      // Show user-friendly message
      const userMessage = this.errorHandlerService.getUserMessage(error);
      // Use your notification service here
      // this.notificationService.showError(userMessage);
    } else {
      // Application error
      console.error('Application Error:', error);
      // this.notificationService.showError('An unexpected error occurred');
    }
  }
}
```

---

## Correlation ID Handling

### Request Tracking Service

```typescript
// request-tracking.service.ts

import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable()
export class RequestTrackingService {
  /**
   * Logs request with correlation ID for debugging.
   */
  logRequest(request: HttpRequest<any>, correlationId: string): void {
    console.debug(`[${correlationId}] ${request.method} ${request.url}`);
  }

  /**
   * Logs response with correlation ID.
   */
  logResponse(correlationId: string, status: number): void {
    console.debug(`[${correlationId}] Response: ${status}`);
  }

  /**
   * Logs error with correlation ID.
   */
  logError(correlationId: string, error: any): void {
    console.error(`[${correlationId}] Error:`, error);
  }
}
```

---

## Usage Examples

### Service Example

```typescript
// snapshot.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ProgramOverviewSnapshotDto } from './models/program-overview-snapshot.dto';
import { ErrorHandlerService } from './error-handler.service';

@Injectable({
  providedIn: 'root'
})
export class SnapshotService {
  private apiUrl = '/api/v1/dashboard';

  constructor(
    private http: HttpClient,
    private errorHandler: ErrorHandlerService
  ) {}

  /**
   * Gets program overview snapshot for a hospital.
   * The interceptor automatically unwraps ApiResponse<T> to return just the data.
   */
  getProgramOverviewSnapshot(hospitalId: string): Observable<ProgramOverviewSnapshotDto> {
    return this.http.get<ProgramOverviewSnapshotDto>(
      `${this.apiUrl}/program-overview-snapshot`,
      { params: { hospitalId } }
    ).pipe(
      catchError((error) => {
        // Error is already enhanced by interceptor
        if (this.errorHandler.isErrorCode(error, 'SNAPSHOT_NOT_FOUND')) {
          // Handle specific error code
          console.log('Snapshot not found for hospital:', hospitalId);
        }
        
        return throwError(() => error);
      })
    );
  }
}
```

### Component Example

```typescript
// dashboard.component.ts

import { Component, OnInit } from '@angular/core';
import { SnapshotService } from './snapshot.service';
import { ProgramOverviewSnapshotDto } from './models/program-overview-snapshot.dto';
import { ErrorHandlerService } from './error-handler.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  snapshot: ProgramOverviewSnapshotDto | null = null;
  loading = false;
  error: string | null = null;
  validationErrors: { field: string; messages: string[] }[] = [];

  constructor(
    private snapshotService: SnapshotService,
    private errorHandler: ErrorHandlerService
  ) {}

  ngOnInit(): void {
    this.loadSnapshot('e198747b-096d-4c19-a341-16be566c8da4');
  }

  loadSnapshot(hospitalId: string): void {
    this.loading = true;
    this.error = null;
    this.validationErrors = [];

    this.snapshotService.getProgramOverviewSnapshot(hospitalId).subscribe({
      next: (data) => {
        // Data is already unwrapped by interceptor
        this.snapshot = data;
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
        
        // Get user-friendly error message
        this.error = this.errorHandler.getUserMessage(error);
        
        // Extract validation errors if present
        if (this.errorHandler.isValidationError(error)) {
          this.validationErrors = this.errorHandler.getValidationErrors(error);
        }
        
        // Handle specific error codes
        const errorCode = this.errorHandler.getErrorCode(error);
        switch (errorCode) {
          case 'SNAPSHOT_NOT_FOUND':
            // Show friendly message or redirect
            console.log('No snapshot available');
            break;
          case 'UNAUTHORIZED':
            // Redirect to login
            // this.router.navigate(['/login']);
            break;
          case 'FORBIDDEN':
            // Show access denied message
            break;
        }
      }
    });
  }
}
```

### Template Example

```html
<!-- dashboard.component.html -->

<div *ngIf="loading">Loading...</div>

<div *ngIf="error" class="error-message">
  <p>{{ error }}</p>
  
  <!-- Show validation errors if present -->
  <div *ngIf="validationErrors.length > 0" class="validation-errors">
    <h4>Please fix the following errors:</h4>
    <ul>
      <li *ngFor="let validationError of validationErrors">
        <strong>{{ validationError.field }}:</strong>
        <span *ngFor="let message of validationError.messages">{{ message }}</span>
      </li>
    </ul>
  </div>
</div>

<div *ngIf="snapshot && !error">
  <h2>Program Overview</h2>
  <p>Overall Score: {{ snapshot.overallScore }}%</p>
  <p>Audit Confidence: {{ snapshot.auditConfidenceScore }}%</p>
  <!-- ... other fields ... -->
</div>
```

---

## Best Practices

### 1. Always Use the Interceptor

✅ **DO:**
- Let the interceptor handle response unwrapping
- Use the enhanced error object from interceptor
- Check error codes for specific handling

❌ **DON'T:**
- Manually unwrap `ApiResponse<T>` in services
- Access `response.body.data` directly
- Ignore correlation IDs

### 2. Error Handling

✅ **DO:**
```typescript
// Use error handler service
this.errorHandler.getUserMessage(error);
this.errorHandler.getValidationErrors(error);
this.errorHandler.isErrorCode(error, 'SNAPSHOT_NOT_FOUND');
```

❌ **DON'T:**
```typescript
// Don't access error properties directly
error.error.message; // May not exist
error.error.error.message; // Wrong structure
```

### 3. Type Safety

✅ **DO:**
```typescript
// Define interfaces matching API DTOs
interface ProgramOverviewSnapshotDto {
  overallScore: number;
  auditConfidenceScore: number;
  // ...
}

// Use in service
getSnapshot(): Observable<ProgramOverviewSnapshotDto> {
  return this.http.get<ProgramOverviewSnapshotDto>('/api/...');
}
```

### 4. Correlation ID Usage

✅ **DO:**
- Include correlation ID in error reports to support
- Log correlation ID for debugging
- Store correlation ID in session storage

❌ **DON'T:**
- Generate new correlation ID for every request
- Ignore correlation ID from API responses

### 5. Status Code Handling

| Status Code | Interceptor Behavior | Component Action |
|-------------|---------------------|------------------|
| 200 OK | Unwraps `data` | Use data directly |
| 201 Created | Unwraps `data` | Use data, show success message |
| 204 No Content | Returns as-is | Show success message |
| 400 BadRequest | Extracts `error` | Show validation errors |
| 401 Unauthorized | Extracts `error` | Redirect to login |
| 403 Forbidden | Extracts `error` | Show access denied |
| 404 NotFound | Extracts `error` | Show not found message |
| 409 Conflict | Extracts `error` | Show conflict message |
| 422 UnprocessableEntity | Extracts `error` | Show semantic validation errors |
| 500 InternalServerError | Extracts `error` | Show generic error message |

### 6. Validation Error Display

```typescript
// Component method to format validation errors
formatValidationErrors(errors: ValidationError[]): string {
  return errors
    .map(e => `${e.field}: ${e.messages.join(', ')}`)
    .join('\n');
}
```

### 7. Error Notification

```typescript
// Use a notification service
showError(error: any): void {
  const message = this.errorHandler.getUserMessage(error);
  const code = this.errorHandler.getErrorCode(error);
  
  // Show toast/notification
  this.notificationService.showError(message, {
    duration: code === 'VALIDATION_ERROR' ? 5000 : 3000,
    correlationId: error.traceId // Include for support
  });
}
```

---

## Migration Notes

### During API Migration

If some endpoints haven't been migrated yet:

1. **Check response format in interceptor:**
   ```typescript
   private isApiResponse(body: any): boolean {
     // Returns false for old format, true for new format
   }
   ```

2. **Handle both formats:**
   ```typescript
   if (this.isApiResponse(body)) {
     // New format - unwrap
     return response.clone({ body: apiResponse.data });
   } else {
     // Old format - return as-is
     return response;
   }
   ```

3. **Gradually migrate:**
   - Start with new endpoints
   - Update services as endpoints migrate
   - Remove old format handling once all migrated

---

## Testing

### Unit Test Example

```typescript
// api-response.interceptor.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { ApiResponseInterceptor } from './api-response.interceptor';

describe('ApiResponseInterceptor', () => {
  let interceptor: ApiResponseInterceptor;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiResponseInterceptor]
    });
    interceptor = TestBed.inject(ApiResponseInterceptor);
  });

  it('should unwrap ApiResponse<T> on success', (done) => {
    const apiResponse: ApiResponse<any> = {
      success: true,
      data: { id: 1, name: 'Test' },
      error: null,
      traceId: 'test-123',
      timestamp: new Date().toISOString()
    };

    const response = new HttpResponse({
      body: apiResponse,
      status: 200
    });

    // Test interceptor logic
    // ...
  });

  it('should extract error from ApiResponse on failure', (done) => {
    const apiResponse: ApiResponse<any> = {
      success: false,
      data: null,
      error: {
        code: 'NOT_FOUND',
        message: 'Resource not found',
        status: 404
      },
      traceId: 'test-123',
      timestamp: new Date().toISOString()
    };

    const error = new HttpErrorResponse({
      error: apiResponse,
      status: 404
    });

    // Test error handling
    // ...
  });
});
```

---

## Summary

### Key Points

1. **Interceptor automatically unwraps `ApiResponse<T>`** - Services receive just the data
2. **Errors are enhanced** - Includes `apiError`, `userMessage`, and `validationErrors`
3. **Correlation IDs are tracked** - For debugging and support
4. **Type safety is maintained** - Use TypeScript interfaces matching API DTOs
5. **Backward compatibility** - Handles both old and new response formats during migration

### Quick Reference

```typescript
// Service
this.http.get<YourDto>('/api/endpoint')
  .subscribe({
    next: (data) => {
      // data is already unwrapped
    },
    error: (error) => {
      // error.apiError - ApiError object
      // error.userMessage - User-friendly message
      // error.validationErrors - Validation errors (if any)
    }
  });
```

---

**Last Updated**: 2024-02-15  
**Version**: 1.0
