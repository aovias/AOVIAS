import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { categorizePromptLayer } from '../models/ai-admin-nav.model';
import { AiIdentityApiService, AIPromptItem } from '../services/ai-identity-api.service';

const CATEGORIES = ['All', 'Compliance', 'Audit', 'Evidence', 'Reports', 'General', 'Support'] as const;

@Component({
  selector: 'app-prompt-library',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Prompt Library"
        description="Browse published prompts organized by category, version, and governance status."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading prompts…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!prompts().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="description"
              message="No prompts in the library yet."
              [showRetry]="false"
            />
          } @else {
            <div class="ai-toolbar">
              <mat-form-field appearance="outline">
                <mat-label>Search</mat-label>
                <input matInput [ngModel]="search()" (ngModelChange)="search.set($event)" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Category</mat-label>
                <mat-select [ngModel]="category()" (ngModelChange)="category.set($event)">
                  @for (c of categories; track c) {
                    <mat-option [value]="c">{{ c }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            @if (!filtered().length) {
              <app-ai-admin-page-state
                state="empty"
                icon="search_off"
                message="No prompts match the current filters."
                [showRetry]="false"
              />
            } @else {
              <div class="ai-table-wrap">
                <table class="ai-table">
                  <thead>
                    <tr>
                      <th>Prompt</th>
                      <th>Category</th>
                      <th>Version</th>
                      <th>Status</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (p of filtered(); track p.id) {
                      <tr>
                        <td>
                          <strong>{{ p.name || p.code }}</strong>
                          <span class="ai-muted">{{ p.code }} · {{ p.layerType }}</span>
                        </td>
                        <td>{{ categoryOf(p) }}</td>
                        <td>v{{ p.currentVersion }}</td>
                        <td><span class="ai-chip">{{ p.status || 'Published' }}</span></td>
                        <td>
                          <a [routerLink]="[p.id, 'versions']" class="ai-link">Versions</a>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .ai-page__card {
        border-radius: 16px;
      }
      .ai-toolbar {
        display: grid;
        grid-template-columns: minmax(180px, 1fr) minmax(140px, 220px);
        gap: 12px;
        margin-bottom: 8px;
      }
      .ai-table-wrap {
        overflow-x: auto;
      }
      .ai-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;
      }
      .ai-table th,
      .ai-table td {
        text-align: left;
        padding: 12px 10px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
        vertical-align: top;
      }
      .ai-table th {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-muted {
        display: block;
        margin-top: 2px;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-chip {
        display: inline-block;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        background: var(--color-primary-50, #eef3ff);
        color: var(--color-primary-700, #1e4bc7);
      }
      .ai-link {
        color: var(--color-primary);
        font-weight: 600;
        text-decoration: none;
      }
      @media (max-width: 640px) {
        .ai-toolbar {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class PromptLibraryComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  readonly categories = CATEGORIES;
  readonly prompts = signal<AIPromptItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly category = signal<(typeof CATEGORIES)[number]>('All');

  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    const cat = this.category();
    return this.prompts().filter((p) => {
      const category = this.categoryOf(p);
      if (cat !== 'All' && category !== cat) return false;
      if (!q) return true;
      return (
        p.code?.toLowerCase().includes(q) ||
        p.name?.toLowerCase().includes(q) ||
        p.layerType?.toLowerCase().includes(q) ||
        category.toLowerCase().includes(q)
      );
    });
  });

  ngOnInit(): void {
    this.reload();
  }

  categoryOf(p: AIPromptItem): string {
    return categorizePromptLayer(p.layerType);
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listPrompts().subscribe({
      next: (d) => {
        this.prompts.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.prompts.set([]);
        this.loading.set(false);
        this.error.set('Unable to load prompts.');
      },
    });
  }
}
