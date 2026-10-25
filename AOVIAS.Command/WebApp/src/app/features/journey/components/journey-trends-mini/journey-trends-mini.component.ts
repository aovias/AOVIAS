import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JourneyTrendPoint } from '../../models/journey-tree.model';

@Component({
  selector: 'app-journey-trends-mini',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="trends">
      <figure class="fig">
        <figcaption>OE score trend</figcaption>
        <svg class="chart" viewBox="0 0 120 40" preserveAspectRatio="none">
          <polyline [attr.points]="linePoints(oeTrend())" fill="none" stroke="var(--primary-color)" stroke-width="2" />
        </svg>
      </figure>
      <figure class="fig">
        <figcaption>Chapter improvement</figcaption>
        <svg class="chart" viewBox="0 0 120 40" preserveAspectRatio="none">
          <polyline [attr.points]="linePoints(chapterTrend(), 100)" fill="none" stroke="var(--success-color)" stroke-width="2" />
        </svg>
      </figure>
      <figure class="fig">
        <figcaption>Risk evolution</figcaption>
        <svg class="chart" viewBox="0 0 120 40" preserveAspectRatio="none">
          <polyline [attr.points]="linePoints(riskTrend(), 100)" fill="none" stroke="var(--warning-color)" stroke-width="2" />
        </svg>
      </figure>
    </div>
  `,
  styles: `
    .trends {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: var(--space-3);
      margin-bottom: var(--space-4);
    }
    .fig {
      margin: 0;
      padding: 0.625rem;
      border: var(--border-width-hairline) solid var(--border-color);
      border-radius: 0.625rem;
      background: var(--card-bg);
      box-shadow: var(--shadow-sm);
    }
    figcaption {
      font-size: 0.6875rem;
      font-weight: var(--font-weight-semibold);
      color: var(--text-secondary);
      margin-bottom: var(--space-2);
    }
    .chart {
      width: 100%;
      height: var(--space-12);
    }
  `,
})
export class JourneyTrendsMiniComponent {
  readonly oeTrend = input<JourneyTrendPoint[]>([]);
  readonly chapterTrend = input<JourneyTrendPoint[]>([]);
  readonly riskTrend = input<JourneyTrendPoint[]>([]);

  linePoints(pts: JourneyTrendPoint[], max = 5): string {
    if (!pts.length) return '';
    const w = 120;
    const h = 40;
    const min = Math.min(...pts.map((p) => p.value));
    const maxV = Math.max(max, Math.max(...pts.map((p) => p.value)), min + 0.01);
    return pts
      .map((p, i) => {
        const x = (i / Math.max(pts.length - 1, 1)) * w;
        const y = h - ((p.value - min) / (maxV - min)) * h;
        return `${x},${y}`;
      })
      .join(' ');
  }
}
