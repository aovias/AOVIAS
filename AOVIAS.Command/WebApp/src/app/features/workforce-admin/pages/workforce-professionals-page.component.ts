import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import {
  ProfessionalApiService,
  ProfessionalDetail,
  ProfessionalListItem,
  RenewalItem,
} from '../../professional/services/professional-api.service';
import { WorkforceAdminPageStateComponent } from '../components/workforce-admin-page-state.component';
import { WORKFORCE_ADMIN_BASE } from '../models/workforce-admin-nav.model';
import { catchError, forkJoin, of } from 'rxjs';

interface ProfessionalRow {
  id: string;
  registrationNumber: string;
  personName: string;
  professionalType: string;
  speciality: string;
  expiry: string;
  verificationStatus: string;
}

@Component({
  selector: 'app-workforce-professionals-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    SettingsPageHeaderComponent,
    WorkforceAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wf-page">
      <app-settings-page-header
        title="Professional Registry"
        description="Professional registrations, specialties, expiry, and verification status from the existing Professional Profiles APIs."
      />
      <mat-card class="wf-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-workforce-admin-page-state state="loading" message="Loading professional registry…" [showRetry]="false" />
          } @else if (error()) {
            <app-workforce-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!rows().length) {
            <app-workforce-admin-page-state
              state="empty"
              icon="verified_user"
              message="No professional profiles are registered yet."
              [showRetry]="false"
            />
          } @else {
            <div class="wf-toolbar">
              <mat-form-field appearance="outline" class="wf-toolbar__search">
                <mat-label>Search registry</mat-label>
                <input matInput [(ngModel)]="searchTerm" (keyup.enter)="reload()" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
              <button mat-stroked-button type="button" (click)="reload()">Search</button>
              <button mat-stroked-button type="button" (click)="prev()" [disabled]="page() <= 1">Previous</button>
              <button mat-stroked-button type="button" (click)="next()" [disabled]="!hasMore()">Next</button>
              <span class="wf-muted">Page {{ page() }} · {{ total() }} total</span>
            </div>
            <div class="wf-table-wrap">
              <table class="wf-table">
                <thead>
                  <tr>
                    <th>Registration Number</th>
                    <th>Person</th>
                    <th>Professional Type</th>
                    <th>Speciality</th>
                    <th>Expiry</th>
                    <th>Verification Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (p of rows(); track p.id) {
                    <tr>
                      <td><strong>{{ p.registrationNumber }}</strong></td>
                      <td>{{ p.personName }}</td>
                      <td>{{ p.professionalType }}</td>
                      <td>{{ p.speciality }}</td>
                      <td>{{ p.expiry }}</td>
                      <td><span class="wf-chip">{{ p.verificationStatus }}</span></td>
                      <td>
                        <a class="wf-link" [routerLink]="['/app/professional', p.id]">View</a>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .wf-page__card { border-radius: 16px; }
      .wf-toolbar {
        display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin-bottom: 12px;
      }
      .wf-toolbar__search { width: min(320px, 100%); }
      .wf-table-wrap { overflow-x: auto; }
      .wf-table { width: 100%; border-collapse: collapse; font-size: 13px; }
      .wf-table th, .wf-table td {
        text-align: left; padding: 12px 10px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb); vertical-align: top;
      }
      .wf-table th { font-size: 12px; color: var(--color-text-secondary, #6b7280); }
      .wf-chip {
        display: inline-block; padding: 2px 10px; border-radius: 999px;
        font-size: 12px; font-weight: 600; background: #ecfdf5; color: #047857;
      }
      .wf-link {
        color: var(--color-primary); font-weight: 600; text-decoration: none;
      }
      .wf-muted { font-size: 12px; color: var(--color-text-secondary, #6b7280); }
    `,
  ],
})
export class WorkforceProfessionalsPageComponent implements OnInit {
  private readonly api = inject(ProfessionalApiService);
  readonly workforceBase = WORKFORCE_ADMIN_BASE;

  readonly rows = signal<ProfessionalRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  searchTerm = '';

  ngOnInit(): void {
    this.reload();
  }

  hasMore(): boolean {
    return this.page() * this.pageSize < this.total();
  }

  prev(): void {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    this.reload();
  }

  next(): void {
    if (!this.hasMore()) return;
    this.page.update((p) => p + 1);
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    const term = this.searchTerm.trim();
    const list$ = term
      ? this.api.search(term, this.page(), this.pageSize)
      : this.api.list(this.page(), this.pageSize);

    list$.subscribe({
      next: (page) => {
        const items = page.items ?? [];
        this.total.set(page.totalCount ?? items.length);
        if (!items.length) {
          this.rows.set([]);
          this.loading.set(false);
          return;
        }

        // Enrich first page items with detail/renewal context from existing APIs.
        forkJoin({
          renewals: this.api.getRenewals(120).pipe(catchError(() => of([] as RenewalItem[]))),
          details: forkJoin(
            items.slice(0, 10).map((item) =>
              this.api.getById(item.id).pipe(catchError(() => of(null as ProfessionalDetail | null))),
            ),
          ).pipe(catchError(() => of([] as Array<ProfessionalDetail | null>))),
        }).subscribe({
          next: ({ renewals, details }) => {
            const detailMap = new Map(
              (details ?? []).filter(Boolean).map((d) => [d!.id, d!] as const),
            );
            const renewalMap = new Map<string, RenewalItem>();
            for (const r of renewals ?? []) {
              if (!renewalMap.has(r.professionalProfileId)) {
                renewalMap.set(r.professionalProfileId, r);
              }
            }
            this.rows.set(items.map((item) => this.toRow(item, detailMap.get(item.id), renewalMap.get(item.id))));
            this.loading.set(false);
          },
          error: () => {
            this.rows.set(items.map((item) => this.toRow(item)));
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
        this.error.set('Unable to load professional registry.');
      },
    });
  }

  private toRow(
    item: ProfessionalListItem,
    detail?: ProfessionalDetail | null,
    renewal?: RenewalItem,
  ): ProfessionalRow {
    const registration = detail?.registrations?.[0];
    const certification = detail?.certifications?.[0];
    return {
      id: item.id,
      registrationNumber: registration?.registrationNumber || item.professionalNumber,
      personName: item.personSearchName,
      professionalType: renewal?.itemType || (registration ? 'Registration' : 'Professional'),
      speciality: certification?.certificationName || detail?.competencies?.[0]?.skillName || '—',
      expiry:
        registration?.expiryDate ||
        renewal?.expiryDate ||
        certification?.expiryDate ||
        '—',
      verificationStatus: registration?.status || certification ? 'Verified' : 'Recorded',
    };
  }
}
