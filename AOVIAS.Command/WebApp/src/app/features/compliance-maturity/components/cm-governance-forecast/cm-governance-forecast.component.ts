import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { AccreditationForecastModel } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-cm-governance-forecast',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="forecast" aria-labelledby="cm-forecast-heading">
      <h2 id="cm-forecast-heading" class="forecast__title">Accreditation Readiness Forecast</h2>
      <p class="forecast__subtitle">Deterministic projection based on current stage unlock criteria</p>
      <dl class="forecast__grid">
        <div>
          <dt>Current Readiness</dt>
          <dd>{{ forecast().currentReadinessPercent | number: '1.0-0' }}%</dd>
        </div>
        <div>
          <dt>Target Readiness</dt>
          <dd>{{ forecast().targetReadinessPercent | number: '1.0-0' }}%</dd>
        </div>
        <div>
          <dt>Estimated Unlock Readiness</dt>
          <dd>{{ forecast().estimatedUnlockReadinessPercent | number: '1.0-0' }}%</dd>
        </div>
        <div>
          <dt>Remaining Effort</dt>
          <dd>{{ forecast().remainingEffortPercent | number: '1.0-0' }}%</dd>
        </div>
        <div>
          <dt>Compliance Maturity Index</dt>
          <dd>{{ forecast().complianceMaturityIndex | number: '1.0-0' }}</dd>
        </div>
      </dl>
    </section>
  `,
  styles: [`
    .forecast {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
    }
    .forecast__title {
      margin: 0 0 4px;
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
    }
    .forecast__subtitle {
      margin: 0 0 16px;
      font-size: var(--font-size-body-sm);
      color: var(--text-secondary);
    }
    .forecast__grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
      gap: 12px;
      margin: 0;
    }
    .forecast__grid dt {
      margin: 0;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--text-secondary);
    }
    .forecast__grid dd {
      margin: 4px 0 0;
      font-size: 22px;
      font-weight: 700;
      color: var(--text-primary);
    }
  `],
})
export class CmGovernanceForecastComponent {
  forecast = input.required<AccreditationForecastModel>();
}
