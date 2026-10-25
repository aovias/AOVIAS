import { asRoleIds } from '../../../../core/auth/roles/role-definitions';

/**
 * Roles permitted on the auditor session surfaces.
 *
 * <p>
 * Referenced once, by the surfaces in `workflow-definition.model.ts` that declare
 * `requiresAnyRole`. Both the route guard and the sub-tab strip read that declaration, so the
 * workspace cannot offer a destination the guard would reject — the property the original
 * consolidation was careful to preserve, now enforced by there being one declaration rather than
 * two that happened to agree.
 * </p>
 *
 * <p>
 * The `Set` form that lived here is gone with the three component-level role checks that used it.
 * </p>
 */
export const AUDITOR_ROLE_IDS = asRoleIds([
  'EXTERNAL_AUDITOR',
  'REGULATOR',
  'REGULATORY_ADMIN',
  'SUPER_ADMIN',
  'ORG_ADMIN',
]);
