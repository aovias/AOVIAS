import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { UserContextService } from '../../../core/services/user-context.service';

@Component({
  selector: 'app-workforce-invitations-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page">
      <h1>Workforce invitations</h1>
      <p class="sub">Pending email invitations. Resend or cancel without creating accounts until accepted.</p>
      <div class="toolbar">
        <select [(ngModel)]="status">
          <option value="">All statuses</option>
          <option>Pending</option>
          <option>Accepted</option>
          <option>Expired</option>
          <option>Revoked</option>
          <option>Cancelled</option>
        </select>
        <button class="primary" type="button" (click)="load()">Refresh</button>
      </div>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <table *ngIf="items().length">
        <thead>
          <tr>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
            <th>Expires</th>
            <th>Pipeline</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let i of items()">
            <td>{{ i.email }}</td>
            <td>{{ i.role }}</td>
            <td>{{ i.status }}</td>
            <td>{{ i.expiresAt | date: 'short' }}</td>
            <td>{{ i.pipelineStep || '—' }}</td>
            <td class="actions">
              <button type="button" *ngIf="i.status === 'Pending' || i.status === 'Expired'" (click)="resend(i.id)">
                Resend
              </button>
              <button type="button" *ngIf="i.status === 'Pending'" (click)="cancel(i.id)">Cancel</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!items().length && !error()">No invitations found.</p>
    </div>
  `,
  styles: [
    `
      .page {
        padding: 1.25rem;
      }
      .sub {
        color: #6b7280;
      }
      .toolbar {
        display: flex;
        gap: 0.5rem;
        margin: 1rem 0;
      }
      .primary {
        background: #0b5fff;
        color: #fff;
        border: 0;
        padding: 0.4rem 0.8rem;
        border-radius: 4px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      th,
      td {
        border-bottom: 1px solid #e5e7eb;
        text-align: left;
        padding: 0.5rem;
        font-size: 0.9rem;
      }
      .actions button {
        margin-right: 0.35rem;
      }
      .err {
        color: #b91c1c;
      }
    `,
  ],
})
export class WorkforceInvitationsPageComponent {
  private readonly http = inject(HttpClient);
  private readonly userContext = inject(UserContextService);
  status = 'Pending';
  readonly items = signal<any[]>([]);
  readonly error = signal<string | null>(null);

  load(): void {
    const orgId = this.userContext.getOrganization()?.id;
    if (!orgId) {
      this.error.set('Organization context required');
      return;
    }
    const params: Record<string, string> = {
      organizationId: orgId,
      page: '1',
      pageSize: '50',
    };
    if (this.status) params['status'] = this.status;
    this.http.get<any>(`${environment.apiBaseUrl}/invitations`, { params }).subscribe({
      next: (res) => {
        const data = res?.data ?? res;
        this.items.set(data?.items ?? data?.Items ?? []);
        this.error.set(null);
      },
      error: (e) => this.error.set(e?.message || 'Failed to load invitations'),
    });
  }

  resend(id: string): void {
    this.http.post(`${environment.apiBaseUrl}/invitations/${id}/resend`, {}).subscribe({
      next: () => this.load(),
      error: (e) => this.error.set(e?.message || 'Resend failed'),
    });
  }

  cancel(id: string): void {
    this.http.post(`${environment.apiBaseUrl}/invitations/${id}/cancel`, {}).subscribe({
      next: () => this.load(),
      error: (e) => this.error.set(e?.message || 'Cancel failed'),
    });
  }

  constructor() {
    this.load();
  }
}
