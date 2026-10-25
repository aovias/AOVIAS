import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { GraphEntityPanelsComponent } from '../components/graph-entity-panels.component';
import type { GraphDependencies, GraphEntitySummary, GraphImpact, GraphRelationships, GraphTimeline, GraphTraceability } from '../models/compliance-graph.models';
import { GraphApiService } from '../services/graph-api.service';

@Component({
  selector: 'app-entity-explorer-page',
  standalone: true,
  imports: [CommonModule, GraphEntityPanelsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Entity Explorer</h1>
          <p class="subtitle">Universal traceability, timeline, relationships, impact, and dependencies</p>
        </div>
      </header>
      @if (loading()) { <p class="muted">Loading entity graph projection…</p> }
      @else {
        <app-graph-entity-panels
          [entity]="entity()"
          [traceability]="traceability()"
          [timeline]="timeline()"
          [relationships]="relationships()"
          [impact]="impact()"
          [dependencies]="dependencies()"
        />
      }
    </div>
  `,
})
export class EntityExplorerPageComponent implements OnInit {
  private readonly api = inject(GraphApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly aiContext = inject(AiPageContextService);

  readonly loading = signal(true);
  readonly entity = signal<GraphEntitySummary | null>(null);
  readonly traceability = signal<GraphTraceability | null>(null);
  readonly timeline = signal<GraphTimeline | null>(null);
  readonly relationships = signal<GraphRelationships | null>(null);
  readonly impact = signal<GraphImpact | null>(null);
  readonly dependencies = signal<GraphDependencies | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', entityType: 'GraphEntity', entityId: id });
    forkJoin({
      entity: this.api.getEntity(id),
      traceability: this.api.getTraceability(id),
      timeline: this.api.getTimeline(id),
      relationships: this.api.getRelationships(id),
      impact: this.api.getImpact(id),
      dependencies: this.api.getDependencies(id),
    }).subscribe({
      next: (data) => {
        this.entity.set(data.entity);
        this.traceability.set(data.traceability);
        this.timeline.set(data.timeline);
        this.relationships.set(data.relationships);
        this.impact.set(data.impact);
        this.dependencies.set(data.dependencies);
        this.loading.set(false);
        this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', entityType: data.entity.entityType, entityId: id, label: data.entity.title });
      },
      error: () => this.loading.set(false),
    });
  }
}
