import type { NavigationCategory } from '../models/navigation.model';

export const NavigationCategoryIds = {
  Dashboard: 'dashboard',
  QualityManagement: 'quality-management',
  ComplianceExecution: 'compliance-execution',
  ActionCenter: 'action-center',
  Administration: 'administration',
  HeaderModules: 'header-modules',
  Bottom: 'bottom',
} as const;

export const CATEGORY_REGISTRY: ReadonlyArray<NavigationCategory> = Object.freeze([
  Object.freeze({
    id: NavigationCategoryIds.Dashboard,
    title: 'Dashboard',
    order: 10,
    surface: 'sidebar' as const,
  }),
  Object.freeze({
    id: NavigationCategoryIds.QualityManagement,
    title: 'Quality Management',
    order: 20,
    surface: 'sidebar' as const,
  }),
  Object.freeze({
    id: NavigationCategoryIds.ComplianceExecution,
    title: 'Compliance Execution',
    order: 30,
    surface: 'sidebar' as const,
  }),
  Object.freeze({
    id: NavigationCategoryIds.ActionCenter,
    title: 'Action Center',
    order: 40,
    surface: 'sidebar' as const,
  }),
  Object.freeze({
    id: NavigationCategoryIds.Administration,
    title: 'Administration',
    order: 50,
    surface: 'sidebar' as const,
  }),
  Object.freeze({
    id: NavigationCategoryIds.HeaderModules,
    title: 'Modules',
    order: 10,
    surface: 'header' as const,
  }),
  Object.freeze({
    id: NavigationCategoryIds.Bottom,
    title: 'Bottom',
    order: 10,
    surface: 'bottom' as const,
  }),
]);
