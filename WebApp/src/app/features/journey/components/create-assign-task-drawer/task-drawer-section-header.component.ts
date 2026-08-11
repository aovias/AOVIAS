import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideClipboardList,
  lucideLink2,
  lucidePaperclip,
  lucideTag,
  lucideUsers,
} from '@ng-icons/lucide';

@Component({
  selector: 'app-task-drawer-section-header',
  standalone: true,
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  viewProviders: [
    provideIcons({
      lucideClipboardList,
      lucideUsers,
      lucideTag,
      lucideLink2,
      lucidePaperclip,
    }),
  ],
  template: `
    <div class="task-drawer-section-header" role="presentation">
      <span class="task-drawer-section-header__icon-wrap" aria-hidden="true">
        <ng-icon [name]="iconName()" class="task-drawer-section-header__icon" />
      </span>
      <h3 class="task-drawer-section-header__title">{{ title() }}</h3>
    </div>
  `,
  styles: [
    `
      .task-drawer-section-header {
        display: flex;
        align-items: center;
        gap: var(--space-3, 12px);
        margin-bottom: var(--space-3, 12px);
      }
      .task-drawer-section-header__icon-wrap {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border-radius: var(--radius-md, 10px);
        background: color-mix(in srgb, var(--color-brand-primary) 12%, transparent);
        color: var(--color-brand-primary);
        flex-shrink: 0;
      }
      .task-drawer-section-header__icon {
        width: 20px;
        height: 20px;
      }
      .task-drawer-section-header__title {
        margin: 0;
        font-size: var(--font-size-md, 0.9375rem);
        font-weight: 600;
        color: color-mix(in srgb, var(--color-text-primary) 94%, var(--color-text-secondary) 6%);
        letter-spacing: -0.01em;
      }
    `,
  ],
})
export class TaskDrawerSectionHeaderComponent {
  title = input.required<string>();
  /** Lucide registry name, e.g. lucideClipboardList */
  iconName = input.required<string>();
}
