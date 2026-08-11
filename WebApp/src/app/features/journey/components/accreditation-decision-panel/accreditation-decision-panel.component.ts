import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccreditationEligibility } from '../../models/journey-tree.model';
import { NABH_THRESHOLDS } from '../../constants/nabh-thresholds';

@Component({
  selector: 'app-accreditation-decision-panel',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data()) {
      <section class="accreditation-panel" aria-label="Accreditation eligibility">
        <h3 class="panel-title">Accreditation readiness</h3>
        <ul class="rule-list">
          <li [class.pass]="passOverall()" [class.fail]="!passOverall()">
            <span class="icon">{{ passOverall() ? '✔' : '❌' }}</span>
            Overall compliance ≥ {{ thresholds.minOverallCompliancePercent }}%
            <span class="val">({{ data()!.overallCompliancePercent }}%)</span>
          </li>
          <li [class.pass]="passCoreAvg()" [class.fail]="!passCoreAvg()">
            <span class="icon">{{ passCoreAvg() ? '✔' : '❌' }}</span>
            Core OE average ≥ {{ thresholds.minCoreOeAverage }}
            <span class="val">({{ data()!.coreOeAverageScore | number: '1.1-1' }})</span>
          </li>
          <li [class.pass]="passStd()" [class.fail]="!passStd()">
            <span class="icon">{{ passStd() ? '✔' : '❌' }}</span>
            Average standard ≥ {{ thresholds.minAverageStandard }}
            <span class="val">({{ data()!.averageStandardScore | number: '1.1-1' }})</span>
          </li>
          <li [class.pass]="passCh()" [class.fail]="!passCh()">
            <span class="icon">{{ passCh() ? '✔' : '❌' }}</span>
            Average chapter ≥ {{ thresholds.minAverageChapter }}
            <span class="val">({{ data()!.averageChapterScore | number: '1.1-1' }})</span>
          </li>
        </ul>
        @if (data()!.violations.length) {
          <div class="violations" role="status">
            <strong>Violations</strong>
            <ul>
              @for (v of data()!.violations; track v) {
                <li>{{ v }}</li>
              }
            </ul>
          </div>
        }
      </section>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .accreditation-panel {
      border: var(--border-width-hairline) solid var(--border-color);
      border-radius: var(--radius-card);
      padding: var(--card-padding-compact) var(--space-4);
      background: var(--card-bg);
      box-shadow: var(--shadow-xs);
    }
    .panel-title {
      margin: 0 0 var(--space-3);
      font-size: 0.9375rem;
      font-weight: var(--font-weight-bold);
      color: var(--text-primary);
    }
    .rule-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
    .rule-list li {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--space-2);
    }
    .rule-list li.pass .icon {
      color: var(--success-color);
    }
    .rule-list li.fail .icon {
      color: var(--danger-color);
    }
    .val {
      font-weight: var(--font-weight-semibold);
      color: var(--text-primary);
    }
    .violations {
      margin-top: var(--space-3);
      padding-top: var(--space-3);
      border-top: 1px dashed var(--border-color);
      font-size: var(--font-size-caption);
      color: var(--danger-color);
    }
    .violations ul {
      margin: var(--space-2) 0 0;
      padding-left: 1.125rem;
    }
  `,
})
export class AccreditationDecisionPanelComponent {
  readonly data = input<AccreditationEligibility | null>(null);
  readonly thresholds = NABH_THRESHOLDS;

  passOverall(): boolean {
    const d = this.data();
    return !!d && d.overallCompliancePercent >= NABH_THRESHOLDS.minOverallCompliancePercent;
  }
  passCoreAvg(): boolean {
    const d = this.data();
    return !!d && d.coreOeAverageScore >= NABH_THRESHOLDS.minCoreOeAverage;
  }
  passStd(): boolean {
    const d = this.data();
    return !!d && d.averageStandardScore >= NABH_THRESHOLDS.minAverageStandard;
  }
  passCh(): boolean {
    const d = this.data();
    return !!d && d.averageChapterScore >= NABH_THRESHOLDS.minAverageChapter;
  }
}
