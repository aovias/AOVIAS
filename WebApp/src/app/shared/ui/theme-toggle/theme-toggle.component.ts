import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      class="theme-toggle"
      (click)="toggleTheme()"
      [attr.aria-label]="isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
      type="button">
      <div class="toggle-track">
        <div class="toggle-thumb" [class.dark]="isDark()">
          <svg
            *ngIf="!isDark()"
            class="icon sun-icon"
            width="16"
            height="16"
            viewBox="0 0 16 16"
            fill="none">
            <circle cx="8" cy="8" r="3" fill="currentColor"/>
            <path d="M8 1V2M8 14V15M15 8H14M2 8H1M12.5 3.5L11.8 4.2M4.2 11.8L3.5 12.5M12.5 12.5L11.8 11.8M4.2 4.2L3.5 3.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          </svg>
          <svg
            *ngIf="isDark()"
            class="icon moon-icon"
            width="16"
            height="16"
            viewBox="0 0 16 16"
            fill="none">
            <path d="M14 8.5C13.8 10.7 12 12.5 9.8 12.9C7.6 13.3 5.4 12.4 4.3 10.6C3.2 8.8 3.3 6.5 4.6 4.8C5.9 3.1 8 2.3 10.1 2.7C8.9 3.3 8 4.6 8 6C8 8.2 9.8 10 12 10C12.8 10 13.5 9.8 14 9.4V8.5Z" fill="currentColor"/>
          </svg>
        </div>
      </div>
      <span class="toggle-label">{{ isDark() ? 'Dark' : 'Light' }}</span>
    </button>
  `,
  styles: [`
    .theme-toggle {
      position: fixed;
      bottom: 24px;
      right: 24px;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 20px;
      background: var(--color-surface, #ffffff);
      border: 1px solid var(--color-border, #e5e7eb);
      border-radius: 24px;
      cursor: pointer;
      transition: all 0.3s ease;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      z-index: 1000;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    }

    [data-theme="dark"] .theme-toggle {
      background: var(--color-surface, #1f2937);
      border-color: var(--color-border, #374151);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
    }

    .theme-toggle:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
    }

    [data-theme="dark"] .theme-toggle:hover {
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.6);
    }

    .theme-toggle:active {
      transform: translateY(0);
    }

    .toggle-track {
      position: relative;
      width: 44px;
      height: 24px;
      background: #e5e7eb;
      border-radius: 12px;
      transition: background-color 0.3s ease;
    }

    [data-theme="dark"] .toggle-track {
      background: #374151;
    }

    .toggle-thumb {
      position: absolute;
      top: 2px;
      left: 2px;
      width: 20px;
      height: 20px;
      background: #ffffff;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
    }

    .toggle-thumb.dark {
      transform: translateX(20px);
      background: #1f2937;
    }

    .icon {
      color: #f59e0b;
      transition: all 0.3s ease;
    }

    .moon-icon {
      color: var(--color-primary);
    }

    .toggle-label {
      font-size: 14px;
      font-weight: 600;
      color: var(--color-text-primary, #111827);
      user-select: none;
    }

    [data-theme="dark"] .toggle-label {
      color: var(--color-text-primary, #f9fafb);
    }

    @media (max-width: 640px) {
      .theme-toggle {
        bottom: 16px;
        right: 16px;
        padding: 10px 16px;
        gap: 10px;
      }

      .toggle-track {
        width: 40px;
        height: 22px;
      }

      .toggle-thumb {
        width: 18px;
        height: 18px;
      }

      .toggle-thumb.dark {
        transform: translateX(18px);
      }

      .icon {
        width: 14px;
        height: 14px;
      }

      .toggle-label {
        font-size: 13px;
      }
    }
  `]
})
export class ThemeToggleComponent {
  isDark = computed(() => this.themeService.theme() === 'dark');

  constructor(private themeService: ThemeService) {}

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
