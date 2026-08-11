import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import type { IncidentDetail } from '../models/incident.models';
import { formatIncidentLabel } from '../models/incident.models';

@Component({
  selector: 'app-incident-detail-panel',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    @if (detail(); as d) {
      <aside class="detail-panel">
        <header class="detail-panel__header">
          <div>
            <span class="detail-panel__num">{{ d.incidentNumber }}</span>
            <h2>{{ d.title }}</h2>
          </div>
          <button type="button" class="detail-panel__close" (click)="close.emit()" aria-label="Close">×</button>
        </header>

        <div class="detail-panel__body">
          <section>
            <h3>Overview</h3>
            <dl>
              <dt>Severity</dt><dd><span [class]="'sev sev--' + d.severity.toLowerCase()">{{ d.severity }}</span></dd>
              <dt>Status</dt><dd>{{ formatLabel(d.status) }}</dd>
              <dt>Category</dt><dd>{{ formatLabel(d.category) }}</dd>
              <dt>Department</dt><dd>{{ formatLabel(d.department) }}</dd>
              <dt>Reporter</dt><dd>{{ d.reporterName || 'Anonymous' }}</dd>
              <dt>Date</dt><dd>{{ d.incidentDateTime | date: 'medium' }}</dd>
              <dt>RCA Required</dt><dd>{{ d.rcaRequired ? 'Yes' : 'No' }}</dd>
            </dl>
            <p class="desc">{{ d.description }}</p>
          </section>

          @if (d.timeline.length) {
            <section>
              <h3>Timeline</h3>
              @for (t of d.timeline; track t.id) {
                <div class="timeline-item">
                  <span class="timeline-type">{{ t.eventType }}</span>
                  <span>{{ t.description }}</span>
                  <small>{{ t.occurredAt | date: 'short' }}</small>
                </div>
              }
            </section>
          }

          @if (d.relatedDocumentation.length) {
            <section>
              <h3>Related Documentation</h3>
              @for (doc of d.relatedDocumentation; track doc.documentId) {
                <div class="doc-link">
                  <strong>{{ doc.documentNumber }}</strong> — {{ doc.title }}
                  <small>{{ doc.documentType }} · v{{ doc.currentVersion || '—' }} · {{ doc.acknowledgementPercent }}% ack</small>
                  <a class="btn-link" routerLink="/app/documentation/register">Open Documentation Studio</a>
                </div>
              }
            </section>
          }

          <section class="actions">
            <h3>Workflow Actions</h3>
            <p class="hint">Human approval required — incidents never auto-promote.</p>
            @if (!d.findingId) {
              <button type="button" class="btn-secondary" (click)="promoteFinding.emit(d.id)">Promote to Finding</button>
            } @else {
              <span class="linked">Finding linked</span>
            }
            @if (d.findingId && !d.deficiencyId) {
              <button type="button" class="btn-secondary" (click)="promoteDeficiency.emit(d.id)">Promote to Deficiency</button>
            }
            @if (d.status === 'Closed' && !d.findingId) {
              <button type="button" class="btn-primary" (click)="createLesson.emit(d.id)">Create Lesson Learned</button>
            }
            <a class="btn-link" [routerLink]="['/app/patient-safety/investigations']" [queryParams]="{ incidentId: d.id }">Open Investigation</a>
            <a class="btn-link" [routerLink]="['/app/patient-safety/rca']" [queryParams]="{ incidentId: d.id }">Open RCA</a>
          </section>
        </div>
      </aside>
    }
  `,
})
export class IncidentDetailPanelComponent {
  readonly detail = input<IncidentDetail | null>(null);
  readonly close = output<void>();
  readonly promoteFinding = output<string>();
  readonly promoteDeficiency = output<string>();
  readonly createLesson = output<string>();
  readonly formatLabel = formatIncidentLabel;
}
