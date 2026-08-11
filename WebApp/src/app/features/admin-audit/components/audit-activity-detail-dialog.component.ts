import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { PlatformActivityEntry } from '../services/administration-audit-api.service';

@Component({
  selector: 'app-audit-activity-detail-dialog',
  standalone: true,
  imports: [CommonModule, DatePipe, MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Activity details</h2>
    <mat-dialog-content>
      <dl class="detail-grid">
        <div><dt>Summary</dt><dd>{{ data.action }} — {{ data.status }}</dd></div>
        <div><dt>Timestamp</dt><dd>{{ data.timestampUtc | date: 'medium' }}</dd></div>
        <div><dt>Actor</dt><dd>{{ data.userId ?? 'System' }}</dd></div>
        <div><dt>Affected resource</dt><dd>{{ data.target ?? '—' }}</dd></div>
        <div><dt>Module</dt><dd>{{ data.module }}</dd></div>
        <div><dt>Service</dt><dd>{{ data.service }}</dd></div>
        <div><dt>Category</dt><dd>{{ data.category }}</dd></div>
        <div><dt>Correlation ID</dt><dd class="mono">{{ data.correlationId ?? '—' }}</dd></div>
        <div><dt>IP address</dt><dd>{{ data.ipAddress ?? '—' }}</dd></div>
        @if (data.previousValue) {
          <div class="detail-grid__full"><dt>Previous value</dt><dd class="mono">{{ data.previousValue }}</dd></div>
        }
        @if (data.newValue) {
          <div class="detail-grid__full"><dt>New value</dt><dd class="mono">{{ data.newValue }}</dd></div>
        }
        @if (data.detailsJson) {
          <div class="detail-grid__full"><dt>Technical details</dt><dd class="mono">{{ data.detailsJson }}</dd></div>
        }
      </dl>
      <section class="detail-meta">
        <h3>Request metadata</h3>
        <p>Correlation and service metadata are propagated from the originating microservice audit record.</p>
      </section>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" type="button" (click)="close()">Close</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin: 0; }
      .detail-grid__full { grid-column: 1 / -1; }
      dt { font-size: 12px; color: #6b7280; margin: 0; }
      dd { margin: 4px 0 0; font-weight: 500; }
      .mono { font-family: ui-monospace, monospace; font-size: 12px; word-break: break-all; }
      .detail-meta { margin-top: 16px; padding-top: 12px; border-top: 1px solid #e5e7eb; }
      .detail-meta h3 { margin: 0 0 6px; font-size: 14px; }
      .detail-meta p { margin: 0; font-size: 12px; color: #6b7280; }
    `,
  ],
})
export class AuditActivityDetailDialogComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) readonly data: PlatformActivityEntry,
    private readonly dialogRef: MatDialogRef<AuditActivityDetailDialogComponent>,
  ) {}

  close(): void {
    this.dialogRef.close();
  }
}
