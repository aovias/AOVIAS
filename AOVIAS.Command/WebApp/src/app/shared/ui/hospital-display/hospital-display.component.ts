import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-hospital-display',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="hospital-display">
      <div class="hospital-icon">
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M9 22V12h6v10" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>

      <div class="hospital-info">
        <span class="hospital-name">{{ hospitalName() }}</span>
        <span class="organization-name">{{ organizationName() }}</span>
      </div>

      <button class="change-button" (click)="changeClick.emit()" title="Change hospital">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path d="M7 7h10v10M7 17L17 7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
    </div>
  `,
  styles: [`
    .hospital-display {
      margin: 16px 12px;
      padding: 16px;
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 12px;
      display: flex;
      align-items: center;
      gap: 12px;
      transition: all 0.3s ease;
      position: relative;
      overflow: hidden;
    }

    .hospital-display::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: linear-gradient(135deg, color-mix(in srgb, var(--color-primary) 5%, transparent) 0%, transparent 100%);
      opacity: 0;
      transition: opacity 0.3s ease;
    }

    .hospital-display:hover {
      background: rgba(255, 255, 255, 0.05);
      border-color: color-mix(in srgb, var(--color-primary) 30%, transparent);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
    }

    .hospital-display:hover::before {
      opacity: 1;
    }

    .hospital-icon {
      width: 40px;
      height: 40px;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-hover) 100%);
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      flex-shrink: 0;
      box-shadow: 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
      transition: all 0.3s ease;
    }

    .hospital-display:hover .hospital-icon {
      transform: scale(1.05);
      box-shadow: 0 6px 16px color-mix(in srgb, var(--color-primary) 40%, transparent);
    }

    .hospital-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 0;
    }

    .hospital-name {
      font-size: 14px;
      font-weight: 700;
      color: rgba(255, 255, 255, 0.95);
      line-height: 1.3;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .organization-name {
      font-size: 12px;
      font-weight: 500;
      color: rgba(255, 255, 255, 0.5);
      line-height: 1.3;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }

    .change-button {
      width: 32px;
      height: 32px;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: rgba(255, 255, 255, 0.6);
      cursor: pointer;
      transition: all 0.2s ease;
      flex-shrink: 0;
    }

    .change-button:hover {
      background: rgba(255, 255, 255, 0.1);
      border-color: var(--primary-color);
      color: var(--primary-color);
      transform: rotate(90deg);
    }

    @media (max-width: 1024px) {
      .hospital-display {
        margin: 12px 10px;
        padding: 14px;
      }

      .hospital-icon {
        width: 36px;
        height: 36px;
      }

      .hospital-icon svg {
        width: 20px;
        height: 20px;
      }

      .hospital-name {
        font-size: 13px;
      }

      .organization-name {
        font-size: 11px;
      }
    }
  `],
})
export class HospitalDisplayComponent {
  readonly hospitalName = input.required<string>();
  readonly organizationName = input.required<string>();
  readonly changeClick = output<void>();
}
