import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-gauge-chart',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="gauge-container">
      <h3 class="gauge-title">{{ title() }}</h3>
      <div class="gauge-wrapper">
        <svg class="gauge-svg" viewBox="0 0 200 108" preserveAspectRatio="xMidYMid meet">
          <defs>
            <linearGradient id="gauge-gradient-{{ id() }}" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" [attr.stop-color]="gradientStart()" />
              <stop offset="100%" [attr.stop-color]="gradientEnd()" />
            </linearGradient>
          </defs>

          <path
            class="gauge-track"
            [attr.d]="arcPath()"
            fill="none"
            stroke="var(--border-color)"
            stroke-width="12"
            stroke-linecap="round"
          />

          <path
            class="gauge-progress"
            [attr.d]="arcPath()"
            fill="none"
            [attr.stroke]="'url(#gauge-gradient-' + id() + ')'"
            stroke-width="12"
            stroke-linecap="round"
            [attr.stroke-dasharray]="dashArray()"
            [attr.stroke-dashoffset]="dashOffset()"
          />

          <text x="100" y="85" class="gauge-value" text-anchor="middle">
            {{ value() }}%
          </text>
          <text x="100" y="105" class="gauge-label" text-anchor="middle">
            {{ getStatusLabel() }}
          </text>
        </svg>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        min-height: 0;
      }

      .gauge-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: all 0.3s ease;
        height: 100%;
        display: flex;
        flex-direction: column;
      }

      .gauge-container:hover {
        box-shadow: var(--shadow-md);
      }

      .gauge-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 10px;
      }

      .gauge-wrapper {
        display: flex;
        justify-content: center;
        align-items: center;
        flex: 1;
        min-height: 0;
      }

      /* Fill the available card height so stacked gauges leave no vertical dead space */
      .gauge-svg {
        width: 100%;
        height: 100%;
        max-width: 320px;
        max-height: 100%;
      }

      .gauge-track {
        opacity: 0.2;
      }

      .gauge-progress {
        transition: stroke-dashoffset 1s ease-out;
      }

      .gauge-value {
        font-size: 40px;
        font-weight: 700;
        fill: var(--text-primary);
      }

      .gauge-label {
        font-size: 14px;
        font-weight: 500;
        fill: var(--text-secondary);
      }
    `,
  ],
})
export class GaugeChartComponent {
  title = input.required<string>();
  value = input.required<number>();
  id = input<string>('default');

  private readonly radius = 80;
  private readonly startAngle = -120;
  private readonly endAngle = 120;

  arcPath = computed(() => {
    const start = this.polarToCartesian(100, 90, this.radius, this.endAngle);
    const end = this.polarToCartesian(100, 90, this.radius, this.startAngle);
    const largeArcFlag = this.endAngle - this.startAngle <= 180 ? '0' : '1';
    return `M ${start.x} ${start.y} A ${this.radius} ${this.radius} 0 ${largeArcFlag} 0 ${end.x} ${end.y}`;
  });

  dashArray = computed(() => {
    const angleRange = this.endAngle - this.startAngle;
    const circumference = (angleRange / 360) * 2 * Math.PI * this.radius;
    return `${circumference} ${circumference}`;
  });

  dashOffset = computed(() => {
    const angleRange = this.endAngle - this.startAngle;
    const circumference = (angleRange / 360) * 2 * Math.PI * this.radius;
    const progress = this.value() / 100;
    return circumference * (1 - progress);
  });

  gradientStart = computed(() => {
    const value = this.value();
    if (value >= 90) return '#22c55e';
    if (value >= 75) return '#f59e0b';
    return '#ef4444';
  });

  gradientEnd = computed(() => {
    const value = this.value();
    if (value >= 90) return '#16a34a';
    if (value >= 75) return '#d97706';
    return '#dc2626';
  });

  getStatusLabel(): string {
    const value = this.value();
    if (value >= 90) return 'Excellent';
    if (value >= 75) return 'Good';
    if (value >= 60) return 'Fair';
    return 'Needs Attention';
  }

  private polarToCartesian(centerX: number, centerY: number, radius: number, angleInDegrees: number) {
    const angleInRadians = ((angleInDegrees - 90) * Math.PI) / 180.0;
    return {
      x: centerX + radius * Math.cos(angleInRadians),
      y: centerY + radius * Math.sin(angleInRadians),
    };
  }
}
