import { HttpInterceptorFn, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';
import { LoggerService } from '../services/logger.service';

export interface ApiResponse<T> {
  success: boolean;
  code: string;
  message: string;
  data: T;
  errors: string[];
  meta?: any;
  error?: any;
  traceId?: string;
  timestamp?: string;
}

export const apiResponseInterceptor: HttpInterceptorFn = (req, next) => {
  const logger = inject(LoggerService);
  return next(req).pipe(
    map((event) => {
      // Only process HttpResponse events
      if (event instanceof HttpResponse) {
        const body = event.body;

        // Binary downloads — never unwrap as ApiResponse<T>
        if (body instanceof Blob) {
          return event;
        }

        // Check if response is ApiResponse envelope
        if (body && typeof body === 'object' && 'success' in body) {
          const apiResponse = body as ApiResponse<any>;

          if (apiResponse.success) {
            // Return HttpResponse with data property
            return event.clone({
              body: apiResponse.data,
            });
          } else {
            // Throw error for unsuccessful responses
            throw new HttpErrorResponse({
              error: apiResponse.error || apiResponse.errors,
              status: 400,
              statusText: apiResponse.message || 'Request failed',
              url: req.url,
            });
          }
        }
      }
      return event;
    }),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 || error.status === 403) {
        logger.debug('API response error (auth)', { status: error.status, url: req.url });
      } else {
        console.error('API Error:', error);
      }
      return throwError(() => error);
    }),
  );
};
