import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import type {
  AuditorAssessmentContext,
  AuditorChapterContext,
  AuditorObjectiveElementContext,
  AuditorStandardContext,
  EvidenceTraceability,
} from '../models/auditor-workspace.model';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';

type StatusTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

@Component({
  selector: 'app-auditor-assessment-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: [
    '../styles/auditor-workspace.layout.scss',
    '../styles/auditor-assessment-page.component.scss',
  ],
  template: `
    <div class="auditor-workspace">
      <header class="page-header">
        <div>
          <h1>Assessment Workspace</h1>
          <p class="subtitle">
            Chapters → Standards → Objective Elements. Select an OE to review activity timeline and
            evidences.
          </p>
        </div>
      </header>

      @if (loading()) {
        <p class="loading-block">Loading assessment context…</p>
      } @else if (context(); as ctx) {
        <div class="framework-chip">{{ ctx.frameworkName || 'Framework' }}</div>

        <div class="assessment-layout">
          <section class="panel-card">
            <div class="panel-card__head">
              <h2>Compliance Tree</h2>
              <div class="tree-search" role="search">
                <input
                  type="search"
                  class="tree-search__input"
                  placeholder="Search chapters, standards, OEs…"
                  [value]="treeSearch()"
                  (input)="onTreeSearch($any($event.target).value)"
                  aria-label="Search compliance tree"
                />
                @if (treeSearch()) {
                  <button type="button" class="tree-search__clear" (click)="clearTreeSearch()">
                    Clear
                  </button>
                }
              </div>
            </div>
            <div class="tree-panel">
              @if (filteredChapters().length) {
                @for (ch of filteredChapters(); track ch.id) {
                  <details class="tree-chapter" open>
                    <summary>{{ ch.code }} — {{ ch.name }}</summary>
                    @for (st of ch.standards; track st.id) {
                      <details class="tree-standard" [open]="treeExpanded()">
                        <summary>{{ st.code }} · {{ st.name }}</summary>
                        @for (oe of st.objectiveElements; track oe.id) {
                          <button
                            type="button"
                            class="oe-btn"
                            [class.active]="selectedOe()?.id === oe.id"
                            (click)="selectOe(oe)"
                          >
                            <span>{{ oe.code }}</span>
                            <span
                              class="oe-meta"
                              [class.oe-meta--verified]="oe.verifiedEvidenceCount > 0"
                            >
                              {{ oe.verifiedEvidenceCount }}/{{ oe.evidenceCount }} ev
                            </span>
                          </button>
                        }
                      </details>
                    }
                  </details>
                }
              } @else {
                <p class="tree-empty">
                  No chapters, standards, or objective elements match your search.
                </p>
              }
            </div>
          </section>

          <section class="panel-card detail-panel">
            @if (selectedOe(); as oe) {
              <div class="detail-header">
                <span class="oe-code-badge">{{ oe.code }}</span>
                <h2>Objective Element Review</h2>
                <p class="oe-description">{{ oe.description }}</p>
              </div>

              <div class="kpi-row">
                <div class="kpi-chip">
                  <span class="kpi-chip__v">{{ oe.evidenceCount }}</span>
                  <span class="kpi-chip__l">Evidence</span>
                </div>
                <div class="kpi-chip">
                  <span class="kpi-chip__v">{{ oe.verifiedEvidenceCount }}</span>
                  <span class="kpi-chip__l">Verified</span>
                </div>
                <div class="kpi-chip" [class.kpi-chip--warn]="oe.deficiencyCount > 0">
                  <span class="kpi-chip__v">{{ oe.deficiencyCount }}</span>
                  <span class="kpi-chip__l">Deficiencies</span>
                </div>
                <div class="kpi-chip" [class.kpi-chip--warn]="oe.openCapaCount > 0">
                  <span class="kpi-chip__v">{{ oe.openCapaCount }}</span>
                  <span class="kpi-chip__l">Open CAPA</span>
                </div>
                <div class="kpi-chip">
                  <span class="kpi-chip__v">{{ oe.taskCount }}</span>
                  <span class="kpi-chip__l">Tasks</span>
                </div>
                @if (oe.readinessScore != null) {
                  <div class="kpi-chip kpi-chip--ready">
                    <span class="kpi-chip__v">{{ oe.readinessScore | number: '1.0-1' }}%</span>
                    <span class="kpi-chip__l">Readiness</span>
                  </div>
                }
              </div>

              @if (traceLoading()) {
                <div class="trace-loading">
                  <span class="trace-loading__spinner" aria-hidden="true"></span>
                  Loading activity and evidences…
                </div>
              } @else if (trace(); as t) {
                <div class="trace-section">
                  <h3>Evidences</h3>
                  @if (evidenceEntries().length) {
                    <ul class="evidence-list">
                      @for (entry of evidenceEntries(); track $index) {
                        <li class="evidence-card">
                          <div class="evidence-card__row">
                            <strong>{{ entry.label }}</strong>
                            <span class="status-badge" [class]="statusBadgeClass(entry.status)">
                              {{ entry.status }}
                            </span>
                          </div>
                          @if (entry.detail) {
                            <p class="timeline__detail">{{ entry.detail }}</p>
                          }
                          <div class="evidence-card__meta">
                            @if (entry.timestamp) {
                              <span>{{ entry.timestamp | date: 'medium' }}</span>
                            }
                            @if (entry.entityId) {
                              <a
                                [routerLink]="['/app/evidence']"
                                [queryParams]="{ evidenceId: entry.entityId }"
                              >
                                Open evidence
                              </a>
                            }
                          </div>
                        </li>
                      }
                    </ul>
                  } @else {
                    <p class="muted">No evidences linked to this objective element yet.</p>
                    <a routerLink="/app/evidence" class="inline-link">Browse evidence library</a>
                  }
                </div>

                <div class="trace-section">
                  <h3>Activity Timeline</h3>
                  @if (activityEntries().length) {
                    <ol class="timeline">
                      @for (entry of activityEntries(); track $index) {
                        <li [class]="timelineItemClass(entry.layer)">
                          <span class="timeline__dot" aria-hidden="true"></span>
                          <div class="timeline__card">
                            <span class="timeline__layer">{{ entry.layer }}</span>
                            <div class="timeline__row">
                              <span class="timeline__label">{{ entry.label }}</span>
                              <span class="status-badge" [class]="statusBadgeClass(entry.status)">
                                {{ entry.status }}
                              </span>
                            </div>
                            @if (entry.detail && entry.detail !== oe.description) {
                              <p class="timeline__detail">{{ entry.detail }}</p>
                            }
                            @if (entry.timestamp) {
                              <div class="timeline__time">
                                {{ entry.timestamp | date: 'medium' }}
                              </div>
                            }
                          </div>
                        </li>
                      }
                    </ol>
                  } @else {
                    <p class="muted">No activity recorded for this element.</p>
                  }
                </div>
              }
            } @else {
              <div class="empty-state">
                <div class="empty-state__icon" aria-hidden="true">◎</div>
                <h3>Select an Objective Element</h3>
                <p>
                  Choose an OE from the compliance tree to review its activity timeline and linked
                  evidences on the right.
                </p>
              </div>
            }
          </section>
        </div>
      } @else {
        <div class="empty-state">
          <div class="empty-state__icon" aria-hidden="true">!</div>
          <h3>No Framework Context</h3>
          <p>No active journey or framework context is available for assessment review.</p>
        </div>
      }
    </div>
  `,
})
export class AuditorAssessmentPageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly traceLoading = signal(false);
  readonly context = signal<AuditorAssessmentContext | null>(null);
  readonly selectedOe = signal<AuditorObjectiveElementContext | null>(null);
  readonly trace = signal<EvidenceTraceability | null>(null);
  readonly treeSearch = signal('');

  readonly treeExpanded = computed(() => this.treeSearch().trim().length > 0);

  readonly filteredChapters = computed(() => {
    const ctx = this.context();
    if (!ctx) return [] as AuditorChapterContext[];

    const query = this.treeSearch().trim().toLowerCase();
    if (!query) return ctx.chapters;

    return ctx.chapters
      .map((chapter) => this.filterChapter(chapter, query))
      .filter((chapter): chapter is AuditorChapterContext => chapter != null);
  });

  readonly evidenceEntries = computed(() =>
    (this.trace()?.timeline ?? []).filter((e) => this.isEvidenceLayer(e.layer)),
  );

  readonly activityEntries = computed(() => this.trace()?.timeline ?? []);

  ngOnInit(): void {
    this.publishParticipation();
    this.api
      .getAssessmentContext()
      .pipe(
        catchError(() => of(null)),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((ctx) => {
        if (ctx) this.context.set(ctx);
      });
  }

  /**
   * Evidence stage declaration. Subject narrows from the framework to the
   * selected objective element, which is what makes the panel's linked
   * documents and AI guidance specific rather than framework-wide.
   */
  private publishParticipation(): void {
    const oe = this.selectedOe();

    this.workspace.set({
      stage: 'evidence',
      subject: oe
        ? { kind: 'clause', id: oe.id, title: `${oe.code} — ${oe.description}` }
        : { kind: 'evidence', title: 'Assessment workspace' },
      workflowState: {
        stage: 'evidence',
        statusLabel: oe
          ? `${oe.verifiedEvidenceCount} of ${oe.evidenceCount} evidence verified`
          : 'No objective element selected',
        completionCriteria: 'All required evidence uploaded and verified',
      },
      relatedRecords: oe
        ? [
            {
              id: `${oe.id}-def`,
              label: 'Open deficiencies',
              kind: 'Deficiency',
              status: String(oe.deficiencyCount),
            },
            {
              id: `${oe.id}-capa`,
              label: 'Open CAPAs',
              kind: 'CAPA',
              status: String(oe.openCapaCount),
            },
            {
              id: `${oe.id}-tasks`,
              label: 'Linked tasks',
              kind: 'Task',
              status: String(oe.taskCount),
            },
          ]
        : [],
      aiContext: {
        facts: oe
          ? {
              objectiveElement: oe.code,
              evidenceCount: String(oe.evidenceCount),
              verifiedEvidence: String(oe.verifiedEvidenceCount),
              openCapas: String(oe.openCapaCount),
            }
          : {},
      },
    });
  }

  selectOe(oe: AuditorObjectiveElementContext): void {
    this.selectedOe.set(oe);
    this.publishParticipation();
    this.trace.set(null);
    this.traceLoading.set(true);
    this.api
      .getTraceability(oe.id)
      .pipe(
        catchError(() => of(null)),
        finalize(() => this.traceLoading.set(false)),
      )
      .subscribe((t) => {
        if (t) this.trace.set(t);
      });
  }

  onTreeSearch(value: string): void {
    this.treeSearch.set(value);
  }

  clearTreeSearch(): void {
    this.treeSearch.set('');
  }

  private filterChapter(
    chapter: AuditorChapterContext,
    query: string,
  ): AuditorChapterContext | null {
    const chapterMatch = this.matchesQuery(query, chapter.code, chapter.name);
    const standards = chapter.standards
      .map((standard) => this.filterStandard(standard, query, chapterMatch))
      .filter((standard): standard is AuditorStandardContext => standard != null);

    if (chapterMatch || standards.length) {
      return { ...chapter, standards };
    }
    return null;
  }

  private filterStandard(
    standard: AuditorStandardContext,
    query: string,
    chapterMatch: boolean,
  ): AuditorStandardContext | null {
    const standardMatch = this.matchesQuery(query, standard.code, standard.name);
    const objectiveElements = standard.objectiveElements.filter(
      (oe) => chapterMatch || standardMatch || this.matchesQuery(query, oe.code, oe.description),
    );

    if (standardMatch || objectiveElements.length) {
      return { ...standard, objectiveElements };
    }
    return null;
  }

  private matchesQuery(query: string, ...fields: string[]): boolean {
    return fields.some((field) => field.toLowerCase().includes(query));
  }

  private isEvidenceLayer(layer: string): boolean {
    return layer.toLowerCase().replace(/\s+/g, '') === 'evidence';
  }

  timelineItemClass(layer: string): string {
    const key = layer.toLowerCase().replace(/\s+/g, '');
    const map: Record<string, string> = {
      objectiveelement: 'timeline__item timeline__item--objective',
      obligation: 'timeline__item timeline__item--obligation',
      evidence: 'timeline__item timeline__item--evidence',
      deficiency: 'timeline__item timeline__item--deficiency',
      capa: 'timeline__item timeline__item--capa',
      readiness: 'timeline__item timeline__item--readiness',
    };
    return map[key] ?? 'timeline__item';
  }

  statusBadgeClass(status: string): string {
    const tone = this.statusTone(status);
    return tone === 'neutral' ? 'status-badge' : `status-badge status-badge--${tone}`;
  }

  private statusTone(status: string): StatusTone {
    const s = status.toLowerCase();
    if (
      s.includes('verified') ||
      s.includes('compliant') ||
      s.includes('active') ||
      s.includes('closed')
    ) {
      return 'success';
    }
    if (s.includes('reject') || s.includes('noncompliant') || s.includes('critical')) {
      return 'danger';
    }
    if (
      s.includes('pending') ||
      s.includes('not assessed') ||
      s.includes('draft') ||
      s.includes('open')
    ) {
      return 'warning';
    }
    if (s.includes('%') || s.includes('progress') || s.includes('ca:') || s.includes('pa:')) {
      return 'info';
    }
    return 'neutral';
  }
}
