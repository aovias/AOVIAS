import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthorizationService } from '../authorization/authorization.service';

export const permissionGuard = (permissionCode: string): CanActivateFn => () => {
  const authz = inject(AuthorizationService);
  const router = inject(Router);
  return authz.can(permissionCode).pipe(
    map((allowed) => allowed || router.createUrlTree(['/error/403'])),
    catchError(() => of(router.createUrlTree(['/error/403']))),
  );
};
