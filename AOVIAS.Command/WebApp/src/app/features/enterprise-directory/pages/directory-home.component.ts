import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { DirectorySearchBarComponent } from '../components/directory-search-bar.component';
import { EnterpriseDirectoryApiService, DirectorySuggestion } from '../services/enterprise-directory-api.service';
import { SessionService } from '../../../core/auth/session.service';

@Component({
  selector: 'app-directory-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, DirectorySearchBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="directory-page">
      <header class="directory-page__header">
        <h1>Enterprise Directory</h1>
        <p>Governed people discovery across your organization</p>
      </header>
      <app-directory-search-bar (search)="onSearch($event)" />
      <nav class="directory-page__links">
        <a routerLink="search">Advanced Search</a>
        <a routerLink="explorer">People Explorer</a>
        <a routerLink="saved">Saved Searches</a>
        <a routerLink="history">Search History</a>
      </nav>
      @if (popular().length) {
        <div class="directory-page__popular">
          <h2>Popular searches</h2>
          <div class="directory-page__chips">
            @for (p of popular(); track p.term) {
              <button type="button" mat-stroked-button (click)="onSearch(p.term)">{{ p.term }} ({{ p.count }})</button>
            }
          </div>
        </div>
      }
    </section>
  `,
  styles: [`
    .directory-page { padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end); }
    .directory-page__header h1 { margin: 0 0 0.25rem; }
    .directory-page__links { display: flex; gap: 1rem; margin: 1.5rem 0; flex-wrap: wrap; }
    .directory-page__chips { display: flex; gap: 0.5rem; flex-wrap: wrap; }
  `],
})
export class DirectoryHomeComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private session = inject(SessionService);
  popular = signal<{ term: string; count: number }[]>([]);
  query = '';

  constructor() {
    const orgId = this.session.getOrganizationId();
    if (orgId) {
      this.api.popular(orgId).subscribe((items) => this.popular.set(items));
    }
  }

  onSearch(q: string): void {
    this.query = q;
  }
}
