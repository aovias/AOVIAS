import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { OverlayModule } from '@angular/cdk/overlay';
import { A11yModule } from '@angular/cdk/a11y';
import { UserContextService } from '../../services/user-context.service';
import { RoleRegistryService } from '../../auth/roles/role-registry.service';
import { HeaderLayoutService } from './header-layout.service';

/**
 * Viewing As — presentation-only role selector (NAV-2).
 * Extension hooks reserved for role switching / persona preview / simulation.
 */
export interface RoleSelectorExtensionHooks {
  /** Future: enable actual role switching. */
  enableRoleSwitch?: boolean;
  /** Future: persona / permission preview mode. */
  enablePersonaPreview?: boolean;
  /** Future: simulation mode. */
  enableSimulation?: boolean;
}

@Component({
  selector: 'app-role-selector',
  standalone: true,
  imports: [CommonModule, OverlayModule, A11yModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="role-selector__trigger"
      cdkOverlayOrigin
      #origin="cdkOverlayOrigin"
      (click)="toggle()"
      [attr.aria-expanded]="open()"
      aria-haspopup="listbox"
      aria-controls="role-selector-panel"
      [attr.aria-label]="'Viewing as ' + primaryLabel()"
    >
      @if (!layout.condenseLabels()) {
        <span class="role-selector__eyebrow">Viewing As</span>
      }
      <span class="role-selector__value">{{ primaryLabel() }}</span>
      <svg
        class="role-selector__chevron"
        [class.role-selector__chevron--open]="open()"
        width="12"
        height="12"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        aria-hidden="true"
      >
        <path d="M6 9l6 6 6-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>

    <ng-template
      cdkConnectedOverlay
      [cdkConnectedOverlayOrigin]="origin"
      [cdkConnectedOverlayOpen]="open()"
      [cdkConnectedOverlayHasBackdrop]="true"
      cdkConnectedOverlayBackdropClass="role-selector-backdrop"
      [cdkConnectedOverlayPositions]="positions"
      (backdropClick)="close()"
      (detach)="close()"
    >
      <div
        id="role-selector-panel"
        class="role-selector__panel"
        role="listbox"
        aria-label="Roles"
        cdkTrapFocus
        [cdkTrapFocusAutoCapture]="true"
      >
        <p class="role-selector__hint">Display only — role switching coming later</p>
        @for (role of roleLabels(); track role.id) {
          <button
            type="button"
            class="role-selector__option"
            role="option"
            [attr.aria-selected]="role.id === primaryId()"
            [class.role-selector__option--current]="role.id === primaryId()"
            (click)="onSelect(role.id)"
          >
            {{ role.label }}
          </button>
        }
      </div>
    </ng-template>
  `,
  styles: `
    :host {
      display: inline-flex;
      min-width: 0;
    }

    .role-selector__trigger {
      display: inline-flex;
      flex-direction: column;
      align-items: flex-start;
      justify-content: center;
      gap: 0;
      height: var(--header-control-height, 36px);
      padding: 2px 10px;
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 8px;
      background: transparent;
      color: inherit;
      cursor: pointer;
      min-width: 0;
      max-width: 180px;
      position: relative;
      padding-right: 26px;
    }

    .role-selector__trigger:hover {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .role-selector__trigger:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .role-selector__eyebrow {
      font-size: 10px;
      font-weight: 600;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--color-text-muted, var(--text-secondary));
      line-height: 1;
    }

    .role-selector__value {
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 140px;
    }

    .role-selector__chevron {
      position: absolute;
      right: 8px;
      top: 50%;
      transform: translateY(-50%);
      opacity: 0.7;
      transition: transform var(--sidebar-motion-duration, 180ms) ease;
    }

    .role-selector__chevron--open {
      transform: translateY(-50%) rotate(180deg);
    }

    .role-selector__panel {
      min-width: 200px;
      max-width: 280px;
      padding: 8px;
      background: var(--card-bg, #fff);
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 10px;
      box-shadow: 0 10px 28px rgba(0, 0, 0, 0.14);
    }

    .role-selector__hint {
      margin: 0 0 6px;
      padding: 0 6px;
      font-size: 11px;
      color: var(--color-text-muted, var(--text-secondary));
    }

    .role-selector__option {
      display: block;
      width: 100%;
      text-align: left;
      border: none;
      background: transparent;
      border-radius: 6px;
      padding: 8px 10px;
      font-size: 13px;
      cursor: pointer;
      color: inherit;
    }

    .role-selector__option:hover {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .role-selector__option--current {
      color: var(--color-brand-primary, var(--primary-color));
      font-weight: 600;
    }

    .role-selector__option:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: -2px;
    }
  `,
})
export class RoleSelectorComponent {
  private readonly userContext = inject(UserContextService);
  private readonly roleRegistry = inject(RoleRegistryService);
  readonly layout = inject(HeaderLayoutService);

  /** Reserved for NAV-3 — not applied in NAV-2. */
  readonly extensionHooks: RoleSelectorExtensionHooks = {
    enableRoleSwitch: false,
    enablePersonaPreview: false,
    enableSimulation: false,
  };

  readonly open = signal(false);
  private readonly roles = toSignal(this.userContext.roles$, {
    initialValue: this.userContext.getUserRoles(),
  });

  readonly roleLabels = computed(() =>
    (this.roles() ?? []).map((id) => ({
      id,
      label: this.roleRegistry.labelOf(id) || id,
    })),
  );

  readonly primaryId = computed(() => this.roles()?.[0] ?? '');
  readonly primaryLabel = computed(() => {
    const id = this.primaryId();
    return id ? this.roleRegistry.labelOf(id) || id : 'User';
  });

  readonly positions = [
    {
      originX: 'end' as const,
      originY: 'bottom' as const,
      overlayX: 'end' as const,
      overlayY: 'top' as const,
      offsetY: 6,
    },
  ];

  toggle(): void {
    this.open.update((v) => !v);
  }

  close(): void {
    this.open.set(false);
  }

  /** Visual-only selection — no business logic change. */
  onSelect(_roleId: string): void {
    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) this.close();
  }
}
