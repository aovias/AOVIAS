import { inject } from '@angular/core';
import { Router, CanActivateFn, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { SessionService } from '../auth/session.service';
import { TokenService } from '../auth/token.service';
import { TokenRefreshService } from '../auth/token-refresh.service';
import { AppContextService } from '../services/app-context.service';
import { PerformanceTimelineService } from '../performance/performance-timeline.service';

/**
 * Admits a navigation when the session is usable, renewing it first when it can be.
 *
 * <p>
 * **Why this guard refreshes (DEF-RC2S1-001).** Token renewal used to be reachable only from
 * `authInterceptor`, which redeems the refresh token when an in-flight request returns 401. A
 * navigation never gets that far: the guard runs *before* any HTTP request, decided expiry from the
 * local clock, and returned a redirect — so no request was issued, no 401 occurred, and the refresh
 * token was never redeemed.
 * </p>
 *
 * <p>
 * The effect was that an operator was signed out the moment their access token aged out, while
 * holding a refresh token that was still good. Measured during RC2S2: a refresh token redeemed
 * successfully **20.8 hours** after the access token expired. The session was recoverable the whole
 * time and the application never asked.
 * </p>
 *
 * <p>
 * So an expired access token is no longer the end of the session — only a failed *renewal* is.
 * `getFreshAccessToken$` is single-flight and already tears the session down on failure, so this
 * adds a trigger, not a second refresh mechanism.
 * </p>
 */
export const authGuard: CanActivateFn = (
  route,
  state,
): boolean | UrlTree | Observable<boolean | UrlTree> => {
  const sessionService = inject(SessionService);
  const tokenService = inject(TokenService);
  const tokenRefresh = inject(TokenRefreshService);
  const appContext = inject(AppContextService);
  const router = inject(Router);
  const perf = inject(PerformanceTimelineService);

  const rejectToLogin = () =>
    router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

  perf.mark('route_guard_auth_start', { url: state.url });
  const valid = sessionService.isSessionValid();
  perf.mark('route_guard_auth_end', { valid });
  perf.measure('route_guard_auth', 'route_guard_auth_start', 'route_guard_auth_end');

  if (valid) {
    appContext.initializeFromSession();
    return true;
  }

  // Expired access token, but a refresh token to redeem: renew before giving up. Without a refresh
  // token there is nothing to try, so fall through to the redirect rather than provoking a request
  // that is certain to fail.
  if (!tokenService.getRefreshToken()) {
    return rejectToLogin();
  }

  return tokenRefresh.getFreshAccessToken$().pipe(
    map(() => {
      appContext.initializeFromSession();
      return true as const;
    }),
    // A refusal is authoritative: the refresh token is spent or rejected, and the session is over.
    // `getFreshAccessToken$` has already run session teardown by this point.
    catchError(() => of(rejectToLogin())),
  );
};
