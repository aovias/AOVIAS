import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../ui/services/notification.service';

export type SessionTerminationReason = 'refresh_failed' | 'no_refresh_token' | 'api_unauthorized';

/**
 * Single place for idempotent session teardown + login redirect after refresh failure or forced logout.
 */
@Injectable({
  providedIn: 'root',
})
export class SessionExpiryCoordinatorService {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notification = inject(NotificationService);

  /** True while the first logout + navigate sequence is running (suppress duplicate work). */
  private handling = false;

  /** Exposed for components that want to suppress inline errors during global redirect. */
  readonly terminationInProgress = signal(false);

  handleRefreshFailure(error: unknown): void {
    this.handleSessionTerminated('refresh_failed', error);
  }

  handleSessionTerminated(reason: SessionTerminationReason, _error?: unknown): void {
    if (this.handling) {
      return;
    }
    this.handling = true;
    this.terminationInProgress.set(true);

    this.authService.logout();

    this.notification.showWarning('Session expired. Please sign in again.');

    const returnUrl = this.router.url?.startsWith('/login')
      ? undefined
      : this.router.url || undefined;
    void this.router
      .navigate(['/login'], {
        queryParams: {
          ...(returnUrl ? { returnUrl } : {}),
          sessionExpired: '1',
        },
      })
      .finally(() => {
        this.handling = false;
        this.terminationInProgress.set(false);
      });
  }
}
