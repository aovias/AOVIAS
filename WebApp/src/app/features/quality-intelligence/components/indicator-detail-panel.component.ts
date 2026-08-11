import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import type { ClinicalIndicatorDetail } from '../models/quality-indicator.models';
import { formatQualityLabel, thresholdClass } from '../models/quality-indicator.models';

@Component({
  selector: 'app-indicator-detail-panel',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    @if (detail(); as d) {
      <aside class="detail-panel" role="complementary" aria-label="Indicator detail">
        <header class="detail-panel__header">
          <div>
            <h2>{{ d.name }}</h2>
            <p class="muted">{{ d.indicatorCode }} · {{ formatLabel(d.category) }}</p>
          </div>
          <button type="button" class="btn-link" (click)="close.emit()">Close</button>
        </header>
        <div class="detail-panel__body">
          <p class="desc">{{ d.description }}</p>
          <div class="meta-row">
            <span [class]="thresholdClass(d.thresholdStatus)">{{ formatLabel(d.thresholdStatus) }}</span>
            <span class="badge">{{ formatLabel(d.trend) }}</span>
            <span class="muted">Target: {{ d.targetValue ?? '—' }} · Current: {{ d.currentValue ?? '—' }}</span>
          </div>

          <section><h3>Definition</h3>
            <p class="muted">Method: {{ formatLabel(d.calculationMethod) }}</p>
            @if (d.formulaExpression) { <p class="muted">Formula: {{ d.formulaExpression }}</p> }
          </section>

          @if (d.investigationRecommendations.length) {
            <section><h3>Investigation Recommendations (Advisory)</h3>
              @for (rec of d.investigationRecommendations; track rec.title) {
                <div class="doc-link">
                  <strong>{{ rec.title }}</strong>
                  <small>{{ rec.rationale }}</small>
                  @if (rec.deepLink) { <a [routerLink]="rec.deepLink.split('?')[0]" [queryParams]="parseQuery(rec.deepLink)">Open →</a> }
                </div>
              }
            </section>
          }

          @if (d.relatedIncidents.length) {
            <section><h3>Related Incidents</h3>
              @for (link of d.relatedIncidents; track link.id) {
                <a class="doc-link" [routerLink]="routePath(link.deepLink)" [queryParams]="parseQuery(link.deepLink)">{{ link.label }}</a>
              }
            </section>
          }

          @if (d.relatedFindings.length) {
            <section><h3>Related Findings</h3>
              @for (link of d.relatedFindings; track link.id) { <a class="doc-link" [routerLink]="'/app/audit/findings'">{{ link.label }}</a> }
            </section>
          }

          @if (d.trendHistory.length) {
            <section><h3>Trend History</h3>
              @for (pt of d.trendHistory.slice(0, 6); track pt.id) {
                <div class="timeline-item"><span class="timeline-type">{{ pt.periodEnd | date: 'MMM yyyy' }}</span> {{ pt.value }}</div>
              }
            </section>
          }

          @if (d.complianceContext) {
            <section><h3>Compliance Context</h3>
              <p class="muted">{{ d.complianceContext.objectiveElementName }} ({{ d.complianceContext.clauseReference }})</p>
              <p class="linked">Readiness impact: {{ d.complianceContext.readinessImpact }}</p>
            </section>
          }
        </div>
      </aside>
    }
  `,
})
export class IndicatorDetailPanelComponent {
  readonly detail = input<ClinicalIndicatorDetail | null>(null);
  readonly close = output<void>();
  readonly formatLabel = formatQualityLabel;
  readonly thresholdClass = thresholdClass;

  routePath(link?: string): string {
    return link?.split('?')?.[0] ?? '/app/patient-safety/command-center';
  }

  parseQuery(link?: string): Record<string, string> {
    if (!link?.includes('?')) return {};
    const qs = link.split('?')[1];
    const params: Record<string, string> = {};
    qs.split('&').forEach((p) => { const [k, v] = p.split('='); if (k) params[k] = v ?? ''; });
    return params;
  }
}
