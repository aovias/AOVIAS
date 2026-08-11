import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize as rxFinalize, of, type Observable } from 'rxjs';
import { IntelligenceApiService } from '../services/intelligence-api.service';
import type { ComplianceGraph, ComplianceGraphNode, DocumentRelationships } from '../models/intelligence.models';

const NODE_COLORS: Record<string, string> = {
  document: '#2f6bff',
  objectiveElement: '#2f6bff',
  framework: '#0891b2',
  chapter: '#0d9488',
  standard: '#059669',
  capa: '#dc2626',
  policy: '#2f6bff',
  finding: '#ea580c',
  evidence: '#ca8a04',
};

@Component({
  selector: 'app-document-graph-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace graph-page">
      <header class="page-header">
        <div>
          <h1>Compliance Relationship Graph</h1>
          <p class="subtitle">
            @if (documentTitle()) {
              Read-only graph for <strong>{{ documentTitle() }}</strong>
            } @else {
              Organization-wide documentation compliance graph (read-only)
            }
          </p>
        </div>
        <div class="header-actions">
          <input
            type="search"
            class="search-input"
            placeholder="Filter nodes…"
            [value]="filter()"
            (input)="filter.set($any($event.target).value)"
          />
          @if (documentId()) {
            <a [routerLink]="['/app/documentation/authoring', documentId()]" class="btn-secondary">Open in Studio</a>
          } @else {
            <a routerLink="/app/documentation/explorer" class="btn-secondary">Document Explorer</a>
          }
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      } @else if (loading()) {
        <p class="muted">Loading relationship graph…</p>
      } @else if (graph(); as g) {
        <div class="graph-stats">
          <span><strong>{{ g.totalDocuments }}</strong> documents</span>
          <span><strong>{{ g.totalRelationships }}</strong> relationships</span>
          <span><strong>{{ filteredNodes().length }}</strong> nodes visible</span>
        </div>

        @if (g.totalRelationships === 0) {
          <div class="graph-empty banner">
            <p>
              No compliance relationships yet. Map documents to Objective Elements in Authoring Studio,
              or add <code>@</code> cross-references to link policies, evidence, findings, and CAPAs.
            </p>
            <a routerLink="/app/documentation/authoring" class="btn-primary">Open Authoring Studio</a>
          </div>
        }

        <div class="graph-canvas" [style.height.px]="canvasHeight()">
          <svg class="graph-edges" [attr.width]="canvasWidth()" [attr.height]="canvasHeight()">
            @for (edge of visibleEdges(); track edge.sourceId + edge.targetId + edge.relationshipType) {
              @if (nodePos(edge.sourceId); as s) {
                @if (nodePos(edge.targetId); as t) {
                  <line
                    [attr.x1]="s.x"
                    [attr.y1]="s.y"
                    [attr.x2]="t.x"
                    [attr.y2]="t.y"
                    class="graph-edge"
                    [class.graph-edge--bidirectional]="edge.bidirectional"
                  />
                }
              }
            }
          </svg>

          @for (node of filteredNodes(); track node.id) {
            @if (nodePos(node.id); as pos) {
              <div
                class="graph-node"
                [class.graph-node--expanded]="expanded().has(node.id)"
                [ngClass]="'graph-node--' + node.nodeType"
                [style.left.px]="pos.x - nodeWidth(node) / 2"
                [style.top.px]="pos.y - 36"
                [style.border-left-color]="nodeColor(node.nodeType)"
                (click)="toggleNode(node)"
              >
                <span class="graph-node__type">{{ formatNodeType(node.nodeType) }}</span>
                <span class="graph-node__label" [title]="node.label">{{ node.label }}</span>
                @if (node.connectionCount > 0) {
                  <span class="graph-node__meta">{{ node.connectionCount }} link(s)</span>
                }
                @if (node.route) {
                  <a [routerLink]="nodeRoute(node.route)" class="graph-node__link" (click)="$event.stopPropagation()">Open →</a>
                }
              </div>
            }
          }
        </div>

        <section class="panel graph-legend">
          <h2>Legend</h2>
          <div class="legend">
            @for (type of nodeTypes(); track type) {
              <span class="legend-item">
                <span class="legend-swatch" [style.background]="nodeColor(type)"></span>
                {{ formatNodeType(type) }}
              </span>
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: [`
    .graph-page .header-actions { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .graph-page .search-input {
      padding: 8px 12px; border-radius: 6px;
      border: 1px solid #475569; background: #1e293b; color: #f1f5f9;
      min-width: 220px;
    }
    .graph-page .search-input::placeholder { color: #94a3b8; }
    .graph-stats {
      display: flex; gap: 20px; font-size: 13px; color: #cbd5e1; margin-bottom: 12px;
    }
    .graph-stats strong { color: #f8fafc; }
    .graph-empty {
      background: #1e293b; border: 1px solid #334155; border-radius: 8px;
      padding: 14px 16px; margin-bottom: 12px; color: #e2e8f0;
    }
    .graph-empty p { margin: 0 0 10px; font-size: 13px; line-height: 1.5; }
    .graph-empty code { background: #0f172a; padding: 1px 5px; border-radius: 4px; }
    .graph-canvas {
      position: relative; border: 1px solid #cbd5e1; border-radius: 8px;
      background: #f1f5f9; overflow: hidden; min-height: 480px;
      color: #0f172a;
    }
    .graph-edges { position: absolute; inset: 0; pointer-events: none; }
    .graph-edge { stroke: #64748b; stroke-width: 2; }
    .graph-edge--bidirectional { stroke: var(--color-primary); stroke-dasharray: 6 3; }
    .graph-node {
      position: absolute; width: 168px; padding: 10px 12px; border-radius: 8px;
      border: 1px solid #cbd5e1; border-left-width: 4px; background: #ffffff;
      cursor: pointer; font-size: 12px; box-shadow: 0 2px 8px rgba(15, 23, 42, 0.12);
      z-index: 1; color: #0f172a;
    }
    .graph-node--expanded {
      border-color: var(--color-primary); box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary) 25%, transparent);
    }
    .graph-node__type {
      display: block; font-size: 10px; text-transform: uppercase;
      letter-spacing: 0.04em; color: #64748b; font-weight: 600;
    }
    .graph-node__label {
      display: block; font-weight: 700; line-height: 1.35; margin: 4px 0;
      color: #0f172a; overflow: hidden; text-overflow: ellipsis;
      display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;
      white-space: normal;
    }
    .graph-node__meta { display: block; font-size: 10px; color: #64748b; }
    .graph-node__link {
      display: inline-block; margin-top: 6px; font-size: 11px;
      font-weight: 600; color: var(--color-primary); text-decoration: none;
    }
    .graph-node__link:hover { text-decoration: underline; }
    .graph-legend h2 { color: inherit; }
    .legend { display: flex; flex-wrap: wrap; gap: 10px; }
    .legend-item {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 5px 12px; border-radius: 999px; font-size: 12px;
      border: 1px solid #475569; background: #1e293b; color: #e2e8f0;
    }
    .legend-swatch {
      width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0;
    }
  `],
})
export class DocumentGraphPageComponent implements OnInit {
  private readonly api = inject(IntelligenceApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly graph = signal<ComplianceGraph | null>(null);
  readonly documentTitle = signal<string | null>(null);
  readonly documentId = signal<string | null>(null);
  readonly filter = signal('');
  readonly expanded = signal(new Set<string>());

  readonly canvasWidth = signal(960);
  readonly canvasHeight = signal(520);

  readonly filteredNodes = computed(() => {
    const g = this.graph();
    const q = this.filter().trim().toLowerCase();
    if (!g) return [] as ComplianceGraphNode[];
    if (!q) return g.nodes;
    return g.nodes.filter(
      (n) => n.label.toLowerCase().includes(q) || n.nodeType.toLowerCase().includes(q),
    );
  });

  readonly visibleEdges = computed(() => {
    const g = this.graph();
    const ids = new Set(this.filteredNodes().map((n) => n.id));
    if (!g) return [];
    return g.edges.filter((e) => ids.has(e.sourceId) && ids.has(e.targetId));
  });

  readonly nodeTypes = computed(() => {
    const g = this.graph();
    if (!g) return [] as string[];
    return [...new Set(g.nodes.map((n) => n.nodeType))];
  });

  private positions = new Map<string, { x: number; y: number }>();

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.documentId.set(id);

    let req$: Observable<ComplianceGraph | DocumentRelationships | null>;
    if (id) {
      req$ = this.api.getRelationships(id).pipe(
        catchError(() => {
          this.error.set('Unable to load document relationships.');
          return of(null);
        }),
      );
    } else {
      req$ = this.api.getGraph().pipe(
        catchError(() => {
          this.error.set('Unable to load compliance graph.');
          return of(null);
        }),
      );
    }

    req$.pipe(rxFinalize(() => this.loading.set(false))).subscribe((res) => {
      if (!res) return;
      if ('graph' in res) {
        this.graph.set(res.graph);
        this.documentTitle.set(res.title);
      } else {
        this.graph.set(res);
      }
      this.layoutNodes(this.graph()?.nodes ?? [], this.graph()?.edges ?? []);
    });
  }

  nodePos(id: string): { x: number; y: number } | null {
    return this.positions.get(id) ?? null;
  }

  nodeWidth(node: ComplianceGraphNode): number {
    return node.nodeType === 'document' ? 180 : 168;
  }

  nodeColor(nodeType: string): string {
    return NODE_COLORS[nodeType] ?? '#64748b';
  }

  formatNodeType(nodeType: string): string {
    return nodeType.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase());
  }

  toggleNode(node: ComplianceGraphNode): void {
    this.expanded.update((set) => {
      const next = new Set(set);
      if (next.has(node.id)) next.delete(node.id);
      else next.add(node.id);
      return next;
    });
  }

  nodeRoute(route: string): string[] {
    return route.startsWith('/') ? route.split('/').filter(Boolean) : [route];
  }

  private layoutNodes(
    nodes: ComplianceGraphNode[],
    edges: { sourceId: string; targetId: string }[],
  ): void {
    this.positions.clear();
    const w = this.canvasWidth();
    const h = this.canvasHeight();
    const cx = w / 2;
    const cy = h / 2;

    if (nodes.length === 0) return;

    if (nodes.length === 1) {
      this.positions.set(nodes[0].id, { x: cx, y: cy });
      return;
    }

    const docNodes = nodes.filter((n) => n.nodeType === 'document');
    const otherNodes = nodes.filter((n) => n.nodeType !== 'document');
    const radius = Math.min(w, h) * 0.32;

    docNodes.forEach((node, i) => {
      const angle = (2 * Math.PI * i) / Math.max(docNodes.length, 1);
      this.positions.set(node.id, {
        x: cx + radius * Math.cos(angle),
        y: cy + radius * Math.sin(angle),
      });
    });

    otherNodes.forEach((node) => {
      const linkedEdge = edges.find((e) => e.targetId === node.id || e.sourceId === node.id);
      const anchorId = linkedEdge
        ? (linkedEdge.sourceId === node.id ? linkedEdge.targetId : linkedEdge.sourceId)
        : null;
      const anchor = anchorId ? this.positions.get(anchorId) : null;

      if (anchor) {
        const jitter = (this.hashId(node.id) % 80) - 40;
        this.positions.set(node.id, {
          x: Math.min(w - 100, Math.max(100, anchor.x + jitter)),
          y: Math.min(h - 60, Math.max(60, anchor.y - 90)),
        });
      } else {
        const i = otherNodes.indexOf(node);
        const angle = (2 * Math.PI * i) / Math.max(otherNodes.length, 1);
        this.positions.set(node.id, {
          x: cx + (radius + 80) * Math.cos(angle),
          y: cy + (radius + 80) * Math.sin(angle),
        });
      }
    });
  }

  private hashId(id: string): number {
    return id.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0);
  }
}
