export * from './sidebar/sidebar.models';
export * from './sidebar/sidebar.state';
export { SidebarService } from './sidebar/sidebar.service';
export { SidebarComponent } from './sidebar/sidebar.component';
export { SidebarItemComponent } from './sidebar/sidebar-item.component';
export { SidebarGroupComponent } from './sidebar/sidebar-group.component';
export { FavoritesSectionComponent } from './sidebar/favorites-section.component';
export { RecentSectionComponent } from './sidebar/recent-section.component';
export { CollapseToggleComponent } from './sidebar/collapse-toggle.component';

export { HeaderComponent } from './header/app-header.component';
export { ModuleSwitcherComponent } from './header/module-switcher/module-switcher.component';
export { ModuleSwitcherService } from './header/module-switcher/module-switcher.service';
export type { ModuleEntry } from './header/module-switcher/module-switcher.models';
export { RoleSelectorComponent } from './header/role-selector.component';
export { WorkspaceContextComponent } from './header/workspace-context.component';
export { HeaderActionsComponent } from './header/header-actions.component';
export { HeaderLayoutService } from './header/header-layout.service';
export { HeaderRefreshService } from './header/header-refresh.service';
export { PageTitleService } from './header/page-title.service';
export { SearchTriggerComponent } from './header/search-trigger.component';
export { OverflowMenuComponent } from './header/overflow-menu.component';

/** NAV-3 engine / query / models */
export { NavigationEngineService } from './engine/navigation-engine.service';
export { NavigationQueryService } from './engine/navigation-query.service';
export { NavigationStateService } from './engine/navigation-state.service';
export { NavigationCacheService } from './engine/navigation-cache.service';
export { NavigationHistoryService } from './engine/navigation-history.service';
export { provideNavigationFramework } from './engine/provide-navigation';
export {
  NAV_ROLE_VISIBILITY,
  NAV_PERMISSION_VISIBILITY,
  NAV_FEATURE_FLAGS,
  NAV_BADGE_PROVIDERS,
  NAV_BADGE_RESOLVER,
} from './engine/providers/navigation.tokens';

export { MENU_REGISTRY } from './registry/menu.registry';
export {
  MODULE_REGISTRY,
  NavigationModuleIds,
  COMPLIANCE_GRAPH_NAV_ROLES,
} from './registry/module.registry';
export { CATEGORY_REGISTRY, NavigationCategoryIds } from './registry/category.registry';
export { FEATURE_REGISTRY, DEFAULT_FEATURE_ENABLED } from './registry/feature.registry';
export { PERMISSION_REGISTRY, NavigationPermissionKeys } from './registry/permission.registry';

export type {
  NavigationItem,
  NavigationCategory,
  NavigationContext,
  NavigationSurface,
  NavigationExtension,
  NavigationSidebarSection,
  NavigationHeaderModule,
  NavigationBottomItem,
  NavigationBreadcrumbItem,
} from './models/navigation.model';
export type { NavigationModule } from './models/module.model';
export type { NavigationFeatureKey } from './models/feature.model';
export { NavigationFeatureKeys } from './models/feature.model';
export type { NavigationBadgeKey, NavigationBadgeSnapshot } from './models/badge.model';
export { NavigationBadgeKeys } from './models/badge.model';

export { SidebarProvider } from './surfaces/sidebar.provider';
export { HeaderProvider } from './surfaces/header.provider';
export { BreadcrumbProvider } from './surfaces/breadcrumb.provider';
export { SearchProvider } from './surfaces/search.provider';
export { FavoritesProvider } from './surfaces/favorites.provider';

/** NAV-4 intelligent navigation */
export { CommandPaletteService } from './search/command-palette.service';
export { CommandPaletteComponent } from './search/command-palette.component';
export { SearchIndexService } from './search/search-index.service';
export { FavoritesService } from './favorites/favorites.service';
export { PinnedService } from './favorites/pinned.service';
export { WorkspacePreferencesService } from './personalization/workspace-preferences.service';
export { PersonalizationService } from './personalization/personalization.service';
export { NavigationPreferencesComponent } from './personalization/navigation-preferences.component';
export { KeyboardShortcutsService } from './shortcuts/keyboard-shortcuts.service';
export { PinnedSectionComponent } from './sidebar/pinned-section.component';
