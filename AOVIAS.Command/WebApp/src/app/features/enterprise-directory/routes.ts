import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';

export const ENTERPRISE_DIRECTORY_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/directory-home.component').then((m) => m.DirectoryHomeComponent), canActivate: [permissionGuard('Directory.Search.Read')] },
  { path: 'search', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectorySearchComponent), canActivate: [permissionGuard('Directory.Search.Read')] },
  { path: 'explorer', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectoryExplorerComponent), canActivate: [permissionGuard('Directory.Search.Read')] },
  { path: 'results', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectoryResultsComponent), canActivate: [permissionGuard('Directory.Search.Read')] },
  { path: 'profile/:id', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectoryProfileComponent), canActivate: [permissionGuard('Directory.Profile.Read')] },
  { path: 'saved', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectorySavedComponent), canActivate: [permissionGuard('Directory.SavedSearch.Manage')] },
  { path: 'history', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectoryHistoryComponent), canActivate: [permissionGuard('Directory.History.Read')] },
  { path: 'preferences', loadComponent: () => import('./pages/directory-pages.component').then((m) => m.DirectoryPreferencesComponent), canActivate: [permissionGuard('Directory.Search.Read')] },
];
