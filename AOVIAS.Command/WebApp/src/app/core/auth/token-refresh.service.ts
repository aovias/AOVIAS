import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, defer, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { TokenService } from './token.service';
import { LoggerService } from '../services/logger.service';
import { SessionExpiryCoordinatorService } from '../session/session-expiry-coordinator.service';

/**
 * Single-flight token refresh: concurrent 401s share one POST /identity/refresh.
 * All refresh failures (HTTP, network, missing refresh token) are non-recoverable without re-login.
 */
@Injectable({
  providedIn: 'root',
})
export class TokenRefreshService {
  private readonly authService = inject(AuthService);
  private readonly tokenService = inject(TokenService);
  private readonly logger = inject(LoggerService);
  private readonly sessionExpiry = inject(SessionExpiryCoordinatorService);

  private refreshInFlight$: Observable<void> | null = null;

  constructor() {}

  /**
   * Returns after access tokens are updated in storage, or errors after session teardown.
   */
  getFreshAccessToken$(): Observable<void> {
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = defer(() => {
        const rt = this.tokenService.getRefreshToken();
        if (!rt) {
          const err = new Error('no_refresh_token');
          return throwError(() => err);
        }
        return this.authService.refreshToken();
      }).pipe(
        map(() => void 0),
        catchError((err: unknown) => {
          this.logRefreshFailure(err);
          if (err instanceof Error && err.message === 'no_refresh_token') {
            this.sessionExpiry.handleSessionTerminated('no_refresh_token', err);
          } else {
            this.sessionExpiry.handleRefreshFailure(err);
          }
          return throwError(() => err);
        }),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: true }),
      );
    }
    return this.refreshInFlight$;
  }

  private logRefreshFailure(err: unknown): void {
    const status = err instanceof HttpErrorResponse ? err.status : 0;
    const url =
      err instanceof HttpErrorResponse ? (err.url ?? '/identity/refresh') : '/identity/refresh';
    this.logger.warn('Refresh token request failed', { event: 'refresh_failed', status, url });
  }
}
