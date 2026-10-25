import type { UtilityItem } from './types';

/** Future-ready utility navigation (portals, account). */
export const utilityNavigation: readonly UtilityItem[] = [
  {
    id: 'pricing',
    label: 'Pricing',
    href: '/pricing',
    visibility: 'visible',
  },
  {
    id: 'docs',
    label: 'Docs',
    href: '/resources/docs',
    visibility: 'visible',
  },
  {
    id: 'sign-in',
    /** Hidden until Identity is live — auth routes remain for Release 2 wiring. */
    label: 'Sign in',
    href: '/auth/sign-in',
    visibility: 'future',
    hidden: true,
  },
  {
    id: 'customer-portal',
    label: 'Customer Portal',
    href: '/portal',
    visibility: 'future',
    hidden: true,
  },
] as const;

export function getVisibleUtilityItems(
  items: readonly UtilityItem[] = utilityNavigation,
): UtilityItem[] {
  return items.filter((item) => !item.hidden && item.visibility !== 'future');
}
