import { TestBed } from '@angular/core/testing';
import {
  Router,
  UrlTree,
  type ActivatedRouteSnapshot,
  type RouterStateSnapshot,
} from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Observable, isObservable, of, throwError } from 'rxjs';
import { authGuard } from './auth.guard';
import { SessionService } from '../auth/session.service';
import { TokenService } from '../auth/token.service';
import { TokenRefreshService } from '../auth/token-refresh.service';
import { AppContextService } from '../services/app-context.service';
import { PerformanceTimelineService } from '../performance/performance-timeline.service';

/**
 * DEF-RC2S1-001 — an expired access token must not end a session that can still be renewed.
 *
 * <p>
 * Token renewal was reachable only from the HTTP interceptor's 401 path. A navigation never reaches
 * it: the guard runs first, decided expiry from the local clock, and redirected — so no request was
 * issued and the refresh token was never redeemed. Measured in RC2S2, a refresh token was still good
 * **20.8 hours** after the access token expired, while the operator sat on the login screen.
 * </p>
 *
 * <p>
 * These tests pin the trigger. They deliberately assert *when a refresh is attempted*, not how
 * refreshing works — that belongs to `TokenRefreshService`, which is stubbed here.
 * </p>
 */
describe('authGuard', () => {
  let session: { isSessionValid: ReturnType<typeof vi.fn> };
  let tokens: { getRefreshToken: ReturnType<typeof vi.fn> };
  let refresh: { getFreshAccessToken$: ReturnType<typeof vi.fn> };
  let appContext: { initializeFromSession: ReturnType<typeof vi.fn> };

  const run = () =>
    TestBed.runInInjectionContext(() =>
      authGuard(
        {} as ActivatedRouteSnapshot,
        { url: '/app/audit/execution/sessions' } as RouterStateSnapshot,
      ),
    );

  /** Guard results may be sync or async; normalise so each test reads the same way. */
  const resolve = async (result: ReturnType<typeof run>) =>
    isObservable(result)
      ? await new Promise((r) => (result as Observable<unknown>).subscribe(r))
      : result;

  beforeEach(() => {
    session = { isSessionValid: vi.fn(() => false) };
    tokens = { getRefreshToken: vi.fn(() => 'refresh-token') };
    refresh = { getFreshAccessToken$: vi.fn(() => of(void 0)) };
    appContext = { initializeFromSession: vi.fn() };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionService, useValue: session },
        { provide: TokenService, useValue: tokens },
        { provide: TokenRefreshService, useValue: refresh },
        { provide: AppContextService, useValue: appContext },
        { provide: PerformanceTimelineService, useValue: { mark: vi.fn(), measure: vi.fn() } },
        {
          provide: Router,
          useValue: { createUrlTree: vi.fn(() => ({ __urlTree: true }) as unknown as UrlTree) },
        },
      ],
    });
  });

  it('admits a valid session without touching the refresh token', () => {
    session.isSessionValid.mockReturnValue(true);

    expect(run()).toBe(true);
    expect(refresh.getFreshAccessToken$).not.toHaveBeenCalled();
    expect(appContext.initializeFromSession).toHaveBeenCalled();
  });

  it('renews an expired session rather than ending it', async () => {
    // The defect: this navigation used to redirect to /login while a good refresh token sat unused.
    const result = await resolve(run());

    expect(refresh.getFreshAccessToken$).toHaveBeenCalledTimes(1);
    expect(result).toBe(true);
    expect(appContext.initializeFromSession).toHaveBeenCalled();
  });

  it('redirects when the refresh is refused — a spent token really is the end of the session', async () => {
    refresh.getFreshAccessToken$.mockReturnValue(throwError(() => new Error('invalid_grant')));

    const result = await resolve(run());

    expect(result).toHaveProperty('__urlTree', true);
    expect(appContext.initializeFromSession).not.toHaveBeenCalled();
  });

  it('does not attempt a refresh when there is no refresh token to redeem', () => {
    tokens.getRefreshToken.mockReturnValue(null);

    const result = run();

    expect(refresh.getFreshAccessToken$).not.toHaveBeenCalled();
    expect(result).toHaveProperty('__urlTree', true);
  });
});
