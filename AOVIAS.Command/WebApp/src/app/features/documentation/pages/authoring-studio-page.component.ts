import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, finalize, of, Subject, switchMap } from 'rxjs';
import { AuthoringApiService } from '../services/authoring-api.service';
import { IntelligenceApiService } from '../services/intelligence-api.service';
import {
  AI_ACTIONS,
  BASIC_BLOCKS,
  HEALTHCARE_BLOCKS,
  type AuthoringBlock,
  type AuthoringSection,
  type AuthoringSession,
  type ComplianceMappingSuggestion,
  type PatchAuthoringRequest,
  type RightPanelTab,
  type StructuredCompareResult,
} from '../models/authoring.models';
import type { AiComplianceReview, CrossReferenceSuggestion, DocumentIntelligence } from '../models/intelligence.models';

@Component({
  selector: 'app-authoring-studio-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="studio" [class.studio--dark]="darkMode()">
      <header class="studio__topbar">
        <div class="studio__topbar-left">
          <a routerLink="/app/documentation/authoring" class="studio__back">← Studio</a>
          <div>
            <input class="studio__title-input" [value]="session()?.title ?? ''" (input)="onTitleInput($event)" />
            <span class="studio__meta">{{ session()?.documentNumber }} · {{ session()?.status }}</span>
          </div>
        </div>
        <div class="studio__topbar-actions">
          <span class="studio__save-status">{{ saveStatus() }}</span>
          <button type="button" class="btn-secondary" (click)="darkMode.set(!darkMode())">Theme</button>
          <button type="button" class="btn-secondary" (click)="exportDoc('pdf')">PDF</button>
          <button type="button" class="btn-secondary" (click)="exportDoc('docx')">DOCX</button>
          <button type="button" class="btn-primary" (click)="saveNow()">Save</button>
        </div>
      </header>

      @if (error()) {
        <div class="studio__error">{{ error() }}</div>
      } @else if (loading()) {
        <p class="studio__loading">Loading Authoring Studio…</p>
      } @else if (session(); as s) {
        <div class="studio__layout" #layoutHost>
          <aside class="studio__panel studio__panel--outline" [style.width.px]="leftWidth()">
            <h3>Outline</h3>
            <ul class="outline-list">
              @for (sec of s.sections; track sec.id) {
                <li>
                  <button type="button" class="outline-item" [class.outline-item--active]="activeSectionId() === sec.id" (click)="selectSection(sec.id)">
                    {{ sec.title }}
                    @if (sec.isRequired && !sectionHasContent(sec)) { <span class="outline-missing">*</span> }
                  </button>
                </li>
              }
            </ul>
            <div class="resize-handle resize-handle--right" (mousedown)="startResize('left', $event)"></div>
          </aside>

          <main class="studio__panel studio__panel--editor">
            @if (activeSection(); as sec) {
              <div class="section-header">
                <h2>{{ sec.title }}</h2>
                <div class="block-toolbar">
                  @for (b of basicBlocks; track b.type) {
                    <button type="button" class="chip" (click)="addBlock(sec.id, b.type)">+ {{ b.label }}</button>
                  }
                  @for (b of healthcareBlocks; track b.type) {
                    <button type="button" class="chip chip--health" (click)="addBlock(sec.id, b.type)">{{ b.label }}</button>
                  }
                </div>
              </div>

              <div class="blocks">
                @for (block of sec.blocks; track block.id) {
                  <div class="block" [attr.data-type]="block.type">
                    @if (block.type === 'heading') {
                      <input class="block-heading" [value]="blockText(block)" (input)="updateBlockText(sec.id, block.id, $event)" placeholder="Heading" />
                    } @else if (block.type === 'bulletList' || block.type === 'numberedList' || block.type === 'checklist') {
                      <textarea rows="4" [value]="listToText(block)" (input)="updateListBlock(sec.id, block.id, block.type, $event)"></textarea>
                    } @else {
                      <div class="block-text-wrap">
                        <textarea rows="3" [value]="blockText(block)" (input)="onBlockInput(sec.id, block.id, $event)" [class.block-callout]="isHealthcareBlock(block.type)"></textarea>
                        @if (xrefSuggestions().length && activeXrefBlockId() === block.id) {
                          <ul class="xref-menu">
                            @for (s of xrefSuggestions(); track s.targetId + s.referenceType) {
                              <li><button type="button" (click)="insertCrossRef(sec.id, block.id, s)">{{ s.insertText }} — {{ s.label }}</button></li>
                            }
                          </ul>
                        }
                      </div>
                    }
                    <div class="block-actions">
                      <button type="button" (click)="removeBlock(sec.id, block.id)">Remove</button>
                    </div>
                  </div>
                }
              </div>
            }
          </main>

          <aside class="studio__panel studio__panel--right" [style.width.px]="rightWidth()">
            <div class="resize-handle resize-handle--left" (mousedown)="startResize('right', $event)"></div>
            <nav class="right-tabs">
              @for (tab of rightTabs; track tab.id) {
                <button type="button" [class.active]="rightTab() === tab.id" (click)="rightTab.set(tab.id)">{{ tab.label }}</button>
              }
            </nav>

            <div class="right-body">
              @switch (rightTab()) {
                @case ('ai') {
                  <p class="muted">AI drafts only — humans approve and publish.</p>
                  @for (action of aiActions; track action.id) {
                    <button type="button" class="ai-action" [disabled]="aiLoading()" (click)="runAi(action.id)">{{ action.label }}</button>
                  }
                  @if (aiExplanation()) { <p class="ai-note">{{ aiExplanation() }}</p> }
                }
                @case ('mapping') {
                  @if (s.mappedObjectiveElement) {
                    <p><strong>Mapped:</strong> {{ s.mappedFramework }} → {{ s.mappedChapter }} → {{ s.mappedStandard }} → {{ s.mappedObjectiveElement }}</p>
                  }
                  @for (m of s.mappingSuggestions; track m.objectiveElement) {
                    <div class="mapping-card">
                      <p>{{ m.framework }} · {{ m.chapter }} · {{ m.standard }} · {{ m.objectiveElement }}</p>
                      <p class="muted">{{ m.reason }} ({{ m.confidence * 100 | number:'1.0-0' }}%)</p>
                      <button type="button" class="btn-secondary" (click)="acceptMapping(m)">Accept</button>
                    </div>
                  } @empty {
                    <p class="muted">No mapping suggestions — add clinical keywords to Purpose or Procedure.</p>
                  }
                }
                @case ('properties') {
                  <label>Department<input [value]="s.department ?? ''" (input)="department.set(($any($event.target)).value)" /></label>
                  <label>Schema<p class="muted">{{ s.authoringSchemaKey }}</p></label>
                  <label>Health score<p><strong>{{ s.health.overallHealthScore }}</strong> / 100</p></label>
                }
                @case ('comments') {
                  <textarea rows="3" placeholder="Add review comment…" [value]="newComment()" (input)="newComment.set(($any($event.target)).value)"></textarea>
                  <button type="button" class="btn-secondary" (click)="postComment()">Post comment</button>
                  @for (c of s.comments; track c.id) {
                    <div class="comment"><strong>{{ c.authorName }}</strong><p>{{ c.content }}</p></div>
                  }
                }
                @case ('health') {
                  <div class="health-grid">
                    <div><span>{{ s.health.completenessPercent }}%</span><small>Completeness</small></div>
                    <div><span>{{ s.health.readabilityScore }}</span><small>Readability</small></div>
                    <div><span>{{ s.health.approvalReadinessPercent }}%</span><small>Approval ready</small></div>
                  </div>
                  @if (s.health.missingSections.length) {
                    <p class="muted">Missing: {{ s.health.missingSections.join(', ') }}</p>
                  }
                }
                @case ('compare') {
                  <button type="button" class="btn-secondary" (click)="loadCompare()">Compare with previous version</button>
                  @if (compareResult(); as cmp) {
                    <p class="ai-note">{{ cmp.aiSummary }}</p>
                    @for (d of cmp.sectionDiffs; track d.sectionKey) {
                      <div class="diff-card"><strong>{{ d.title }}</strong> — {{ d.changeType }}</div>
                    }
                  }
                }
                @case ('intelligence') {
                  @if (intelligenceLoading()) {
                    <p class="muted">Loading document intelligence…</p>
                  } @else if (intelligence(); as intel) {
                    <div class="intel-score">
                      <span>{{ intel.health.overallHealthScore }}</span>
                      <small>Health Score</small>
                    </div>
                    <div class="health-grid">
                      <div><span>{{ intel.complianceMappingCoveragePercent }}%</span><small>Mapping</small></div>
                      <div><span>{{ intel.acknowledgementPercent }}%</span><small>Acknowledged</small></div>
                      <div><span>{{ intel.aiConfidencePercent }}%</span><small>AI Confidence</small></div>
                    </div>
                    <p class="impact-banner" [class.impact-banner--high]="intel.impact.publishingRisk === 'High'">
                      {{ intel.impact.impactSummary }}
                    </p>
                    <p class="muted">Publishing risk: <strong>{{ intel.impact.publishingRisk }}</strong></p>
                    @if (intel.strengths.length) {
                      <h4>Strengths</h4>
                      <ul class="intel-list">@for (s of intel.strengths; track s) { <li>{{ s }}</li> }</ul>
                    }
                    @if (intel.risks.length) {
                      <h4>Risks</h4>
                      <ul class="intel-list intel-list--risk">@for (r of intel.risks; track r) { <li>{{ r }}</li> }</ul>
                    }
                    @if (intel.linkedAssets.length) {
                      <h4>Linked Assets ({{ intel.linkedAssetsCount }})</h4>
                      @for (a of intel.linkedAssets; track a.id) {
                        <div class="mapping-card">{{ a.assetType }} · {{ a.label }}</div>
                      }
                    }
                    <button type="button" class="btn-secondary" [disabled]="aiReviewLoading()" (click)="runAiReview()">Run AI Compliance Review</button>
                    @if (aiReview(); as review) {
                      <div class="ai-review">
                        <p><strong>Review score: {{ review.overallComplianceReviewScore }}/100</strong> (advisory only)</p>
                        @for (sg of review.suggestions; track sg.message) {
                          <div class="diff-card" [class.diff-card--high]="sg.severity === 'high'">{{ sg.category }}: {{ sg.message }}</div>
                        }
                      </div>
                    }
                    <a [routerLink]="['/app/documentation/graph', documentId]" class="btn-secondary graph-link">View Relationship Graph</a>
                  }
                }
              }
            </div>
          </aside>
        </div>
      }
    </div>
  `,
  styles: [`
    .studio { display: flex; flex-direction: column; min-height: calc(100vh - 120px); background: var(--color-bg-surface, #fff); color: var(--color-text-primary, #111); }
    .studio--dark { background: #0f172a; color: #e2e8f0; }
    .studio--dark .studio__panel { background: #1e293b; border-color: #334155; }
    .studio__topbar { display: flex; justify-content: space-between; align-items: center; padding: 10px 16px; border-bottom: 1px solid var(--border-color, #e5e7eb); gap: 12px; flex-wrap: wrap; }
    .studio__topbar-left { display: flex; align-items: center; gap: 12px; }
    .studio__back { text-decoration: none; font-size: 13px; color: inherit; }
    .studio__title-input { font-size: 18px; font-weight: 600; border: none; background: transparent; width: min(480px, 50vw); color: inherit; }
    .studio__meta { display: block; font-size: 11px; opacity: 0.7; }
    .studio__topbar-actions { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .studio__save-status { font-size: 12px; opacity: 0.7; }
    .studio__layout { display: flex; flex: 1; min-height: 0; position: relative; }
    .studio__panel { border-right: 1px solid var(--border-color, #e5e7eb); overflow: auto; position: relative; }
    .studio__panel--outline { padding: 12px; flex-shrink: 0; }
    .studio__panel--editor { flex: 1; padding: 20px 28px; min-width: 280px; }
    .studio__panel--right { flex-shrink: 0; padding: 0; display: flex; flex-direction: column; border-right: none; border-left: 1px solid var(--border-color, #e5e7eb); }
    .outline-list { list-style: none; padding: 0; margin: 8px 0 0; }
    .outline-item { width: 100%; text-align: left; padding: 8px 10px; border: none; background: transparent; border-radius: 6px; cursor: pointer; color: inherit; font-size: 13px; }
    .outline-item--active { background: color-mix(in srgb, var(--color-primary) 12%, transparent); color: var(--color-primary); font-weight: 600; }
    .outline-missing { color: #dc2626; }
    .section-header h2 { margin: 0 0 12px; font-size: var(--font-size-section-title); }
    .block-toolbar { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 16px; }
    .chip { font-size: 11px; padding: 4px 8px; border-radius: 999px; border: 1px solid var(--border-color, #e5e7eb); background: transparent; cursor: pointer; color: inherit; }
    .chip--health { border-color: var(--color-primary-text); color: var(--color-primary-hover); }
    .blocks { display: flex; flex-direction: column; gap: 12px; max-width: 760px; }
    .block { border: 1px solid var(--border-color, #e5e7eb); border-radius: 8px; padding: 10px; }
    .block textarea, .block-heading { width: 100%; border: none; background: transparent; resize: vertical; font: inherit; color: inherit; }
    .block-callout { border-left: 3px solid var(--color-primary); padding-left: 8px; }
    .block-actions { text-align: right; margin-top: 4px; }
    .block-actions button { font-size: 11px; border: none; background: none; color: #64748b; cursor: pointer; }
    .resize-handle { position: absolute; top: 0; bottom: 0; width: 4px; cursor: col-resize; z-index: 2; }
    .resize-handle--right { right: 0; }
    .resize-handle--left { left: 0; }
    .right-tabs { display: flex; flex-wrap: wrap; gap: 2px; padding: 8px; border-bottom: 1px solid var(--border-color, #e5e7eb); }
    .right-tabs button { font-size: 11px; padding: 6px 8px; border: none; background: transparent; cursor: pointer; color: inherit; border-radius: 4px; }
    .right-tabs button.active { background: color-mix(in srgb, var(--color-primary) 15%, transparent); color: var(--color-primary); font-weight: 600; }
    .right-body { padding: 12px; overflow: auto; flex: 1; display: flex; flex-direction: column; gap: 8px; }
    .ai-action { text-align: left; padding: 8px 10px; border: 1px solid var(--border-color, #e5e7eb); border-radius: 6px; background: transparent; cursor: pointer; color: inherit; font-size: 12px; }
    .ai-note, .muted { font-size: 12px; opacity: 0.8; }
    .mapping-card, .diff-card, .comment { padding: 8px; border: 1px solid var(--border-color, #e5e7eb); border-radius: 6px; font-size: 12px; }
    .health-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; text-align: center; }
    .health-grid span { display: block; font-size: 20px; font-weight: 700; }
    .health-grid small { font-size: 10px; text-transform: uppercase; opacity: 0.7; }
    .studio__error { padding: 12px; background: #fef2f2; color: #b91c1c; }
    .studio__loading { padding: 24px; }
    label { display: block; font-size: 12px; margin-bottom: 8px; }
    label input { width: 100%; margin-top: 4px; padding: 6px; border-radius: 4px; border: 1px solid var(--border-color, #e5e7eb); background: transparent; color: inherit; }
    .btn-primary, .btn-secondary { padding: 6px 12px; border-radius: 6px; font-size: 12px; cursor: pointer; border: 1px solid var(--border-color, #e5e7eb); background: transparent; color: inherit; }
    .btn-primary { background: var(--color-primary); color: #fff; border-color: var(--color-primary); }
    .block-text-wrap { position: relative; }
    .xref-menu { position: absolute; left: 0; right: 0; top: 100%; list-style: none; margin: 0; padding: 4px; background: #fff; border: 1px solid var(--border-color, #e5e7eb); border-radius: 6px; z-index: 5; max-height: 160px; overflow: auto; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }
    .xref-menu button { width: 100%; text-align: left; padding: 6px 8px; border: none; background: transparent; font-size: 11px; cursor: pointer; }
    .xref-menu button:hover { background: color-mix(in srgb, var(--color-primary) 8%, transparent); }
    .intel-score { text-align: center; margin-bottom: 8px; }
    .intel-score span { font-size: 32px; font-weight: 700; display: block; }
    .intel-score small { font-size: 10px; text-transform: uppercase; opacity: 0.7; }
    .impact-banner { padding: 8px; border-radius: 6px; background: var(--color-primary-50); font-size: 12px; }
    .impact-banner--high { background: #fef2f2; color: #b91c1c; }
    .intel-list { margin: 0; padding-left: 16px; font-size: 12px; }
    .intel-list--risk { color: #b45309; }
    .ai-review { margin-top: 8px; }
    .diff-card--high { border-color: #fca5a5; }
    .graph-link { display: inline-block; margin-top: 8px; text-align: center; text-decoration: none; }
    h4 { margin: 8px 0 4px; font-size: 12px; }
  `],
})
export class AuthoringStudioPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AuthoringApiService);
  private readonly intelligenceApi = inject(IntelligenceApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly saveTrigger$ = new Subject<void>();
  private readonly xrefQuery$ = new Subject<{ blockId: string; query: string }>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly session = signal<AuthoringSession | null>(null);
  readonly activeSectionId = signal<string | null>(null);
  readonly saveStatus = signal('Saved');
  readonly darkMode = signal(false);
  readonly rightTab = signal<RightPanelTab>('ai');
  readonly aiLoading = signal(false);
  readonly aiExplanation = signal<string | null>(null);
  readonly compareResult = signal<StructuredCompareResult | null>(null);
  readonly newComment = signal('');
  readonly department = signal('');
  readonly leftWidth = signal(240);
  readonly rightWidth = signal(320);
  readonly intelligence = signal<DocumentIntelligence | null>(null);
  readonly intelligenceLoading = signal(false);
  readonly aiReview = signal<AiComplianceReview | null>(null);
  readonly aiReviewLoading = signal(false);
  readonly xrefSuggestions = signal<CrossReferenceSuggestion[]>([]);
  readonly activeXrefBlockId = signal<string | null>(null);
  readonly documentId = signal('');

  readonly basicBlocks = BASIC_BLOCKS;
  readonly healthcareBlocks = HEALTHCARE_BLOCKS;
  readonly aiActions = AI_ACTIONS;
  readonly rightTabs: { id: RightPanelTab; label: string }[] = [
    { id: 'intelligence', label: 'Intelligence' },
    { id: 'ai', label: 'AI' },
    { id: 'mapping', label: 'Mapping' },
    { id: 'properties', label: 'Props' },
    { id: 'comments', label: 'Review' },
    { id: 'health', label: 'Health' },
    { id: 'compare', label: 'Compare' },
  ];

  readonly activeSection = computed(() => {
    const s = this.session();
    const id = this.activeSectionId();
    return s?.sections.find((sec) => sec.id === id) ?? s?.sections[0] ?? null;
  });

  private documentIdInternal = '';
  private resizeSide: 'left' | 'right' | null = null;
  private resizeStartX = 0;
  private resizeStartWidth = 0;

  ngOnInit(): void {
    this.documentIdInternal = this.route.snapshot.paramMap.get('id') ?? '';
    this.documentId.set(this.documentIdInternal);
    if (!this.documentIdInternal) {
      this.error.set('Document not found.');
      this.loading.set(false);
      return;
    }

    this.api.getAuthoring(this.documentIdInternal).pipe(
      catchError(() => { this.error.set('Unable to load authoring session.'); return of(null); }),
      finalize(() => this.loading.set(false)),
    ).subscribe((s) => {
      if (!s) return;
      this.session.set(structuredClone(s));
      this.activeSectionId.set(s.sections[0]?.id ?? null);
      this.department.set(s.department ?? '');
      this.refreshIntelligence();
    });

    this.saveTrigger$.pipe(
      debounceTime(2000),
      switchMap(() => {
        this.saveStatus.set('Saving…');
        return this.api.patchAuthoring(this.documentIdInternal, this.buildPatch()).pipe(
          catchError(() => { this.saveStatus.set('Save failed'); return of(null); }),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((s) => {
      if (s) {
        this.session.set(s);
        this.saveStatus.set('Saved ' + new Date().toLocaleTimeString());
        this.refreshIntelligence();
      }
    });

    this.xrefQuery$.pipe(
      debounceTime(250),
      switchMap(({ blockId, query }) => {
        if (query.length < 1) {
          this.xrefSuggestions.set([]);
          this.activeXrefBlockId.set(null);
          return of([] as CrossReferenceSuggestion[]);
        }
        this.activeXrefBlockId.set(blockId);
        return this.intelligenceApi.getCrossReferenceSuggestions(this.documentIdInternal, query).pipe(
          catchError(() => of([] as CrossReferenceSuggestion[])),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((suggestions) => this.xrefSuggestions.set(suggestions));
  }

  selectSection(id: string): void {
    this.activeSectionId.set(id);
  }

  sectionHasContent(sec: AuthoringSection): boolean {
    return sec.blocks.some((b) => this.blockText(b).trim().length > 0);
  }

  blockText(block: AuthoringBlock): string {
    return String(block.data['text'] ?? '');
  }

  listToText(block: AuthoringBlock): string {
    const items = block.data['items'];
    return Array.isArray(items) ? items.join('\n') : '';
  }

  isHealthcareBlock(type: string): boolean {
    return HEALTHCARE_BLOCKS.some((b) => b.type === type);
  }

  onTitleInput(event: Event): void {
    const title = (event.target as HTMLInputElement).value;
    this.session.update((s) => s ? { ...s, title } : s);
    this.queueSave();
  }

  updateBlockText(sectionId: string, blockId: string, event: Event): void {
    const text = (event.target as HTMLInputElement | HTMLTextAreaElement).value;
    this.updateBlock(sectionId, blockId, (b) => ({ ...b, data: { ...b.data, text } }));
  }

  onBlockInput(sectionId: string, blockId: string, event: Event): void {
    const el = event.target as HTMLTextAreaElement;
    const text = el.value;
    this.updateBlock(sectionId, blockId, (b) => ({ ...b, data: { ...b.data, text } }), false);

    const atMatch = text.match(/@([\w.-]*)$/);
    if (atMatch) {
      this.xrefQuery$.next({ blockId, query: atMatch[1] });
    } else {
      this.xrefSuggestions.set([]);
      this.activeXrefBlockId.set(null);
    }
    this.queueSave();
  }

  insertCrossRef(sectionId: string, blockId: string, suggestion: CrossReferenceSuggestion): void {
    const sec = this.session()?.sections.find((s) => s.id === sectionId);
    const block = sec?.blocks.find((b) => b.id === blockId);
    if (!block) return;

    const current = this.blockText(block);
    const updated = current.replace(/@[\w.-]*$/, suggestion.insertText + ' ');
    this.updateBlock(sectionId, blockId, (b) => ({ ...b, data: { ...b.data, text: updated } }));
    this.xrefSuggestions.set([]);
    this.activeXrefBlockId.set(null);

    this.api.addCrossReference({
      documentId: this.documentIdInternal,
      sectionId,
      referenceType: suggestion.referenceType,
      targetId: suggestion.targetId,
      targetLabel: suggestion.label,
      targetRoute: suggestion.route ?? undefined,
    }).subscribe();
  }

  refreshIntelligence(): void {
    this.intelligenceLoading.set(true);
    this.intelligenceApi.getIntelligence(this.documentIdInternal).pipe(
      catchError(() => of(null)),
      finalize(() => this.intelligenceLoading.set(false)),
    ).subscribe((intel) => this.intelligence.set(intel));
  }

  runAiReview(): void {
    this.aiReviewLoading.set(true);
    this.intelligenceApi.reviewAi(this.documentIdInternal).pipe(
      catchError(() => of(null)),
      finalize(() => this.aiReviewLoading.set(false)),
    ).subscribe((review) => this.aiReview.set(review));
  }

  updateListBlock(sectionId: string, blockId: string, type: string, event: Event): void {
    const items = (event.target as HTMLTextAreaElement).value.split('\n').filter(Boolean);
    this.updateBlock(sectionId, blockId, (b) => ({ ...b, data: { ...b.data, items } }));
  }

  updateBlock(sectionId: string, blockId: string, fn: (b: AuthoringBlock) => AuthoringBlock, triggerSave = true): void {
    this.session.update((s) => {
      if (!s) return s;
      return {
        ...s,
        sections: s.sections.map((sec) =>
          sec.id !== sectionId
            ? sec
            : { ...sec, blocks: sec.blocks.map((b) => (b.id === blockId ? fn(b) : b)) },
        ),
      };
    });
    if (triggerSave) this.queueSave();
  }

  addBlock(sectionId: string, type: string): void {
    const block: AuthoringBlock = {
      id: crypto.randomUUID().replace(/-/g, ''),
      type,
      data: type === 'bulletList' || type === 'numberedList' || type === 'checklist'
        ? { items: [''] }
        : { text: '' },
    };
    this.session.update((s) => {
      if (!s) return s;
      return {
        ...s,
        sections: s.sections.map((sec) =>
          sec.id === sectionId ? { ...sec, blocks: [...sec.blocks, block] } : sec,
        ),
      };
    });
    this.queueSave();
  }

  removeBlock(sectionId: string, blockId: string): void {
    this.session.update((s) => {
      if (!s) return s;
      return {
        ...s,
        sections: s.sections.map((sec) =>
          sec.id === sectionId ? { ...sec, blocks: sec.blocks.filter((b) => b.id !== blockId) } : sec,
        ),
      };
    });
    this.queueSave();
  }

  runAi(action: string): void {
    const sec = this.activeSection();
    if (!sec) return;
    this.aiLoading.set(true);
    this.api.draftAi({ documentId: this.documentIdInternal, sectionId: sec.id, action }).pipe(
      catchError(() => of(null)),
      finalize(() => this.aiLoading.set(false)),
    ).subscribe((res) => {
      if (!res) return;
      this.aiExplanation.set(res.explanation);
      this.session.update((s) => {
        if (!s) return s;
        return {
          ...s,
          sections: s.sections.map((section) =>
            section.id === sec.id
              ? { ...section, blocks: [...section.blocks, ...res.suggestedBlocks] }
              : section,
          ),
        };
      });
      this.queueSave();
    });
  }

  acceptMapping(m: ComplianceMappingSuggestion): void {
    this.api.patchAuthoring(this.documentIdInternal, {
      manualMapping: m,
    }).subscribe((s) => this.session.set(s));
  }

  postComment(): void {
    const content = this.newComment().trim();
    if (!content) return;
    this.api.addComment({ documentId: this.documentIdInternal, sectionId: this.activeSectionId() ?? undefined, content }).subscribe(() => {
      this.newComment.set('');
      this.api.getAuthoring(this.documentIdInternal).subscribe((s) => this.session.set(s));
    });
  }

  loadCompare(): void {
    this.api.compareStructured(this.documentIdInternal).pipe(catchError(() => of(null))).subscribe((r) => this.compareResult.set(r));
  }

  saveNow(): void {
    this.saveStatus.set('Saving…');
    this.api.patchAuthoring(this.documentIdInternal, { ...this.buildPatch(), createCheckpoint: true, checkpointLabel: 'Manual save' }).pipe(
      catchError(() => { this.saveStatus.set('Save failed'); return of(null); }),
    ).subscribe((s) => {
      if (s) {
        this.session.set(s);
        this.saveStatus.set('Checkpoint saved');
      }
    });
  }

  exportDoc(format: 'pdf' | 'docx'): void {
    this.api.exportDocument(this.documentIdInternal, format).pipe(
      catchError(() => {
        this.saveStatus.set('Export failed — save the document and try again.');
        return of(null);
      }),
    ).subscribe((res) => {
      if (!res?.base64Content) return;
      const bytes = Uint8Array.from(atob(res.base64Content), (c) => c.charCodeAt(0));
      const blob = new Blob([bytes], { type: res.contentType });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = res.fileName;
      a.click();
      URL.revokeObjectURL(url);
      this.saveStatus.set(`Exported ${res.fileName}`);
    });
  }

  startResize(side: 'left' | 'right', event: MouseEvent): void {
    event.preventDefault();
    this.resizeSide = side;
    this.resizeStartX = event.clientX;
    this.resizeStartWidth = side === 'left' ? this.leftWidth() : this.rightWidth();
    const onMove = (e: MouseEvent) => {
      const delta = e.clientX - this.resizeStartX;
      if (this.resizeSide === 'left') this.leftWidth.set(Math.max(180, Math.min(420, this.resizeStartWidth + delta)));
      else this.rightWidth.set(Math.max(260, Math.min(480, this.resizeStartWidth - delta)));
    };
    const onUp = () => {
      this.resizeSide = null;
      window.removeEventListener('mousemove', onMove);
      window.removeEventListener('mouseup', onUp);
    };
    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
  }

  private queueSave(): void {
    this.saveStatus.set('Unsaved changes…');
    this.saveTrigger$.next();
  }

  private buildPatch(): PatchAuthoringRequest {
    const s = this.session();
    return {
      title: s?.title,
      department: this.department() || undefined,
      sections: s?.sections.map((sec) => ({ id: sec.id, blocks: sec.blocks, isCollapsed: sec.isCollapsed })),
    };
  }
}
