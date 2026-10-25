import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { GraphApiService } from '../services/graph-api.service';
import type { GraphExecutiveImpact } from '../models/compliance-graph.models';
import { graphEntityQuery, graphEntityRoute } from '../models/compliance-graph.models';

@Component({
  selector: 'app-executive-impact-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div><h1>Executive Impact View</h1><p class="subtitle">Read-only cross-domain impact intelligence</p></div>
      </header>
      @if (exec(); as e) {
        <div class="kpi-grid">
          @for (risk of e.topConnectedRisks; track risk.entityId) {
            <div class="kpi-card"><strong>{{ risk.title }}</strong><span class="muted">{{ risk.risk }}</span>
              <a [routerLink]="['/app/compliance-graph/entity', risk.entityId]">Explore →</a></div>
          }
        </div>
        <section class="panel"><h2>Most Referenced Documents</h2>
          @for (doc of e.mostReferencedDocuments; track doc.entityId) {
            <a class="doc-link" [routerLink]="routePath(doc.route)" [queryParams]="routeQuery(doc.route)">{{ doc.title }}</a>
          }
        </section>
        <section class="panel"><h2>Compliance Bottlenecks</h2>
          @for (b of e.complianceBottlenecks; track b) { <div class="timeline-item">{{ b }}</div> }
        </section>
      }
    </div>
  `,
})
export class ExecutiveImpactPageComponent implements OnInit {
  private readonly api = inject(GraphApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly exec = signal<GraphExecutiveImpact | null>(null);
  readonly routePath = graphEntityRoute;
  readonly routeQuery = graphEntityQuery;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', label: 'Executive Impact' });
    this.api.getExecutive().subscribe((e) => this.exec.set(e));
  }
}
