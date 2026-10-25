import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  Input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  displayEvidenceFileName,
  type TaskComment,
  type TaskDetail,
  type TaskDrawerTab,
  type TaskEvidenceItem,
  type TaskHistoryEntry,
} from '../models';
import {
  taskPriorityBadgeClass,
  taskRiskBadgeClass,
  taskStatusBadgeClass,
  taskStatusLabel,
} from '../task-display.util';
import { EvidenceStatusBadgeComponent } from '../../../shared/ui/evidence-status-badge/evidence-status-badge.component';
import { canReviewEvidenceInQueue } from '../../../shared/models/evidence-status.model';
import {
  EVIDENCE_REVIEW_QUEUE_PATH,
  evidenceReviewQueueQuery,
} from '../../../shared/utils/evidence-review-navigation.util';

@Component({
  selector: 'app-task-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatProgressSpinnerModule, EvidenceStatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['../tasks-tags.css'],
  template: `
    @if (open()) {
      <div class="backdrop" (click)="close.emit()"></div>
      <aside class="panel" [class.fullscreen]="mode() === 'fullscreen'">
        <header class="hdr">
          <h2 class="hdr-title">{{ detail()?.title || 'Task' }}</h2>
          <button type="button" class="icon" (click)="close.emit()" aria-label="Close">×</button>
        </header>
        @if (detail(); as d) {
          <nav class="tabs">
            <button type="button" [class.on]="tab === 'details'" (click)="tab = 'details'">Details</button>
            <button type="button" [class.on]="tab === 'evidence'" (click)="tab = 'evidence'">Evidence</button>
            <button type="button" [class.on]="tab === 'comments'" (click)="onCommentsTab()">Comments</button>
            <button type="button" [class.on]="tab === 'history'" (click)="onHistoryTab()">History</button>
          </nav>
          <div class="body">
            @if (tab === 'details') {
              @if (d.relatedDeficiency; as def) {
                <section class="remediation-panel" aria-labelledby="remediation-h">
                  <h3 id="remediation-h" class="remediation-panel__title">Why this task exists</h3>
                  <dl class="remediation-grid">
                    <div class="remediation-row">
                      <dt>Deficiency</dt>
                      <dd>{{ def.title }}</dd>
                    </div>
                    <div class="remediation-row">
                      <dt>Severity</dt>
                      <dd>
                        <span class="tag tag-severity" [attr.data-severity]="def.severity">{{
                          def.severity
                        }}</span>
                      </dd>
                    </div>
                    <div class="remediation-row">
                      <dt>Type</dt>
                      <dd>{{ def.deficiencyType }}</dd>
                    </div>
                    @if (d.maturityStageLabel) {
                      <div class="remediation-row">
                        <dt>Stage</dt>
                        <dd>{{ d.maturityStageLabel }}</dd>
                      </div>
                    }
                    @if (d.relatedObligation; as ob) {
                      <div class="remediation-row">
                        <dt>Evidence</dt>
                        <dd>{{ ob.evidenceStatus }}</dd>
                      </div>
                      <div class="remediation-row">
                        <dt>Verification</dt>
                        <dd>{{ ob.verificationStatus }}</dd>
                      </div>
                      @if (ob.nextActionLabel) {
                        <div class="remediation-row remediation-row--full">
                          <dt>Next action</dt>
                          <dd>{{ ob.nextActionLabel }}</dd>
                        </div>
                      }
                    }
                  </dl>
                  @if (def.description) {
                    <p class="remediation-panel__reason">{{ def.description }}</p>
                  }
                </section>
              }
              <dl class="kv-grid">
                <div class="kv">
                  <dt>OE</dt>
                  <dd><span class="tag tag-oe">{{ d.objectiveElementCode }}</span></dd>
                </div>
                <div class="kv">
                  <dt>Status</dt>
                  <dd>
                    <span class="tag" [ngClass]="taskStatusBadgeClass(d.status)">{{
                      taskStatusLabel(d.status)
                    }}</span>
                  </dd>
                </div>
                <div class="kv">
                  <dt>Priority</dt>
                  <dd>
                    <span class="tag" [ngClass]="taskPriorityBadgeClass(d.priority)">{{ d.priority | uppercase }}</span>
                  </dd>
                </div>
                <div class="kv">
                  <dt>Risk</dt>
                  <dd>
                    <span class="tag" [ngClass]="taskRiskBadgeClass(d.riskLevel)">
                      {{ d.riskLevel }}
                      <span class="risk-num">({{ d.riskScore }})</span>
                    </span>
                  </dd>
                </div>
                <div class="kv kv--full">
                  <dt>Due</dt>
                  <dd>
                    @if (d.isOverdue && d.dueDate) {
                      <span class="due-urgent">
                        <svg class="due-ic" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                          <path
                            d="M12 2L2 20h20L12 2zm0 3.99L18.51 18H5.49L12 5.99zM11 10v4h2v-4h-2zm0 6v2h2v-2h-2z"
                          />
                        </svg>
                        {{ d.dueDate | date: 'mediumDate' }}
                      </span>
                    } @else {
                      {{ d.dueDate | date: 'mediumDate' }}
                    }
                  </dd>
                </div>
              </dl>
            }
            @if (tab === 'evidence') {
              <div class="evidence-tab">
                <div class="evidence-tab__list-wrap praman-scrollbar praman-scrollbar--panel">
                  @if (evidences().length === 0) {
                    <p class="muted evidence-tab__empty">No evidence linked to this task yet.</p>
                  }
                  <ul class="ev-cards">
                    @for (e of evidences(); track e.evidenceId) {
                      <li class="ev-card">
                        <div class="ev-card__icon" [attr.data-kind]="evidenceIconKind(e)">
                          <span class="ev-card__glyph" aria-hidden="true">{{ evidenceIconGlyph(e) }}</span>
                        </div>
                        <div class="ev-card__main">
                          <a class="ev-card__name" [href]="e.fileUrl" target="_blank" rel="noopener noreferrer">{{
                            displayFile(e)
                          }}</a>
                          <div class="ev-card__row">
                            <span class="ev-chip">{{ e.source }}</span>
                            <app-evidence-status-badge [status]="e.status" />
                            @if (canReviewEvidence(e)) {
                              <a
                                class="ev-review-link"
                                [routerLink]="reviewQueuePath"
                                [queryParams]="reviewQueueParams(e)"
                              >
                                Review
                              </a>
                            }
                            @if (e.fileSizeBytes > 0) {
                              <span class="ev-meta">{{ formatBytes(e.fileSizeBytes) }}</span>
                            }
                          </div>
                          <div class="ev-card__sub">{{ e.uploadedAt | date: 'medium' }}</div>
                        </div>
                      </li>
                    }
                  </ul>
                </div>
                <input #taskEvFile type="file" hidden (change)="onFile($event)" tabindex="-1" />
                <div
                  class="task-drawer__dropzone task-drawer__dropzone--pinned"
                  [class.task-drawer__dropzone--drag]="evidenceDragActive()"
                  role="button"
                  tabindex="0"
                  aria-label="Evidence upload area. Drop a file or press Enter to browse."
                  (click)="onEvidenceDropzoneClick($event, taskEvFile)"
                  (keydown)="onEvidenceDropzoneKeydown($event, taskEvFile)"
                  (dragover)="onEvidenceDragOver($event)"
                  (dragenter)="onEvidenceDragEnter($event)"
                  (dragleave)="onEvidenceDragLeave($event)"
                  (drop)="onEvidenceDrop($event)"
                >
                  <svg class="task-drawer__dropzone-ico" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path
                      d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12"
                      stroke="currentColor"
                      stroke-width="2"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
                  </svg>
                  <span class="task-drawer__dropzone-title">Drop file or click to upload</span>
                  <span class="task-drawer__dropzone-hint">Single file · linked to this task</span>
                </div>
              </div>
            }
            @if (tab === 'comments') {
              <div class="composer">
                <textarea rows="2" [(ngModel)]="commentDraft" placeholder="Add comment"></textarea>
                <button type="button" class="btn-primary" (click)="submitComment.emit(commentDraft); commentDraft = ''">
                  Post
                </button>
              </div>
              <ul>
                @for (c of comments(); track c.id) {
                  <li>{{ c.body }} — {{ c.createdAt | date: 'short' }}</li>
                }
              </ul>
            }
            @if (tab === 'history') {
              @if (historyLoading) {
                <mat-spinner class="history-spinner" diameter="28" />
              } @else if (history().length === 0) {
                <p class="muted history-empty">No history events.</p>
              } @else {
                <div class="timeline">
                  @for (h of history(); track h.id) {
                    <div class="timeline__row">
                      <div class="timeline__dot"></div>
                      <div class="timeline__body">
                        <div class="timeline__title">{{ h.action }}</div>
                        <div class="timeline__meta">
                          {{ h.performedBy }} · {{ h.timestamp | date: 'medium' }}
                        </div>
                      </div>
                    </div>
                  }
                </div>
              }
            }
          </div>
          <footer class="sticky">
            @if (detail(); as d) {
              <a
                class="btn-secondary btn-footer"
                [routerLink]="['/app', 'journey', 'execution', 'Progress', d.objectiveElementId]"
                >Edit task</a
              >
            }
            <button
              type="button"
              class="btn-primary btn-footer"
              [disabled]="completeDisabled()"
              [attr.title]="
                completeDisabled()
                  ? 'Evidence is still pending review. Approve or reject before completing.'
                  : null
              "
              (click)="markComplete.emit()"
            >
              Mark complete
            </button>
          </footer>
        } @else {
          <p class="muted p2">Loading…</p>
        }
      </aside>
    }
  `,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        background: var(--color-overlay-backdrop);
        z-index: var(--z-modal, 1000);
      }
      .panel {
        position: fixed;
        top: var(--header-height, 4rem);
        right: 0;
        width: min(440px, 100%);
        height: calc(100dvh - var(--header-height, 4rem));
        max-height: calc(100dvh - var(--header-height, 4rem));
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
        z-index: calc(var(--z-modal, 1000) + 1);
        display: flex;
        flex-direction: column;
        box-shadow: var(--shadow-elevated-panel);
      }
      .panel.fullscreen {
        top: 0;
        width: 100%;
        height: 100dvh;
        max-height: 100dvh;
        z-index: calc(var(--z-modal, 1000) + 1);
      }
      .hdr {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0.75rem 1rem;
        border-bottom: 1px solid var(--color-border-subtle);
      }
      .hdr-title {
        margin: 0;
        font-size: 1.05rem;
        font-weight: 700;
        color: var(--color-text-primary);
        line-height: 1.3;
        padding-right: 0.5rem;
      }
      .btn-secondary {
        text-align: center;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
        cursor: pointer;
        text-decoration: none;
        display: inline-flex;
        align-items: center;
        justify-content: center;
      }
      .btn-secondary:hover {
        border-color: color-mix(in srgb, var(--color-brand-primary) 35%, transparent);
        color: var(--color-brand-primary);
      }
      .icon {
        border: none;
        background: none;
        font-size: 1.5rem;
        cursor: pointer;
        line-height: 1;
        color: var(--color-text-secondary);
      }
      .tabs {
        display: flex;
        gap: 0.25rem;
        padding: 0.5rem;
        border-bottom: 1px solid var(--color-border-subtle);
        flex-wrap: wrap;
      }
      .tabs button {
        border: none;
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
        padding: 0.35rem 0.6rem;
        border-radius: 6px;
        cursor: pointer;
        font-size: 0.8rem;
        font-weight: 500;
      }
      .tabs button.on {
        background: var(--color-brand-primary);
        color: var(--color-text-inverse);
      }
      .body {
        flex: 1;
        min-height: 0;
        overflow: auto;
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .remediation-panel {
        border: 1px solid color-mix(in srgb, var(--color-brand-primary) 25%, var(--color-border-subtle));
        background: color-mix(in srgb, var(--color-brand-primary) 6%, var(--color-bg-surface));
        border-radius: 10px;
        padding: 0.85rem 1rem;
      }
      .remediation-panel__title {
        margin: 0 0 0.65rem;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        color: var(--color-text-secondary);
      }
      .remediation-panel__reason {
        margin: 0.65rem 0 0;
        font-size: 0.85rem;
        color: var(--color-text-secondary);
        line-height: 1.45;
      }
      .remediation-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.45rem 0.75rem;
        margin: 0;
      }
      .remediation-row {
        margin: 0;
      }
      .remediation-row--full {
        grid-column: 1 / -1;
      }
      .remediation-row dt {
        font-size: 0.62rem;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-muted);
        margin-bottom: 0.15rem;
      }
      .remediation-row dd {
        margin: 0;
        font-size: 0.85rem;
        color: var(--color-text-primary);
      }
      .tag-severity[data-severity='Critical'] {
        background: color-mix(in srgb, #dc2626 18%, transparent);
        color: #b91c1c;
      }
      .kv-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.75rem 1rem;
        margin: 0;
      }
      .kv {
        margin: 0;
      }
      .kv--full {
        grid-column: 1 / -1;
      }
      .kv dt {
        font-size: 0.65rem;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-secondary);
        margin-bottom: 0.35rem;
      }
      .kv dd {
        margin: 0;
      }
      .due-urgent {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        color: var(--color-status-danger);
        font-weight: 600;
      }
      .due-ic {
        width: 1rem;
        height: 1rem;
      }
      .sticky {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        padding: 0.5rem 0.75rem;
        border-top: 1px solid var(--color-border-subtle);
        position: sticky;
        bottom: 0;
        background: var(--color-bg-surface);
      }
      .btn-footer {
        flex: 1 1 0;
        min-width: 0;
        padding: 0.38rem 0.55rem;
        border-radius: 6px;
        font-size: 0.8rem;
        font-weight: 600;
      }
      .btn-primary {
        text-align: center;
        border: none;
        background: var(--color-brand-primary);
        color: var(--color-text-inverse);
        cursor: pointer;
      }
      .btn-primary:hover:not(:disabled) {
        background: var(--color-brand-primary-hover);
      }
      .btn-primary:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .evidence-tab {
        display: flex;
        flex-direction: column;
        flex: 1 1 auto;
        min-height: 0;
        gap: 0.75rem;
      }
      .evidence-tab__list-wrap {
        flex: 1 1 0;
        min-height: 0;
        max-height: min(48vh, 22rem);
        overflow-x: hidden;
        overflow-y: auto;
        padding-right: 2px;
        margin-right: -2px;
      }
      .evidence-tab__empty {
        margin: 0 0 0.5rem;
      }
      .task-drawer__dropzone--pinned {
        flex-shrink: 0;
        margin-top: auto;
      }
      .task-drawer__dropzone {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.3rem;
        padding: 0.65rem 0.75rem;
        border-radius: 8px;
        border: 1px dashed color-mix(in srgb, var(--color-brand-primary) 35%, var(--color-border-subtle));
        background: color-mix(in srgb, var(--color-brand-primary-soft, var(--color-brand-primary)) 28%, var(--color-bg-page));
        color: var(--color-text-secondary);
        font-size: 0.72rem;
        cursor: pointer;
        transition: background-color 0.15s ease, border-color 0.15s ease;
        text-align: center;
      }
      .task-drawer__dropzone:hover {
        background: color-mix(in srgb, var(--color-brand-primary-soft, var(--color-brand-primary)) 40%, var(--color-bg-page));
      }
      .task-drawer__dropzone:focus-visible {
        outline: none;
        box-shadow: var(--shadow-focus-ring-primary, 0 0 0 2px var(--color-brand-primary));
      }
      .task-drawer__dropzone--drag {
        border-style: solid;
        background: color-mix(in srgb, var(--color-brand-primary-soft, var(--color-brand-primary)) 52%, var(--color-bg-page));
      }
      .task-drawer__dropzone-ico {
        width: 1.25rem;
        height: 1.25rem;
        color: color-mix(in srgb, var(--color-brand-primary) 85%, var(--color-text-secondary));
        flex-shrink: 0;
      }
      .task-drawer__dropzone-title {
        font-size: 0.78rem;
        font-weight: 600;
        color: var(--color-text-primary);
        line-height: 1.25;
      }
      .task-drawer__dropzone-hint {
        font-size: 0.68rem;
        color: var(--color-text-muted);
        line-height: 1.2;
      }
      .timeline {
        display: flex;
        flex-direction: column;
        gap: 14px;
      }
      .timeline__row {
        display: flex;
        gap: 12px;
      }
      .timeline__dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: var(--color-brand-primary);
        margin-top: 4px;
        flex-shrink: 0;
      }
      .timeline__title {
        font-size: 13px;
        font-weight: 500;
        color: var(--color-text-primary);
      }
      .timeline__meta {
        font-size: 11px;
        color: var(--color-text-muted);
      }
      .history-empty {
        font-size: 13px;
        margin: 0;
      }
      .history-spinner {
        margin: 0.25rem auto;
        display: block;
      }
      .composer textarea {
        width: 100%;
        margin-bottom: 0.5rem;
        box-sizing: border-box;
        padding: 0.5rem;
        border-radius: 8px;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
      }
      .ev-cards {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.65rem;
      }
      .ev-card {
        display: flex;
        gap: 0.75rem;
        padding: 0.75rem;
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
        background: color-mix(in srgb, var(--color-bg-surface-alt) 40%, var(--color-bg-surface));
      }
      .ev-card__icon {
        width: 2.5rem;
        height: 2.5rem;
        border-radius: 8px;
        flex-shrink: 0;
        display: grid;
        place-items: center;
        font-size: 0.65rem;
        font-weight: 800;
        letter-spacing: 0.02em;
        color: var(--color-text-inverse);
        background: var(--color-brand-primary);
      }
      .ev-card__icon[data-kind='pdf'] {
        background: #b91c1c;
      }
      .ev-card__icon[data-kind='image'] {
        background: #2f6bff;
      }
      .ev-card__icon[data-kind='sheet'] {
        background: #047857;
      }
      .ev-card__icon[data-kind='text'] {
        background: #4b5563;
      }
      .ev-card__glyph {
        font-size: 0.75rem;
      }
      .ev-card__main {
        min-width: 0;
        flex: 1;
      }
      .ev-card__name {
        display: block;
        font-weight: 600;
        color: var(--color-text-primary);
        text-decoration: none;
        word-break: break-word;
        margin-bottom: 0.35rem;
      }
      .ev-card__name:hover {
        color: var(--color-brand-primary);
        text-decoration: underline;
      }
      .ev-card__row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.35rem;
      }
      .ev-chip {
        font-size: 0.65rem;
        font-weight: 700;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        padding: 0.15rem 0.45rem;
        border-radius: 4px;
        border: 1px solid color-mix(in srgb, var(--color-brand-primary) 40%, transparent);
        color: var(--color-brand-primary);
        background: color-mix(in srgb, var(--color-brand-primary) 10%, transparent);
      }
      .ev-chip--status {
        border-color: color-mix(in srgb, var(--color-text-muted) 45%, transparent);
        color: var(--color-text-secondary);
        background: color-mix(in srgb, var(--color-text-muted) 8%, transparent);
      }
      .ev-review-link {
        font-size: 11px;
        font-weight: 600;
        color: var(--primary-color, var(--color-primary));
        text-decoration: none;
      }
      .ev-review-link:hover {
        text-decoration: underline;
      }
      .ev-meta {
        font-size: 0.75rem;
        color: var(--color-text-muted);
      }
      .ev-card__sub {
        font-size: 0.72rem;
        color: var(--color-text-muted);
        margin-top: 0.35rem;
      }
      .p2 {
        padding: 1rem;
      }
      .muted {
        color: var(--color-text-muted);
      }
    `,
  ],
})
export class TaskDrawerComponent {
  readonly open = input(false);
  readonly mode = input<'side' | 'fullscreen'>('side');
  readonly initialTab = input<TaskDrawerTab | null>(null);
  readonly detail = input<TaskDetail | null>(null);
  readonly evidences = input<TaskEvidenceItem[]>([]);
  readonly comments = input<TaskComment[]>([]);
  readonly history = input<TaskHistoryEntry[]>([]);
  /** Declared with @Input so the template compiler always exposes it (avoids NG8002 with some toolchains). */
  @Input() historyLoading = false;
  readonly completeDisabled = input(false);

  readonly close = output<void>();
  readonly markComplete = output<void>();
  readonly uploadFile = output<File>();
  readonly submitComment = output<string>();
  readonly loadComments = output<void>();
  readonly loadHistory = output<void>();

  readonly taskStatusLabel = taskStatusLabel;
  readonly taskStatusBadgeClass = taskStatusBadgeClass;
  readonly taskPriorityBadgeClass = taskPriorityBadgeClass;
  readonly taskRiskBadgeClass = taskRiskBadgeClass;

  tab: 'details' | 'evidence' | 'comments' | 'history' = 'details';
  commentDraft = '';
  private wasOpen = false;
  readonly evidenceDragActive = signal(false);

  constructor() {
    effect(() => {
      const o = this.open();
      if (o && !this.wasOpen) {
        const t = this.initialTab();
        if (t === 'evidence') {
          this.tab = 'evidence';
        } else if (t === 'comments') {
          this.tab = 'comments';
          this.loadComments.emit();
        } else if (t === 'history') {
          this.tab = 'history';
          this.loadHistory.emit();
        } else {
          this.tab = 'details';
        }
      }
      this.wasOpen = o;
    });
  }

  onCommentsTab(): void {
    this.tab = 'comments';
    this.loadComments.emit();
  }

  onHistoryTab(): void {
    this.tab = 'history';
    this.loadHistory.emit();
  }

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const f = input.files?.[0];
    if (f) this.uploadFile.emit(f);
    input.value = '';
  }

  onEvidenceDropzoneClick(ev: MouseEvent, fileInput: HTMLInputElement): void {
    if ((ev.target as HTMLElement).closest('a,button')) return;
    ev.preventDefault();
    fileInput.click();
  }

  onEvidenceDropzoneKeydown(ev: KeyboardEvent, fileInput: HTMLInputElement): void {
    if (ev.key !== 'Enter' && ev.key !== ' ') return;
    ev.preventDefault();
    fileInput.click();
  }

  onEvidenceDragOver(ev: DragEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
  }

  onEvidenceDragEnter(ev: DragEvent): void {
    ev.preventDefault();
    this.evidenceDragActive.set(true);
  }

  onEvidenceDragLeave(ev: DragEvent): void {
    ev.preventDefault();
    this.evidenceDragActive.set(false);
  }

  onEvidenceDrop(ev: DragEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
    this.evidenceDragActive.set(false);
    const f = ev.dataTransfer?.files?.[0];
    if (f) this.uploadFile.emit(f);
  }

  displayFile(e: TaskEvidenceItem): string {
    return displayEvidenceFileName(e.fileName);
  }

  readonly reviewQueuePath = EVIDENCE_REVIEW_QUEUE_PATH;

  canReviewEvidence(e: TaskEvidenceItem): boolean {
    return canReviewEvidenceInQueue(e.status);
  }

  reviewQueueParams(e: TaskEvidenceItem): Record<string, string> {
    return evidenceReviewQueueQuery({ evidenceId: e.evidenceId });
  }

  evidenceIconKind(e: TaskEvidenceItem): string {
    const t = (e.fileType || '').toLowerCase();
    const n = e.fileName.toLowerCase();
    if (t.includes('pdf') || n.endsWith('.pdf')) return 'pdf';
    if (t.includes('image') || /\.(png|jpe?g|gif|webp)$/i.test(n)) return 'image';
    if (t.includes('sheet') || t.includes('excel') || /\.(csv|xlsx?)$/i.test(n)) return 'sheet';
    if (t.includes('text') || n.endsWith('.txt')) return 'text';
    return 'doc';
  }

  evidenceIconGlyph(e: TaskEvidenceItem): string {
    switch (this.evidenceIconKind(e)) {
      case 'pdf':
        return 'PDF';
      case 'image':
        return 'IMG';
      case 'sheet':
        return 'XLS';
      case 'text':
        return 'TXT';
      default:
        return 'DOC';
    }
  }

  formatBytes(n: number): string {
    if (n <= 0) return '';
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / (1024 * 1024)).toFixed(1)} MB`;
  }
}
