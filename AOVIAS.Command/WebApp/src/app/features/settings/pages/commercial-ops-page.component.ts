import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-commercial-ops-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    SettingsPageHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="commercial-ops-page">
      <app-settings-page-header
        title="Commercial Ops"
        description="Provision trial licenses, inspect customer health, and download diagnostics packs."
      />
      <p class="ca-link">
        <a routerLink="/commercial-admin">Open Commercial Administration Console</a> (AOVIAS staff
        roles)
      </p>

      <mat-card class="panel">
        <h3>Provision trial</h3>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Organization Id</mat-label>
          <input matInput [(ngModel)]="organizationId" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Customer name</mat-label>
          <input matInput [(ngModel)]="customerName" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Max seats</mat-label>
          <input matInput type="number" [(ngModel)]="maxSeats" />
        </mat-form-field>
        <button
          mat-flat-button
          color="primary"
          type="button"
          (click)="provision()"
          [disabled]="busy()"
        >
          <mat-icon>rocket_launch</mat-icon>
          Provision trial
        </button>
        @if (provisionResult(); as p) {
          <div class="result">
            <p><strong>Key:</strong> {{ p.licenseKeyCode }}</p>
            <p><strong>Expires:</strong> {{ p.expiresAtUtc || '—' }}</p>
            <p>{{ p.message }}</p>
            <ol>
              @for (item of p.checklist; track item) {
                <li>{{ item }}</li>
              }
            </ol>
          </div>
        }
      </mat-card>

      <mat-card class="panel">
        <h3>Customer health</h3>
        <button mat-stroked-button type="button" (click)="loadHealth()" [disabled]="busy()">
          <mat-icon>monitor_heart</mat-icon>
          Refresh health
        </button>
        @if (health(); as h) {
          <pre class="json">{{ h | json }}</pre>
        }
      </mat-card>

      <mat-card class="panel">
        <h3>Diagnostics pack</h3>
        <p>Downloads a redacted JSON pack (versions, license summary, health stubs).</p>
        <button
          mat-stroked-button
          type="button"
          (click)="downloadDiagnostics()"
          [disabled]="busy()"
        >
          <mat-icon>download</mat-icon>
          Download diagnostics
        </button>
      </mat-card>

      @if (error()) {
        <p class="err">{{ error() }}</p>
      }
    </div>
  `,
  styles: [
    `
      .commercial-ops-page {
        display: grid;
        gap: 1rem;
        max-width: 800px;
      }
      .ca-link {
        margin: 0;
        font-size: 0.9rem;
      }
      .panel {
        padding: 1.25rem;
        display: grid;
        gap: 0.75rem;
      }
      .full {
        width: 100%;
      }
      .result {
        background: rgba(0, 0, 0, 0.03);
        padding: 0.75rem 1rem;
        border-radius: 4px;
      }
      .json {
        max-height: 320px;
        overflow: auto;
        font-size: 0.8rem;
      }
      .err {
        color: #c62828;
      }
    `,
  ],
})
export class CommercialOpsPageComponent {
  private readonly http = inject(HttpClient);

  organizationId = '';
  customerName = '';
  maxSeats = 10;

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly provisionResult = signal<any | null>(null);
  readonly health = signal<any | null>(null);

  provision(): void {
    if (!this.organizationId) {
      this.error.set('Organization Id is required');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.http
      .post<any>(`${environment.apiBaseUrl}/commercial/provision`, {
        organizationId: this.organizationId,
        customerName: this.customerName || undefined,
        maxSeats: this.maxSeats,
        validDays: 30,
        gracePeriodDays: 7,
      })
      .subscribe({
        next: (res) => {
          this.provisionResult.set(res?.data ?? res);
          this.busy.set(false);
        },
        error: () => {
          this.error.set('Provision failed (requires platform admin).');
          this.busy.set(false);
        },
      });
  }

  loadHealth(): void {
    if (!this.organizationId) {
      this.error.set('Organization Id is required');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.http
      .get<any>(`${environment.apiBaseUrl}/commercial/customer-health`, {
        params: { organizationId: this.organizationId },
      })
      .subscribe({
        next: (res) => {
          this.health.set(res?.data ?? res);
          this.busy.set(false);
        },
        error: () => {
          this.error.set('Failed to load customer health');
          this.busy.set(false);
        },
      });
  }

  downloadDiagnostics(): void {
    if (!this.organizationId) {
      this.error.set('Organization Id is required');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.http
      .get(`${environment.apiBaseUrl}/commercial/diagnostics`, {
        params: { organizationId: this.organizationId },
        responseType: 'blob',
      })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `aovias-diagnostics-${this.organizationId}.json`;
          a.click();
          URL.revokeObjectURL(url);
          this.busy.set(false);
        },
        error: () => {
          this.error.set('Diagnostics download failed');
          this.busy.set(false);
        },
      });
  }
}
