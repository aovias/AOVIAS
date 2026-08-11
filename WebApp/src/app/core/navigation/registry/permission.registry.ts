import { NavigationPermissionKeys } from '../models/permission.model';

/** Permission metadata registry (no evaluation — keys for future providers). */
export const PERMISSION_REGISTRY = Object.freeze({
  ...NavigationPermissionKeys,
} as const);

export { NavigationPermissionKeys };
