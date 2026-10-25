import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import type {
  GraphDependencies,
  GraphEntitySummary,
  GraphImpact,
  GraphRelationships,
  GraphTimeline,
  GraphTraceability,
} from '../models/compliance-graph.models';
import { formatGraphLabel, graphEntityQuery, graphEntityRoute } from '../models/compliance-graph.models';

@Component({
  selector: 'app-graph-entity-panels',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="graph-panels">
      @if (entity(); as e) {
        <section class="panel entity-summary-card">
          <h2>{{ e.title }}</h2>
          <p class="muted">{{ formatLabel(e.entityType) }} · {{ e.status }}</p>
          @if (e.route) {
            <a class="btn-secondary" [routerLink]="routePath(e.route)" [queryParams]="routeQuery(e.route)">Open workspace →</a>
          }
        </section>
      }

      <nav class="graph-tabs" aria-label="Graph panels">
        @for (tab of tabs; track tab.id) {
          <button type="button" class="graph-tabs__btn" [class.graph-tabs__btn--active]="activeTab() === tab.id" (click)="activeTab.set(tab.id)">{{ tab.label }}</button>
        }
      </nav>

      @switch (activeTab()) {
        @case ('traceability') {
          @if (traceability(); as t) {
            <section class="panel"><h3>Traceability Chain</h3><p class="muted">{{ t.summary }}</p>
              @for (step of t.chain; track step.order) {
                <div class="timeline-item">
                  <span class="timeline-type">{{ step.layer }}</span>
                  {{ step.label }}
                  @if (step.route) { <a [routerLink]="routePath(step.route)" [queryParams]="routeQuery(step.route)">Navigate</a> }
                </div>
              }
            </section>
          }
        }
        @case ('timeline') {
          @if (timeline(); as tl) {
            <section class="panel"><h3>Universal Timeline</h3>
              @for (entry of tl.entries; track entry.occurredAt + entry.action) {
                <div class="timeline-item">
                  <span class="timeline-type">{{ entry.occurredAt | date: 'medium' }}</span>
                  <strong>{{ entry.label }}</strong>
                  @if (entry.detail) { <small class="muted">{{ entry.detail }}</small> }
                </div>
              }
            </section>
          }
        }
        @case ('relationships') {
          @if (relationships(); as rel) {
            <section class="panel"><h3>Relationships</h3><p class="muted">{{ rel.impactSummary }}</p>
              <h4>Incoming</h4>
              @for (r of rel.incoming; track r.id) {
                <div class="doc-link">{{ r.sourceLabel }} → {{ formatLabel(r.relationshipType) }}</div>
              }
              <h4>Outgoing</h4>
              @for (r of rel.outgoing; track r.id) {
                <div class="doc-link">{{ formatLabel(r.relationshipType) }} → {{ r.targetLabel }}
                  @if (r.targetRoute) { <a [routerLink]="routePath(r.targetRoute)" [queryParams]="routeQuery(r.targetRoute)">Open</a> }
                </div>
              }
            </section>
          }
        }
        @case ('impact') {
          @if (impact(); as imp) {
            <section class="panel"><h3>Impact Explorer</h3><p class="muted">{{ imp.summary }}</p>
              @for (area of imp.affectedAreas; track area.category) {
                <div class="kpi-card"><strong>{{ area.category }}</strong> — {{ area.count }} linked asset(s)</div>
              }
            </section>
          }
        }
        @case ('dependencies') {
          @if (dependencies(); as dep) {
            <section class="panel"><h3>Dependency Explorer</h3>
              @if (dep.warningMessage) { <div class="banner warning">{{ dep.warningMessage }}</div> }
              <p>Archive: {{ dep.canArchive ? 'Allowed' : 'Blocked' }} · Close: {{ dep.canClose ? 'Allowed' : 'Blocked' }}</p>
              @for (item of dep.blocking; track item.category) {
                <div class="doc-link"><strong>{{ item.category }}</strong> ({{ item.count }})</div>
              }
            </section>
          }
        }
      }
    </div>
  `,
  styles: [`
    .graph-tabs { display: flex; gap: 4px; margin: 16px 0; flex-wrap: wrap; }
    .graph-tabs__btn { padding: 8px 14px; border: 1px solid var(--color-border-subtle, #e5e7eb); background: var(--color-bg-surface, #fff); border-radius: 6px; cursor: pointer; font-size: 13px; }
    .graph-tabs__btn--active { border-color: var(--color-brand-primary, var(--color-primary)); color: var(--color-brand-primary, var(--color-primary)); font-weight: 600; }
    .entity-summary-card h2 { margin: 0 0 8px; }
  `],
})
export class GraphEntityPanelsComponent {
  readonly entity = input<GraphEntitySummary | null>(null);
  readonly traceability = input<GraphTraceability | null>(null);
  readonly timeline = input<GraphTimeline | null>(null);
  readonly relationships = input<GraphRelationships | null>(null);
  readonly impact = input<GraphImpact | null>(null);
  readonly dependencies = input<GraphDependencies | null>(null);

  readonly formatLabel = formatGraphLabel;
  readonly routePath = graphEntityRoute;
  readonly routeQuery = graphEntityQuery;

  readonly tabs = [
    { id: 'traceability', label: 'Traceability' },
    { id: 'timeline', label: 'Timeline' },
    { id: 'relationships', label: 'Relationships' },
    { id: 'impact', label: 'Impact' },
    { id: 'dependencies', label: 'Dependencies' },
  ] as const;

  readonly activeTab = signal<string>('traceability');
}
