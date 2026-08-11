import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuditContextPanelService } from '../../services/audit-context-panel.service';
import { AuditWorkspaceParticipationService } from '../../services/audit-workspace-participation.service';

/**
 * Audit's contextual drawer sections — Stage 1.2.
 *
 * The permanent panel's contents, split so each becomes a separately loadable
 * drawer section. They are collected in one file because they share the same
 * list vocabulary and are meaningless apart; splitting further would trade
 * cohesion for a handful of near-empty modules.
 *
 * Each reads from {@link AuditWorkspaceParticipationService}, so the Stage 1.1
 * participation contract is unchanged — only where its data is displayed moved.
 */

/** Lifecycle status plus any ad-hoc sections the page contributed. */
@Component({
  selector: 'app-audit-context-summary-section',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (workflowState(); as wf) {
      <section class="sec">
        <h3>Lifecycle</h3>
        <ul>
          <li>
            <span class="lbl">Status</span><span class="val">{{ wf.statusLabel }}</span>
          </li>
          @if (wf.completionCriteria) {
            <li>
              <span class="hint">To advance: {{ wf.completionCriteria }}</span>
            </li>
          }
        </ul>
      </section>
    }

    @for (section of sections(); track section.id) {
      <section class="sec">
        <h3>{{ section.title }}</h3>
        @if (section.items.length) {
          <ul>
            @for (item of section.items; track item.label) {
              <li>
                <span class="lbl">{{ item.label }}</span>
                @if (item.value) {
                  <span class="val">{{ item.value }}</span>
                }
                @if (item.hint) {
                  <span class="hint">{{ item.hint }}</span>
                }
              </li>
            }
          </ul>
        } @else {
          <p class="muted">{{ section.emptyText ?? 'Nothing linked.' }}</p>
        }
      </section>
    }

    @if (recommendations().length) {
      <section class="sec">
        <h3>Suggested next</h3>
        <ul>
          @for (rec of recommendations(); track rec) {
            <li>
              <span class="lbl">{{ rec }}</span>
            </li>
          }
        </ul>
      </section>
    }

    @if (isEmpty()) {
      <p class="muted">No summary for this view.</p>
    }
  `,
  styleUrl: './audit-context-sections.scss',
})
export class AuditContextSummarySectionComponent {
  private readonly participation = inject(AuditWorkspaceParticipationService);
  private readonly panel = inject(AuditContextPanelService);

  protected readonly workflowState = this.participation.workflowState;
  protected readonly sections = this.panel.sections;

  /**
   * Suggestions for the stage the work actually sits in (Stage 3, Phase 9) —
   * not the tab the user happens to have open.
   */
  protected readonly recommendations = computed(() => {
    if (!this.participation.hasLifecycleFacts()) return [];
    const lifecycle = this.participation.lifecycle();
    const current = lifecycle.stages.find((s) => s.stage.id === lifecycle.currentStageId);
    return current?.recommendations ?? [];
  });

  protected readonly isEmpty = computed(
    () =>
      this.workflowState() === null &&
      this.sections().length === 0 &&
      this.recommendations().length === 0,
  );
}

/** Records that relate to the current subject. */
@Component({
  selector: 'app-audit-related-records-section',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (records().length) {
      <section class="sec">
        <ul>
          @for (rec of records(); track rec.id) {
            <li>
              @if (rec.route) {
                <a [routerLink]="rec.route">{{ rec.label }}</a>
              } @else {
                <span class="lbl">{{ rec.label }}</span>
              }
              @if (rec.status) {
                <span class="val">{{ rec.status }}</span>
              }
              <span class="hint">{{ rec.kind }}</span>
            </li>
          }
        </ul>
      </section>
    } @else {
      <p class="muted">No related records for this view.</p>
    }
  `,
  styleUrl: './audit-context-sections.scss',
})
export class AuditRelatedRecordsSectionComponent {
  protected readonly records = inject(AuditWorkspaceParticipationService).relatedRecords;
}

/** Policies, SOPs, evidence files and templates linked to the subject. */
@Component({
  selector: 'app-audit-documents-section',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (documents().length) {
      <section class="sec">
        <ul>
          @for (doc of documents(); track doc.id) {
            <li>
              @if (doc.route) {
                <a [routerLink]="doc.route">{{ doc.title }}</a>
              } @else {
                <span class="lbl">{{ doc.title }}</span>
              }
              <span class="hint">{{ doc.kind }}</span>
            </li>
          }
        </ul>
      </section>
    } @else {
      <p class="muted">No documents linked to this view.</p>
    }
  `,
  styleUrl: './audit-context-sections.scss',
})
export class AuditDocumentsSectionComponent {
  protected readonly documents = inject(AuditWorkspaceParticipationService).documents;
}

/** What has happened to the current record, newest first. */
@Component({
  selector: 'app-audit-timeline-section',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (timeline().length) {
      <section class="sec">
        <ul>
          @for (event of timeline(); track event.id) {
            <li class="tl">
              <span class="lbl">{{ event.label }}</span>
              <span class="hint">
                {{ event.at | date: 'MMM d, y, h:mm a'
                }}{{ event.actor ? ' · ' + event.actor : '' }}
              </span>
            </li>
          }
        </ul>
      </section>
    } @else {
      <p class="muted">No recorded activity for this view.</p>
    }
  `,
  styleUrl: './audit-context-sections.scss',
})
export class AuditTimelineSectionComponent {
  protected readonly timeline = inject(AuditWorkspaceParticipationService).timeline;
}
