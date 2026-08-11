import { Injectable } from '@angular/core';
import type { NavigationContext, NavigationItem } from '../../models/navigation.model';
import type { PermissionVisibilityProvider } from './navigation.tokens';

/** Stub — always allows. Real permission evaluation is a future extension. */
@Injectable({ providedIn: 'root' })
export class NoOpPermissionVisibilityProvider implements PermissionVisibilityProvider {
  isItemVisible(_item: NavigationItem, _ctx: NavigationContext): boolean {
    return true;
  }
}
