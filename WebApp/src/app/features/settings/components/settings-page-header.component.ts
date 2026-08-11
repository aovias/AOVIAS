import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-settings-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="settings-page-header">
      <h1 class="settings-page-header__title">{{ title }}</h1>
      @if (description) {
        <p class="settings-page-header__description">{{ description }}</p>
      }
    </header>
  `,
  styles: [
    `
      .settings-page-header {
        margin-bottom: 28px;
        animation: settingsFadeIn 0.28s ease;
      }
      .settings-page-header__title {
        margin: 0 0 8px;
        font-size: var(--font-size-page-title);
        font-weight: 600;
        letter-spacing: -0.02em;
        color: var(--color-text-primary, var(--text-primary, #111827));
        line-height: 1.2;
      }
      .settings-page-header__description {
        margin: 0;
        max-width: 62ch;
        font-size: 15px;
        line-height: 1.55;
        color: var(--color-text-secondary, var(--text-secondary, #6b7280));
      }
      @keyframes settingsFadeIn {
        from {
          opacity: 0;
          transform: translateY(4px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }
    `,
  ],
})
export class SettingsPageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() description = '';
}
