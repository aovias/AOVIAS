import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  PageBreadcrumbComponent,
  type PageBreadcrumbItem,
} from '../../../shared/ui/page-breadcrumb.component';

@Component({
  selector: 'app-organization-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, PageBreadcrumbComponent],
  template: `
    <div class="aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Organization Settings</h1>
          <p class="aov-page__subtitle">Reserved for future organization-level configuration.</p>
        </div>
      </header>
      <div class="aov-empty">
        <p class="aov-empty__title">Organization settings coming soon</p>
        <p class="aov-empty__message">
          This area is reserved so the Organization administration console can grow without changing
          information architecture.
        </p>
      </div>
    </div>
  `,
})
export class OrganizationSettingsPageComponent {
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Settings' },
  ];
}
