import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of } from 'rxjs';
import { DocumentationApiService } from '../services/documentation-api.service';
import type { DocumentationAnalytics } from '../models/documentation.model';

@Component({
  selector: 'app-documentation-analytics-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Documentation Analytics</h1>
          <p class="subtitle">Operational documentation KPIs — complements Analytics Hub SSOT</p>
        </div>
      </header>

      @if (error()) { <div class="banner error">{{ error() }}</div> }
      @if (loading()) { <p class="muted">Loading analytics…</p> }
      @else if (analytics(); as a) {
        <div class="kpi-grid">
          <div class="kpi kpi--warn"><span class="kpi__v">{{ a.policiesDueReview }}</span><span class="kpi__l">Due Review</span></div>
          <div class="kpi kpi--critical"><span class="kpi__v">{{ a.expiredDocuments }}</span><span class="kpi__l">Expired</span></div>
          <div class="kpi kpi--ready"><span class="kpi__v">{{ a.acknowledgementPercent | number: '1.0-1' }}%</span><span class="kpi__l">Acknowledgement %</span></div>
          <div class="kpi"><span class="kpi__v">{{ a.departmentComplianceCount }}</span><span class="kpi__l">Departments</span></div>
          <div class="kpi"><span class="kpi__v">{{ a.unmappedDocuments }}</span><span class="kpi__l">Unmapped</span></div>
          <div class="kpi"><span class="kpi__v">{{ a.reviewCyclePerformancePercent | number: '1.0-1' }}%</span><span class="kpi__l">Review Cycle</span></div>
          <div class="kpi"><span class="kpi__v">{{ a.approvalTurnaroundDays | number: '1.1-1' }}</span><span class="kpi__l">Approval Days</span></div>
          <div class="kpi kpi--ready"><span class="kpi__v">{{ a.frameworkCoveragePercent | number: '1.0-1' }}%</span><span class="kpi__l">Framework Coverage</span></div>
        </div>

        <section class="panel">
          <h2>Most Revised SOPs</h2>
          @if (!a.mostRevisedSops.length) {
            <p class="muted">No revision history yet.</p>
          } @else {
            <table class="data-table">
              <thead><tr><th>Document</th><th>Title</th><th>Revisions</th></tr></thead>
              <tbody>
                @for (s of a.mostRevisedSops; track s.id) {
                  <tr>
                    <td><strong>{{ s.documentNumber }}</strong></td>
                    <td>{{ s.title }}</td>
                    <td>{{ s.revisionCount }}</td>
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
export class DocumentationAnalyticsPageComponent implements OnInit {
  private readonly api = inject(DocumentationApiService);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly analytics = signal<DocumentationAnalytics | null>(null);

  ngOnInit(): void {
    this.api.getAnalytics().pipe(
      catchError(() => { this.error.set('Unable to load documentation analytics.'); return of(null); }),
      finalize(() => this.loading.set(false)),
    ).subscribe((a) => this.analytics.set(a));
  }
}
