import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuditWorkspaceParticipationService } from '../services/audit-workspace-participation.service';
import { AuditWorkspaceStateService } from '../services/audit-workspace-state.service';
import type { AuditWorkflowStage, AuditWorkflowStageStatus } from '../models/audit-workflow.model';

/** One rendered stage. Both progress sources normalise to this. */
interface StatusBarItem {
  readonly stage: AuditWorkflowStage;
  readonly status: AuditWorkflowStageStatus;
  readonly outstanding: readonly string[];
  readonly blockedReason?: string;
}

/**
 * Workflow progress indicator — Phase 3, Step 12.
 *
 * Answers "where am I, what is done, what remains" without the user having to
 * reconstruct it from the tab strip. It renders the lifecycle from
 * {@link AUDIT_WORKFLOW_STAGES} and marks position from workspace state, so the
 * bar and the workflow engine can never disagree.
 *
 * Purely a read-out plus a shortcut: each stage links to its canonical route, so
 * the bar doubles as workflow-driven navigation (Step 1) rather than another
 * page-driven menu.
 *
 * Hidden entirely outside the lifecycle (overview, intelligence,
 * administration) — those are vantage points over the workflow, and showing an
 * all-pending bar there would imply the user had lost their progress.
 */
@Component({
  selector: 'app-audit-workflow-status-bar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <nav class="workflow-bar" aria-label="Audit workflow progress">
        <ol class="stages">
          @for (item of progress(); track item.stage.id) {
            <li
              class="stage"
              [class.is-complete]="item.status === 'complete'"
              [class.is-current]="item.status === 'current'"
              [attr.aria-current]="item.status === 'current' ? 'step' : null"
            >
              <a [routerLink]="item.stage.route" class="stage-link" [attr.title]="stageHint(item)">
                <span class="marker" aria-hidden="true">
                  @if (item.status === 'complete') {
                    <span class="glyph">&#10003;</span>
                  } @else if (item.status === 'current') {
                    <span class="dot filled"></span>
                  } @else {
                    <span class="dot"></span>
                  }
                </span>
                <span class="label">{{ item.stage.label }}</span>
                <span class="sr-only"
                  >{{ statusText(item.status)
                  }}{{ stageHint(item) ? '. ' + stageHint(item) : '' }}</span
                >
              </a>
            </li>
          }
        </ol>

        <!--
          The recommended action comes from real outstanding work when the page
          reported facts; otherwise it falls back to the next stage's generic
          label, which is what Phase 3 showed.
        -->
        @if (recommendedAction(); as action) {
          <span class="next-action is-derived">{{ action }}</span>
        } @else if (nextStage(); as next) {
          <a class="next-action" [routerLink]="next.route">
            {{ next.nextActionLabel }}
            <span aria-hidden="true">&#8594;</span>
          </a>
        }
      </nav>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .workflow-bar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-4);
        flex-wrap: wrap;
        padding: var(--space-2) var(--page-padding);
        background: var(--card-bg);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
      }

      .stages {
        display: flex;
        align-items: center;
        gap: var(--space-1);
        flex-wrap: wrap;
        margin: 0;
        padding: 0;
        list-style: none;
        min-inline-size: 0;
      }

      /* Connector between stages; the first item has nothing to connect back to. */
      .stage + .stage::before {
        content: '';
        inline-size: var(--space-4);
        block-size: var(--border-width-hairline);
        background: var(--border-color);
        margin-inline-end: var(--space-1);
      }

      .stage {
        display: flex;
        align-items: center;
      }

      .stage-link {
        display: inline-flex;
        align-items: center;
        gap: var(--icon-gap);
        padding: var(--space-1) var(--space-2);
        border-radius: var(--radius-pill);
        text-decoration: none;
        color: var(--text-muted);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-medium);
        white-space: nowrap;
        transition: color var(--motion-duration-base) var(--motion-easing-standard);
      }

      .stage-link:hover {
        color: var(--text-primary);
        background: var(--bg-tertiary);
      }

      .stage-link:focus-visible {
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }

      .is-complete .stage-link {
        color: var(--color-status-success-fg);
      }

      .is-current .stage-link {
        color: var(--text-primary);
        font-weight: var(--font-weight-semibold);
        background: var(--bg-tertiary);
      }

      .marker {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        inline-size: var(--icon-size-md);
        block-size: var(--icon-size-md);
      }

      .dot {
        inline-size: 0.5rem;
        block-size: 0.5rem;
        border-radius: var(--radius-pill);
        border: var(--border-width-hairline) solid currentColor;
      }

      .dot.filled {
        background: currentColor;
      }

      .glyph {
        font-size: var(--font-size-caption);
        line-height: 1;
      }

      .next-action.is-derived {
        background: var(--bg-tertiary);
        color: var(--text-primary);
        cursor: default;
      }

      .next-action {
        display: inline-flex;
        align-items: center;
        gap: var(--space-1);
        padding: var(--space-1) var(--space-3);
        border-radius: var(--radius-control);
        background: var(--primary-color);
        color: #fff;
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
        text-decoration: none;
        white-space: nowrap;
      }

      .next-action:focus-visible {
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }

      .sr-only {
        position: absolute;
        inline-size: 1px;
        block-size: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
      }
    `,
  ],
})
export class AuditWorkflowStatusBarComponent {
  private readonly state = inject(AuditWorkspaceStateService);
  private readonly participation = inject(AuditWorkspaceParticipationService);

  /**
   * Progress from real lifecycle state once a page reports facts, falling back
   * to positional progress otherwise (Stage 1.4, Step 12).
   *
   * The fallback matters: most pages report no facts yet, and a bar that showed
   * everything as pending there would be a regression on what Phase 3 shipped.
   * Where facts exist the bar stops asserting completion it cannot verify.
   */
  protected readonly progress = computed<readonly StatusBarItem[]>(() => {
    if (!this.participation.hasLifecycleFacts()) {
      // Normalised to the same shape so the template has one contract.
      return this.state.stageProgress().map((p) => ({
        stage: p.stage,
        status: p.status,
        outstanding: [] as readonly string[],
        blockedReason: undefined,
      }));
    }
    return this.participation.lifecycle().stages.map((s) => ({
      stage: s.stage,
      status: s.status,
      outstanding: s.outstanding,
      blockedReason: s.blockedReason,
    }));
  });

  /** The concrete thing to do next, when the lifecycle can tell us. */
  protected readonly recommendedAction = computed(() =>
    this.participation.hasLifecycleFacts()
      ? this.participation.lifecycle().recommendedAction
      : null,
  );

  protected readonly nextStage = this.state.suggestedNextStage;

  /** Only meaningful while the user is inside the lifecycle. */
  protected readonly visible = computed(() => this.state.currentStage() !== null);

  protected statusText(status: 'complete' | 'current' | 'pending'): string {
    if (status === 'complete') return 'completed';
    return status === 'current' ? 'current step' : 'not started';
  }

  /**
   * Outstanding work or the unmet prerequisite for a stage, as a tooltip and
   * for screen readers. Guidance only — the stage stays navigable either way
   * (Step 16 asks for guidance, not blocking).
   */
  protected stageHint(item: StatusBarItem): string | null {
    if (item.blockedReason) return item.blockedReason;
    return item.outstanding.length ? item.outstanding.join(' · ') : null;
  }
}
