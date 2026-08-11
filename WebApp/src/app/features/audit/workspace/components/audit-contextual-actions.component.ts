import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ContextDrawerService } from '../../../../shared/ui/context-drawer/context-drawer.service';
import { AuditWorkspaceActionsService } from '../services/audit-workspace-actions.service';
import { AuditWorkspaceStateService } from '../services/audit-workspace-state.service';

/**
 * Contextual action toolbar — Phase 3, Step 14.
 *
 * Renders the operations contributed for the stage the user is in. The set
 * changes with the stage, which is the whole point: the toolbar should offer
 * what is doable *here*, not a fixed menu that is mostly irrelevant.
 *
 * Renders nothing when no page has contributed. An empty toolbar band would
 * reserve vertical space and read as "actions unavailable" rather than "this
 * stage has no operations yet".
 */
@Component({
  selector: 'app-audit-contextual-actions',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (actions().length || drawer.hasSections()) {
      <div class="actions-bar" role="toolbar" aria-label="Audit workspace actions">
        @for (action of actions(); track action.id) {
          <button
            type="button"
            class="action"
            [class.is-primary]="action.variant === 'primary'"
            [class.is-danger]="action.variant === 'danger'"
            [disabled]="action.disabled ?? false"
            [attr.title]="action.disabled ? (action.disabledReason ?? null) : null"
            (click)="action.run()"
          >
            {{ action.label }}
          </button>
        }

        <!--
          Context entry points sit at the trailing edge, separated from the
          operational verbs: these open a surface rather than change a record,
          and mixing them would blur what the toolbar promises.
        -->
        @if (drawer.hasSections()) {
          <div class="context-entries">
            @for (section of drawer.sections(); track section.id) {
              <button
                type="button"
                class="entry"
                [class.is-active]="drawer.isOpen() && drawer.activeSection()?.id === section.id"
                [attr.aria-expanded]="drawer.isOpen() && drawer.activeSection()?.id === section.id"
                [title]="section.label"
                (click)="drawer.toggle(section.id)"
              >
                <span aria-hidden="true">{{ section.glyph }}</span>
                <span class="sr-only">{{ section.label }}</span>
                @if (section.badge) {
                  <span class="entry-badge">{{ section.badge }}</span>
                }
              </button>
            }
          </div>
        }
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .actions-bar {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        flex-wrap: wrap;
        padding: var(--space-2) var(--page-padding);
        background: var(--card-bg);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
      }

      .action {
        display: inline-flex;
        align-items: center;
        block-size: var(--control-height-sm);
        padding-inline: var(--space-3);
        border: var(--border-width-hairline) solid var(--border-color);
        border-radius: var(--radius-control);
        background: var(--card-bg);
        color: var(--text-primary);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-medium);
        cursor: pointer;
        white-space: nowrap;
        transition:
          background var(--motion-duration-base) var(--motion-easing-standard),
          border-color var(--motion-duration-base) var(--motion-easing-standard);
      }

      .action:hover:not(:disabled) {
        background: var(--bg-tertiary);
      }

      .action:focus-visible {
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }

      .action:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .action.is-primary {
        background: var(--primary-color);
        border-color: var(--primary-color);
        color: #fff;
      }

      .action.is-danger {
        color: var(--color-status-danger-fg);
        border-color: var(--color-status-danger-border);
      }

      .context-entries {
        display: flex;
        align-items: center;
        gap: var(--space-1);
        /* Push to the trailing edge so context reads as a separate affordance. */
        margin-inline-start: auto;
        padding-inline-start: var(--space-2);
        border-inline-start: var(--border-width-hairline) solid var(--border-color);
      }

      .entry {
        display: inline-flex;
        align-items: center;
        gap: var(--space-1);
        block-size: var(--control-height-sm);
        padding-inline: var(--space-3);
        border: none;
        border-radius: var(--radius-control);
        background: none;
        color: var(--text-secondary);
        /* The glyph is the only visible affordance here (the label is sr-only), so it carries the
           whole target and reads at icon scale rather than caption scale. */
        font-size: var(--icon-size-md);
        line-height: 1;
        cursor: pointer;
      }

      .entry:hover {
        background: var(--bg-tertiary);
        color: var(--text-primary);
      }

      .entry.is-active {
        background: var(--bg-tertiary);
        color: var(--primary-color);
      }

      .entry:focus-visible {
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }

      .entry-badge {
        font-size: var(--font-size-caption);
        font-variant-numeric: var(--font-variant-numeric-tabular);
        color: var(--text-muted);
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
export class AuditContextualActionsComponent {
  private readonly registry = inject(AuditWorkspaceActionsService);
  private readonly state = inject(AuditWorkspaceStateService);
  protected readonly drawer = inject(ContextDrawerService);

  /**
   * Recomputes when either the stage changes or a page republishes its actions,
   * so a page can enable/disable a button as its own state moves.
   */
  protected readonly actions = computed(() => {
    this.registry.contributions();
    return this.registry.actionsFor(this.state.currentStage()?.id ?? null);
  });
}
