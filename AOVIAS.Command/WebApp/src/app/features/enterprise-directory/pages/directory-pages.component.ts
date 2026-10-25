import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { DirectorySearchBarComponent } from '../components/directory-search-bar.component';
import { EnterpriseDirectoryApiService, DirectorySearchResult } from '../services/enterprise-directory-api.service';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';

@Component({
  selector: 'app-directory-search',
  standalone: true,
  imports: [CommonModule, RouterLink, DirectorySearchBarComponent, MatCardModule, SettingsPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="directory-page">
      <app-settings-page-header
        title="Enterprise Directory"
        description="Search people across department, designation, contact, and employment status."
      />
      <mat-card class="directory-page__card">
        <mat-card-content>
          <app-directory-search-bar (search)="runSearch($event)" />
          @if (loading()) {
            <p class="directory-muted">Searching…</p>
          } @else if (result()) {
            <p class="directory-muted">{{ result()!.totalCount }} results ({{ result()!.executionMs }}ms)</p>
            <div class="directory-table-wrap">
              <table class="directory-table">
                <thead>
                  <tr>
                    <th>Person</th>
                    <th>Department</th>
                    <th>Designation</th>
                    <th>Contact</th>
                    <th>Employment Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (item of result()!.items; track item.personId) {
                    <tr>
                      <td><strong>{{ item.displayName }}</strong></td>
                      <td>{{ item.departmentName || '—' }}</td>
                      <td>{{ item.designation || '—' }}</td>
                      <td>{{ item.email || '—' }}</td>
                      <td>{{ item.employmentStatus || '—' }}</td>
                      <td class="directory-actions">
                        <a [routerLink]="['/app/settings/teams/directory/profile', item.personId]">View</a>
                        <span class="directory-actions--muted">Edit</span>
                        <span class="directory-actions--muted">Deactivate</span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [
    `
      .directory-page { display: flex; flex-direction: column; gap: 16px; }
      .directory-page__card { border-radius: 16px; }
      .directory-muted { color: #6b7280; font-size: 13px; }
      .directory-table-wrap { overflow-x: auto; margin-top: 12px; }
      .directory-table { width: 100%; border-collapse: collapse; font-size: 13px; }
      .directory-table th, .directory-table td {
        text-align: left; padding: 12px 10px; border-bottom: 1px solid #e5e7eb; vertical-align: top;
      }
      .directory-table th { font-size: 12px; color: #6b7280; }
      .directory-actions { display: flex; gap: 10px; font-size: 12px; font-weight: 600; }
      .directory-actions a { color: #2f6bff; text-decoration: none; }
      .directory-actions--muted { color: #9ca3af; font-weight: 500; }
    `,
  ],
})
export class DirectorySearchComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private session = inject(SessionService);
  result = signal<DirectorySearchResult | null>(null);
  loading = signal(false);

  runSearch(q: string): void {
    const orgId = this.session.getOrganizationId();
    if (!orgId) return;
    this.loading.set(true);
    this.api.search(orgId, q).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: () => {
        this.result.set(null);
        this.loading.set(false);
      },
    });
  }
}

@Component({
  selector: 'app-directory-results',
  standalone: true,
  imports: [CommonModule, DirectorySearchBarComponent, MatCardModule, SettingsPageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="directory-page">
      <app-settings-page-header title="Search Results" description="Directory search results for your organization." />
      <mat-card class="directory-page__card">
        <mat-card-content>
          <app-directory-search-bar (search)="runSearch($event)" />
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [
    `
      .directory-page { display: flex; flex-direction: column; gap: 16px; }
      .directory-page__card { border-radius: 16px; }
    `,
  ],
})
export class DirectoryResultsComponent extends DirectorySearchComponent {}

@Component({
  selector: 'app-directory-explorer',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section class="directory-page"><h1>People Explorer</h1><p>Browse by department and organization scope.</p></section>`,
})
export class DirectoryExplorerComponent {}

@Component({
  selector: 'app-directory-profile',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="directory-page">
      @if (profile()) {
        <h1>{{ profile()!.displayName }}</h1>
        <p>{{ profile()!.email }}</p>
        @for (e of profile()!.employments; track e.employmentId) {
          <div>{{ e.departmentName }} — {{ e.designation }}</div>
        }
      }
    </section>
  `,
})
export class DirectoryProfileComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private route = inject(ActivatedRoute);
  private session = inject(SessionService);
  profile = signal<import('../services/enterprise-directory-api.service').EnterpriseDirectoryProfile | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    const orgId = this.session.getOrganizationId() ?? undefined;
    this.api.profile(id, orgId).subscribe((p) => this.profile.set(p));
  }
}

@Component({
  selector: 'app-directory-saved',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section class="directory-page"><h1>Saved Searches</h1><ul>@for (s of items(); track s.id) {<li>{{ s.name }}</li>}</ul></section>`,
})
export class DirectorySavedComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private session = inject(SessionService);
  items = signal<import('../services/enterprise-directory-api.service').SavedSearch[]>([]);
  constructor() {
    const orgId = this.session.getOrganizationId();
    if (orgId) this.api.savedSearches(orgId).subscribe((s) => this.items.set(s));
  }
}

@Component({
  selector: 'app-directory-history',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section class="directory-page"><h1>Search History</h1><ul>@for (h of items(); track h.id) {<li>{{ h.searchTerms }} ({{ h.resultCount }})</li>}</ul></section>`,
})
export class DirectoryHistoryComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private session = inject(SessionService);
  items = signal<import('../services/enterprise-directory-api.service').SearchHistoryItem[]>([]);
  constructor() {
    const orgId = this.session.getOrganizationId();
    if (orgId) this.api.history(orgId).subscribe((h) => this.items.set(h));
  }
}

@Component({
  selector: 'app-directory-preferences',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section class="directory-page"><h1>Search Preferences</h1><p>Configure default organization and natural language search.</p></section>`,
})
export class DirectoryPreferencesComponent {}
