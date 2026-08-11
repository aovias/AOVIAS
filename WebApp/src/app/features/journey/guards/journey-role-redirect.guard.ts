import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';

/**
 * Guard for `/app/journey` (empty path).
 * Sprint 2A — primary landing is the Compliance Execution Workspace.
 */
export const journeyRoleRedirectGuard: CanActivateFn = () => {
  const router = inject(Router);
  return router.createUrlTree(['/app/journey/execution']);
};
