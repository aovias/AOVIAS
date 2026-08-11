import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { TokenService } from '../auth/token.service';
import { TokenRefreshService } from '../auth/token-refresh.service';
import { ComplianceContextService } from '../services/compliance-context.service';
import { SKIP_AUTH_REFRESH } from '../http/http-context.tokens';
import { normalizeGuid } from '../../shared/utils/guid.utils';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const tokenRefresh = inject(TokenRefreshService);
  const complianceContext = inject(ComplianceContextService);

  const accessToken = tokenService.getAccessToken();
  const contextHospitalId = normalizeGuid(complianceContext.getHospitalId());
  const jwtHospitalId = normalizeGuid(
    accessToken ? tokenService.decodeJwt(accessToken)?.hospital_id : null,
  );
  // Only attach X-Hospital-Id when it matches the JWT claim — a mismatch triggers
  // Gateway TenantIsolationMiddleware 403 on nearly every authenticated call.
  const hospitalId =
    contextHospitalId && jwtHospitalId && contextHospitalId === jwtHospitalId
      ? contextHospitalId
      : (jwtHospitalId ?? undefined);

  const isAuthEndpoint =
    req.url.includes('/identity/login') || req.url.includes('/identity/refresh');

  let clonedReq = req;

  if (accessToken && !isAuthEndpoint) {
    clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
  }

  if (hospitalId && !isAuthEndpoint) {
    clonedReq = clonedReq.clone({
      setHeaders: {
        'X-Hospital-Id': hospitalId,
      },
    });
  }

  return next(clonedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      if (req.context.get(SKIP_AUTH_REFRESH)) {
        return throwError(() => error);
      }

      if (isAuthEndpoint) {
        return throwError(() => error);
      }

      return tokenRefresh.getFreshAccessToken$().pipe(
        switchMap(() => {
          const newToken = tokenService.getAccessToken();
          if (!newToken) {
            return throwError(() => error);
          }
          const retryJwtHospital = normalizeGuid(tokenService.decodeJwt(newToken)?.hospital_id);
          const retryHeaders: Record<string, string> = {
            Authorization: `Bearer ${newToken}`,
          };
          if (retryJwtHospital) {
            retryHeaders['X-Hospital-Id'] = retryJwtHospital;
          }
          const retryReq = req.clone({
            setHeaders: retryHeaders,
            context: req.context.set(SKIP_AUTH_REFRESH, true),
          });
          return next(retryReq);
        }),
        catchError((err) => throwError(() => err)),
      );
    }),
  );
};
