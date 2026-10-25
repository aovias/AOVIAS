import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { DocumentationApiService } from '../services/documentation-api.service';
import { IntelligenceApiService } from '../services/intelligence-api.service';
import {
  DOCUMENT_STATUS_LABELS,
  DOCUMENT_TYPE_LABELS,
  type ControlledDocumentStatus,
  type ControlledDocumentType,
} from '../models/documentation.enums';
import type { DocumentationDashboard } from '../models/documentation.model';

@Component({
  selector: 'app-documentation-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Documentation</h1>
          <p class="subtitle">Quality Documentation &amp; Policy Management — controlled lifecycle governance</p>
        </div>
        <div>
          <a routerLink="/app/documentation/register" class="btn-primary">Controlled Document Register</a>
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading documentation dashboard…</p>
      } @else if (dashboard(); as d) {
        <div class="kpi-grid">
          <div class="kpi"><span class="kpi__v">{{ d.totalDocuments }}</span><span class="kpi__l">Total Documents</span></div>
          <div class="kpi kpi--ready"><span class="kpi__v">{{ d.published }}</span><span class="kpi__l">Published</span></div>
          <div class="kpi"><span class="kpi__v">{{ d.draft }}</span><span class="kpi__l">Draft</span></div>
          <div class="kpi kpi--warn"><span class="kpi__v">{{ d.underReview }}</span><span class="kpi__l">Under Review</span></div>
          <div class="kpi kpi--critical"><span class="kpi__v">{{ d.expired }}</span><span class="kpi__l">Expired</span></div>
          <div class="kpi kpi--warn"><span class="kpi__v">{{ d.dueForReview }}</span><span class="kpi__l">Due For Review</span></div>
          <div class="kpi"><span class="kpi__v">{{ d.pendingApprovals }}</span><span class="kpi__l">Pending Approvals</span></div>
          <div class="kpi"><span class="kpi__v">{{ d.pendingAcknowledgements }}</span><span class="kpi__l">Pending Acknowledgements</span></div>
          <div class="kpi"><span class="kpi__v">{{ d.departmentCoverage }}</span><span class="kpi__l">Department Coverage</span></div>
          <div class="kpi kpi--ready"><span class="kpi__v">{{ d.oeCoverage }}</span><span class="kpi__l">OE Coverage</span></div>
        </div>

        @if (health(); as h) {
          <section class="panel">
            <h2>Documentation Health (Compliance Graph)</h2>
            <div class="kpi-grid">
              <div class="kpi kpi--ready"><span class="kpi__v">{{ h.healthyDocuments }}</span><span class="kpi__l">Healthy Documents</span></div>
              <div class="kpi kpi--warn"><span class="kpi__v">{{ h.needsReview }}</span><span class="kpi__l">Needs Review</span></div>
              <div class="kpi kpi--warn"><span class="kpi__v">{{ h.weakMapping }}</span><span class="kpi__l">Weak Mapping</span></div>
              <div class="kpi"><span class="kpi__v">{{ h.aiReviewPending }}</span><span class="kpi__l">AI Review Pending</span></div>
              <div class="kpi"><span class="kpi__v">{{ h.highImpactDocuments }}</span><span class="kpi__l">High Impact</span></div>
              <div class="kpi"><span class="kpi__v">{{ h.recentlyModified }}</span><span class="kpi__l">Recently Modified</span></div>
              <div class="kpi kpi--ready"><span class="kpi__v">{{ h.affectingAccreditation }}</span><span class="kpi__l">Affecting Accreditation</span></div>
            </div>
            @for (insight of h.insights; track insight.title) {
              <p class="muted insight">{{ insight.title }} — {{ insight.description }}</p>
            }
            <div class="header-actions" style="margin-top: 12px">
              <a routerLink="/app/documentation/explorer" class="btn-secondary">Document Explorer</a>
              <a routerLink="/app/documentation/graph" class="btn-secondary">Compliance Graph</a>
            </div>
          </section>
        }

        <section class="panel">
          <h2>Recent Controlled Documents</h2>
          @if (!d.recentDocuments.length) {
            <p class="muted">No controlled documents yet. Create from a template or start a new draft.</p>
          } @else {
            <table class="data-table">
              <thead>
                <tr>
                  <th>Document Number</th>
                  <th>Title</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Version</th>
                  <th>Department</th>
                </tr>
              </thead>
              <tbody>
                @for (doc of d.recentDocuments; track doc.id) {
                  <tr>
                    <td><strong>{{ doc.documentNumber }}</strong></td>
                    <td>{{ doc.title }}</td>
                    <td>{{ typeLabel(doc.documentType) }}</td>
                    <td><span class="badge" [class.badge--effective]="doc.status === 'effective'">{{ statusLabel(doc.status) }}</span></td>
                    <td>{{ doc.currentVersion ?? '—' }}</td>
                    <td>{{ doc.department ?? '—' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </section>
      }
    </div>
  `,
})
export class DocumentationDashboardPageComponent implements OnInit {
  private readonly api = inject(DocumentationApiService);
  private readonly intelligenceApi = inject(IntelligenceApiService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly dashboard = signal<DocumentationDashboard | null>(null);
  readonly health = signal<import('../models/intelligence.models').DocumentHealthDashboard | null>(null);

  ngOnInit(): void {
    forkJoin({
      dashboard: this.api.getDashboard().pipe(catchError(() => of(null))),
      health: this.intelligenceApi.getHealthDashboard().pipe(catchError(() => of(null))),
    }).pipe(
      finalize(() => this.loading.set(false)),
    ).subscribe(({ dashboard, health }) => {
      if (!dashboard) this.error.set('Unable to load documentation dashboard.');
      this.dashboard.set(dashboard);
      this.health.set(health);
    });
  }

  typeLabel(t: ControlledDocumentType): string {
    return DOCUMENT_TYPE_LABELS[t] ?? t;
  }

  statusLabel(s: ControlledDocumentStatus): string {
    return DOCUMENT_STATUS_LABELS[s] ?? s;
  }
}
