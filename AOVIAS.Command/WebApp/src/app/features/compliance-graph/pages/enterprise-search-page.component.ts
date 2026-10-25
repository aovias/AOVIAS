import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { GraphApiService } from '../services/graph-api.service';
import type { GraphSearch } from '../models/compliance-graph.models';
import { graphEntityQuery, graphEntityRoute } from '../models/compliance-graph.models';

@Component({
  selector: 'app-enterprise-search-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Enterprise Search</h1>
          <p class="subtitle">Search across documentation, findings, deficiencies, CAPA, incidents, and clinical indicators</p>
        </div>
      </header>
      <input type="search" class="search-input" placeholder="Search compliance assets…" [value]="query()" (input)="onSearch($any($event.target).value)" />
      @if (results(); as r) {
        <p class="muted">{{ r.totalResults }} result(s) for "{{ r.query }}"</p>
        @for (group of r.groups; track group.entityType) {
          <section class="panel">
            <h2>{{ group.label }}</h2>
            @for (item of group.items; track item.entityId) {
              <div class="doc-link">
                <a [routerLink]="routePath(item.route)" [queryParams]="routeQuery(item.route)">{{ item.title }}</a>
                <small class="muted">{{ item.status }}</small>
                <a [routerLink]="['/app/compliance-graph/entity', item.entityId]" class="btn-link">Traceability →</a>
              </div>
            }
          </section>
        }
      }
    </div>
  `,
})
export class EnterpriseSearchPageComponent implements OnInit {
  private readonly api = inject(GraphApiService);
  private readonly aiContext = inject(AiPageContextService);
  private readonly search$ = new Subject<string>();

  readonly query = signal('');
  readonly results = signal<GraphSearch | null>(null);
  readonly routePath = graphEntityRoute;
  readonly routeQuery = graphEntityQuery;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', label: 'Enterprise Search' });
    this.search$.pipe(debounceTime(300), distinctUntilChanged(), switchMap((q) => this.api.search(q))).subscribe((r) => this.results.set(r));
  }

  onSearch(value: string): void {
    this.query.set(value);
    if (value.trim().length >= 2) this.search$.next(value.trim());
  }
}
