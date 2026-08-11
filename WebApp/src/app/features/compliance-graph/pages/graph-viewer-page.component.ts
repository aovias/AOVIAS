import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { GraphApiService } from '../services/graph-api.service';
import type { ComplianceGraph, ComplianceGraphNode } from '../../documentation/models/intelligence.models';

const NODE_COLORS: Record<string, string> = {
  document: '#2f6bff', ControlledDocument: '#2f6bff', objectiveElement: '#2f6bff', ObjectiveElement: '#2f6bff',
  Finding: '#ea580c', Deficiency: '#dc2626', CAPA: '#b91c1c', Incident: '#f97316', ClinicalIndicator: '#0891b2',
  Evidence: '#ca8a04', Task: '#2f6bff',
};

@Component({
  selector: 'app-graph-viewer-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="ps-page graph-page">
      <header class="page-header">
        <div>
          <h1>Enterprise Compliance Graph</h1>
          <p class="subtitle">Read-only relationship graph across all PRAMAN bounded contexts</p>
        </div>
        <div class="header-actions">
          <input type="search" class="search-input" placeholder="Filter nodes…" [value]="filter()" (input)="filter.set($any($event.target).value)" />
          <a routerLink="/app/compliance-graph/search" class="btn-secondary">Enterprise Search</a>
        </div>
      </header>

      @if (error()) { <div class="banner error">{{ error() }}</div> }
      @else if (loading()) { <p class="muted">Loading compliance graph…</p> }
      @else if (graph(); as g) {
        <div class="graph-stats">
          <span><strong>{{ g.nodes.length }}</strong> nodes</span>
          <span><strong>{{ g.totalRelationships }}</strong> relationships</span>
        </div>
        <div class="graph-canvas" [style.height.px]="520">
          <svg class="graph-edges" width="960" height="520">
            @for (edge of visibleEdges(); track edge.sourceId + edge.targetId) {
              @if (nodePos(edge.sourceId); as s) {
                @if (nodePos(edge.targetId); as t) {
                  <line [attr.x1]="s.x" [attr.y1]="s.y" [attr.x2]="t.x" [attr.y2]="t.y" class="graph-edge" />
                }
              }
            }
          </svg>
          @for (node of filteredNodes(); track node.id) {
            @if (nodePos(node.id); as pos) {
              <div class="graph-node" [style.left.px]="pos.x - 84" [style.top.px]="pos.y - 36" [style.border-left-color]="nodeColor(node.nodeType)" (click)="selectNode(node)">
                <span class="graph-node__type">{{ formatNodeType(node.nodeType) }}</span>
                <span class="graph-node__label">{{ node.label }}</span>
                @if (node.route) {
                  <a [routerLink]="nodeRoute(node.route)" class="graph-node__link" (click)="$event.stopPropagation()">Open →</a>
                }
              </div>
            }
          }
        </div>
        @if (selectedId()) {
          <a [routerLink]="['/app/compliance-graph/entity', selectedId()]" class="btn-primary">Explore entity traceability →</a>
        }
      }
    </div>
  `,
  styles: [`
    .graph-page .header-actions { display: flex; gap: 8px; flex-wrap: wrap; align-items: center; }
    .graph-page .search-input { padding: 8px 12px; border-radius: 6px; border: 1px solid var(--color-border-subtle, #cbd5e1); min-width: 220px; }
    .graph-stats { display: flex; gap: 20px; margin-bottom: 12px; font-size: 13px; }
    .graph-canvas { position: relative; border: 1px solid var(--color-border-subtle, #cbd5e1); border-radius: 8px; background: var(--color-bg-surface, #f8fafc); overflow: hidden; }
    .graph-edges { position: absolute; inset: 0; pointer-events: none; }
    .graph-edge { stroke: #64748b; stroke-width: 2; }
    .graph-node { position: absolute; width: 168px; padding: 10px 12px; border-radius: 8px; border: 1px solid #cbd5e1; border-left-width: 4px; background: #fff; cursor: pointer; font-size: 12px; z-index: 1; }
    .graph-node__type { display: block; font-size: 10px; text-transform: uppercase; color: #64748b; font-weight: 600; }
    .graph-node__label { display: block; font-weight: 700; margin: 4px 0; line-height: 1.3; }
    .graph-node__link { font-size: 11px; color: var(--color-primary); text-decoration: none; }
  `],
})
export class GraphViewerPageComponent implements OnInit {
  private readonly api = inject(GraphApiService);
  private readonly aiContext = inject(AiPageContextService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly graph = signal<ComplianceGraph | null>(null);
  readonly filter = signal('');
  readonly selectedId = signal<string | null>(null);
  private positions = new Map<string, { x: number; y: number }>();

  readonly filteredNodes = computed(() => {
    const g = this.graph();
    const q = this.filter().trim().toLowerCase();
    if (!g) return [] as ComplianceGraphNode[];
    if (!q) return g.nodes;
    return g.nodes.filter((n) => n.label.toLowerCase().includes(q) || n.nodeType.toLowerCase().includes(q));
  });

  readonly visibleEdges = computed(() => {
    const g = this.graph();
    const ids = new Set(this.filteredNodes().map((n) => n.id));
    if (!g) return [];
    return g.edges.filter((e) => ids.has(e.sourceId) && ids.has(e.targetId));
  });

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', label: 'Graph Viewer' });
    const centerId = this.route.snapshot.queryParamMap.get('centerId') ?? undefined;
    this.api.getVisualGraph(centerId).pipe(
      catchError(() => { this.error.set('Unable to load enterprise compliance graph.'); return of(null); }),
      finalize(() => this.loading.set(false)),
    ).subscribe((g) => {
      if (!g) return;
      this.graph.set(g);
      this.layoutNodes(g.nodes, g.edges);
    });
  }

  nodePos(id: string) { return this.positions.get(id) ?? null; }
  nodeColor(type: string) { return NODE_COLORS[type] ?? '#64748b'; }
  formatNodeType(t: string) { return t.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase()); }
  nodeRoute(route: string): string {
    return route.split('?')[0];
  }

  selectNode(node: ComplianceGraphNode): void {
    const id = node.id.includes(':') ? node.id.split(':')[1] : null;
    this.selectedId.set(id);
  }

  private layoutNodes(nodes: ComplianceGraphNode[], edges: { sourceId: string; targetId: string }[]): void {
    this.positions.clear();
    const cx = 480; const cy = 260; const radius = 180;
    nodes.forEach((node, i) => {
      const angle = (2 * Math.PI * i) / Math.max(nodes.length, 1);
      this.positions.set(node.id, { x: cx + radius * Math.cos(angle), y: cy + radius * Math.sin(angle) });
    });
  }
}
