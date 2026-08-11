import { Routes } from '@angular/router';
import { AccountSecurityComponent } from './pages/account-security.component';

export const ACCOUNT_ROUTES: Routes = [
  { path: 'security', component: AccountSecurityComponent },
  { path: '', redirectTo: 'security', pathMatch: 'full' },
];
