import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-type-breakdown',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="breakdown-container">
      <h3 class="section-title">{{ title() }}</h3>
      <div class="types-grid">
        @for (item of items(); track item.label) {
          <div class="type-card">
            <div class="type-icon" [style.background]="item.bgColor" [style.color]="item.color">
              <span [innerHTML]="item.icon"></span>
            </div>
            <div class="type-body">
              <div class="type-info">
                <span class="type-label">{{ item.label }}</span>
                <span class="type-value">{{ item.value }}</span>
              </div>
              <div class="type-bar-wrapper">
                <div
                  class="type-bar"
                  [style.width.%]="item.percent"
                  [style.background]="item.color"
                ></div>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }

      .breakdown-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: all 0.3s ease;
        height: 100%;
        box-sizing: border-box;
        display: flex;
        flex-direction: column;
      }

      /* Type rows expand to fill the card height (kills empty bottom space) */
      .types-grid {
        flex: 1;
        align-content: space-evenly;
      }

      .breakdown-container:hover {
        box-shadow: var(--shadow-md);
      }

      .section-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 12px;
      }

      .types-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 10px;
      }

      /* Compact horizontal rows: icon + (label/value + bar) */
      .type-card {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 12px;
        padding: 10px 12px;
        border-radius: 10px;
        border: 1px solid var(--border-color);
        transition: all 0.2s ease;
      }

      .type-card:hover {
        background: var(--hover-bg);
      }

      .type-icon {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }

      .type-body {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
        gap: 5px;
      }

      .type-info {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 8px;
      }

      .type-value {
        font-size: 18px;
        font-weight: 700;
        color: var(--text-primary);
        line-height: 1;
      }

      .type-label {
        font-size: 11px;
        font-weight: 600;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.3px;
      }

      .type-bar-wrapper {
        height: 4px;
        background: var(--border-color);
        border-radius: 2px;
        overflow: hidden;
      }

      .type-bar {
        height: 100%;
        border-radius: 2px;
        transition: width 0.6s ease;
      }

      @media (max-width: 640px) {
        .types-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class TaskTypeBreakdownComponent {
  title = input<string>('Tasks by Type');
  byType = input.required<Record<string, number>>();

  private readonly typeConfig: Record<string, { color: string; bgColor: string; icon: string }> = {
    Assessment: {
      color: '#3b82f6',
      bgColor: 'rgba(59, 130, 246, 0.12)',
      icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/></svg>',
    },
    Evidence: {
      color: '#0d9488',
      bgColor: 'rgba(47, 107, 255, 0.12)',
      icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><path d="M14 2v6h6"/><path d="M16 13H8m8 4H8m2-8H8"/></svg>',
    },
    Review: {
      color: '#f59e0b',
      bgColor: 'rgba(245, 158, 11, 0.12)',
      icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/></svg>',
    },
    Audit: {
      color: '#ef4444',
      bgColor: 'rgba(239, 68, 68, 0.12)',
      icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>',
    },
  };

  private readonly defaultConfig = {
    color: '#6b7280',
    bgColor: 'rgba(107, 114, 128, 0.12)',
    icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/></svg>',
  };

  items = computed(() => {
    const byType = this.byType();
    const total = Object.values(byType).reduce((sum, v) => sum + v, 0) || 1;

    return Object.entries(byType).map(([label, value]) => {
      const config = this.typeConfig[label] ?? this.defaultConfig;
      return {
        label,
        value,
        percent: Math.round((value / total) * 100),
        ...config,
      };
    });
  });
}
