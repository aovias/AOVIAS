import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-licensing-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    SettingsPageHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="licensing-page">
      <app-settings-page-header
        title="Licensing"
        description="Commercial license, plan catalogue metadata, seats, and entitlements for this organization."
      />

      <mat-card class="panel">
        <h3>License &amp; plan</h3>
        @if (summary(); as s) {
          <p><strong>Commercial status:</strong> {{ s.organizationCommercialStatus }}</p>
          <p>
            <strong>Plan:</strong> {{ s.planName || s.planCode || '—' }}
            @if (s.planLifecycleStatus) {
              <span class="muted">({{ s.planLifecycleStatus }})</span>
            }
          </p>
          @if (s.license; as lic) {
            <p>
              <strong>License status:</strong> {{ lic.status }}
              @if (lic.statusAlias) {
                <span class="muted">/ {{ lic.statusAlias }}</span>
              }
            </p>
            <p><strong>Type:</strong> {{ lic.licenseType }}</p>
            <p><strong>Key:</strong> {{ lic.keyCode }}</p>
            <p><strong>Seats:</strong> {{ lic.usedSeats }} / {{ lic.maxSeats }}</p>
            <p><strong>Expires:</strong> {{ lic.expiresAtUtc || '—' }}</p>
            <p class="msg">{{ lic.message }}</p>
            @if (!lic.isOperational) {
              <div class="banner warn">License is not fully operational — renew or activate.</div>
            }
          }
          @if (entitlementCodes().length) {
            <div class="ents">
              <h4>Entitlements</h4>
              @for (e of entitlementCodes(); track e) {
                <span class="chip">{{ e }}</span>
              }
            </div>
          }
        } @else if (status(); as st) {
          <p><strong>Status:</strong> {{ st.status }}</p>
          <p><strong>Key:</strong> {{ st.keyCode }}</p>
          <p><strong>Seats:</strong> {{ st.usedSeats }} / {{ st.maxSeats }}</p>
          <p><strong>Expires:</strong> {{ st.expiresAtUtc || '—' }}</p>
          <p class="msg">{{ st.message }}</p>
        } @else {
          <p>No status loaded yet.</p>
        }
        <button mat-stroked-button type="button" (click)="refresh()">
          <mat-icon>refresh</mat-icon>
          Refresh
        </button>
      </mat-card>

      <mat-card class="panel">
        <h3>Activate license</h3>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Organization Id</mat-label>
          <input matInput [(ngModel)]="organizationId" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>License key</mat-label>
          <input matInput [(ngModel)]="keyCode" />
        </mat-form-field>
        <button
          mat-flat-button
          color="primary"
          type="button"
          (click)="activate()"
          [disabled]="busy()"
        >
          Activate
        </button>
        @if (error()) {
          <p class="err">{{ error() }}</p>
        }
      </mat-card>
    </div>
  `,
  styles: [
    `
      .licensing-page {
        display: grid;
        gap: 1rem;
        max-width: 720px;
      }
      .panel {
        padding: 1.25rem;
        display: grid;
        gap: 0.75rem;
      }
      .full {
        width: 100%;
      }
      .banner.warn {
        background: #fff4e5;
        border-left: 4px solid #ed6c02;
        padding: 0.75rem 1rem;
      }
      .err {
        color: #c62828;
      }
      .msg {
        opacity: 0.8;
      }
      .muted {
        opacity: 0.65;
        font-size: 0.9em;
      }
      .ents {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        align-items: center;
      }
      .ents h4 {
        width: 100%;
        margin: 0.5rem 0 0;
        font-size: 0.95rem;
      }
      .chip {
        display: inline-block;
        padding: 0.15rem 0.55rem;
        border-radius: 4px;
        background: #eef2f7;
        font-size: 0.8rem;
      }
    `,
  ],
})
export class LicensingPageComponent {
  private readonly http = inject(HttpClient);

  organizationId = '';
  keyCode = '';
  readonly status = signal<any | null>(null);
  readonly summary = signal<any | null>(null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  entitlementCodes(): string[] {
    const s = this.summary();
    const fromDetails = (s?.entitlements ?? s?.Entitlements ?? [])
      .map((e: any) => e?.code ?? e?.Code ?? e)
      .filter((x: unknown) => typeof x === 'string');
    if (fromDetails.length) return fromDetails;
    return s?.license?.entitlements ?? s?.license?.Entitlements ?? [];
  }

  refresh(): void {
    if (!this.organizationId) {
      this.error.set('Organization Id is required');
      return;
    }
    this.error.set(null);
    this.http
      .get<any>(`${environment.apiBaseUrl}/licensing/summary`, {
        params: { organizationId: this.organizationId },
      })
      .subscribe({
        next: (res) => {
          const data = res?.data ?? res;
          this.summary.set(data);
          this.status.set(data?.license ?? data?.License ?? null);
        },
        error: () => {
          this.http
            .get<any>(`${environment.apiBaseUrl}/licenses/status`, {
              params: { organizationId: this.organizationId },
            })
            .subscribe({
              next: (res) => {
                const data = res?.data ?? res;
                this.status.set(data);
                this.summary.set(null);
              },
              error: () => this.error.set('Failed to load license status'),
            });
        },
      });
  }

  activate(): void {
    if (!this.organizationId || !this.keyCode) {
      this.error.set('Organization Id and license key are required');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.http
      .post<any>(`${environment.apiBaseUrl}/licenses/activate`, {
        keyCode: this.keyCode,
        organizationId: this.organizationId,
        machineFingerprint: 'webapp',
        offline: false,
      })
      .subscribe({
        next: (res) => {
          this.status.set(res?.data ?? res);
          this.busy.set(false);
        },
        error: () => {
          this.error.set('Activation failed');
          this.busy.set(false);
        },
      });
  }
}
