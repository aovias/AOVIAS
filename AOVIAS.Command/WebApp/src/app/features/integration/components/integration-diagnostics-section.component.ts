import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { IntegrationAuditEntry } from '../services/integration-api.service';

@Component({
  selector: 'app-integration-diagnostics-section',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="diagnostics" aria-label="Integration diagnostics">
      <h3>Diagnostics</h3>
      <div class="diagnostics__grid">
        <div class="diagnostics__panel">
          <h4>Recent errors</h4>
          @if (recentErrors.length === 0) {
            <p class="diagnostics__empty">No recent errors recorded.</p>
          } @else {
            <ul>
              @for (entry of recentErrors; track entry.id) {
                <li>{{ entry.operation }} — {{ entry.result }} <span>{{ entry.occurredAtUtc | date: 'short' }}</span></li>
              }
            </ul>
          }
        </div>
        <div class="diagnostics__panel">
          <h4>Warnings</h4>
          @if (recentWarnings.length === 0) {
            <p class="diagnostics__empty">No warnings recorded.</p>
          } @else {
            <ul>
              @for (entry of recentWarnings; track entry.id) {
                <li>{{ entry.operation }} — {{ entry.result }}</li>
              }
            </ul>
          }
        </div>
        <div class="diagnostics__panel">
          <h4>Connection history</h4>
          @if (connectionHistory.length === 0) {
            <p class="diagnostics__empty">No connection events yet.</p>
          } @else {
            <ul>
              @for (entry of connectionHistory; track entry.id) {
                <li>{{ entry.operation }} — {{ entry.result }} ({{ entry.durationMs }}ms)</li>
              }
            </ul>
          }
        </div>
        <div class="diagnostics__panel diagnostics__panel--placeholder">
          <h4>Performance metrics</h4>
          <p class="diagnostics__empty">Coming soon — latency trends and throughput analytics.</p>
        </div>
        <div class="diagnostics__panel diagnostics__panel--placeholder">
          <h4>Traffic analytics</h4>
          <p class="diagnostics__empty">Coming soon — request volume and error rate dashboards.</p>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .diagnostics h3 {
        margin: 0 0 12px;
        font-size: 16px;
        font-weight: 600;
      }
      .diagnostics__grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 12px;
      }
      .diagnostics__panel {
        padding: 14px;
        border-radius: 12px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
      }
      .diagnostics__panel h4 {
        margin: 0 0 8px;
        font-size: 13px;
        font-weight: 600;
      }
      .diagnostics__panel ul {
        margin: 0;
        padding-left: 18px;
        font-size: 12px;
      }
      .diagnostics__panel li span {
        color: var(--color-text-secondary, #6b7280);
      }
      .diagnostics__empty {
        margin: 0;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .diagnostics__panel--placeholder {
        border-style: dashed;
        background: color-mix(in srgb, var(--color-bg-muted, #f9fafb) 80%, transparent);
      }
    `,
  ],
})
export class IntegrationDiagnosticsSectionComponent {
  @Input() auditEntries: IntegrationAuditEntry[] = [];

  get recentErrors(): IntegrationAuditEntry[] {
    return this.auditEntries
      .filter((e) => /fail|error/i.test(e.result))
      .slice(0, 5);
  }

  get recentWarnings(): IntegrationAuditEntry[] {
    return this.auditEntries
      .filter((e) => /warn|degraded/i.test(e.result))
      .slice(0, 5);
  }

  get connectionHistory(): IntegrationAuditEntry[] {
    return this.auditEntries
      .filter((e) => /connect|validate|test/i.test(e.operation))
      .slice(0, 5);
  }
}
