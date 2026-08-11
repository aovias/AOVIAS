import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Observable, map, of, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { LoggerService } from '../services/logger.service';
import { OrganizationService } from '../services/organization.service';
import { PerformanceTimelineService } from '../performance/performance-timeline.service';

export const onboardingGuard: CanActivateFn = (): Observable<boolean | UrlTree> => {
  const router = inject(Router);
  const logger = inject(LoggerService);
  const organizationService = inject(OrganizationService);
  const perf = inject(PerformanceTimelineService);

  perf.mark('route_guard_onboarding_start');

  return organizationService.loadOrganizationDetails().pipe(
    tap(() => {
      perf.mark('route_guard_onboarding_end');
      perf.measure(
        'route_guard_onboarding',
        'route_guard_onboarding_start',
        'route_guard_onboarding_end',
      );
    }),
    map(() => {
      const isOnboarded = organizationService.hasAnyOnboardedHospital();
      if (isOnboarded) {
        return true;
      }

      logger.info('Onboarding guard redirecting to onboarding: no onboarded hospitals found');
      return router.createUrlTree(['/onboarding']);
    }),
    catchError((error) => {
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        logger.warn('Onboarding guard: organization details unauthorized; redirecting to login', {
          status: error.status,
        });
        return of(router.createUrlTree(['/login'], { queryParams: { returnUrl: router.url } }));
      }
      logger.warn(
        'Onboarding guard failed to load organization details; redirecting to onboarding',
        error,
      );
      return of(router.createUrlTree(['/onboarding']));
    }),
  );
};

export const onboardingEntryGuard: CanActivateFn = (): Observable<boolean | UrlTree> => {
  const router = inject(Router);
  const logger = inject(LoggerService);
  const organizationService = inject(OrganizationService);

  return organizationService.loadOrganizationDetails().pipe(
    map(() => {
      const isOnboarded = organizationService.hasAnyOnboardedHospital();
      if (!isOnboarded) {
        return true;
      }

      logger.info('Onboarding entry guard redirecting onboarded user to dashboard');
      return router.createUrlTree(['/app/dashboard']);
    }),
    catchError((error) => {
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        return of(router.createUrlTree(['/login'], { queryParams: { returnUrl: router.url } }));
      }
      logger.warn(
        'Onboarding entry guard failed to load organization details; allowing onboarding route',
        error,
      );
      return of(true);
    }),
  );
};
