import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import type { StageBlockingSummaryModel } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-cm-stage-blocking',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (blocking(); as b) {
      <section class="blocking" aria-labelledby="cm-blocking-heading">
        <h2 id="cm-blocking-heading" class="blocking__title">Stage Blockers — {{ b.stageLabel }}</h2>
        <p class="blocking__subtitle">Why progression is blocked and what remains before unlock</p>

        <dl class="blocking__metrics">
          <div><dt>Critical Deficiencies</dt><dd>{{ b.openCriticalDeficiencies }}</dd></div>
          <div><dt>Missing Evidence</dt><dd>{{ b.missingEvidenceCount }}</dd></div>
          <div><dt>Verification Failures</dt><dd>{{ b.verificationFailureCount }}</dd></div>
          <div><dt>Readiness Gap</dt><dd>{{ b.readinessGapPercent | number: '1.0-0' }}%</dd></div>
          <div><dt>Remaining Obligations</dt><dd>{{ b.remainingObligationsCount }}</dd></div>
        </dl>

        @if (b.blockingReasons.length) {
          <ul class="blocking__reasons">
            @for (reason of b.blockingReasons; track reason) {
              <li>{{ reason }}</li>
            }
          </ul>
        }

        <div class="blocking__nav">
          <a class="blocking__link" [routerLink]="['/app/journey/execution']" [queryParams]="{ stage: b.stage }">Open Journey</a>
          <a class="blocking__link" [routerLink]="['/app/tasks']">Open Tasks</a>
          <a class="blocking__link" [routerLink]="['/app/evidence']">Open Evidence</a>
          <a class="blocking__link" [routerLink]="['/app/audit/capa']">Open Issues &amp; Risks</a>
        </div>
      </section>
    }
  `,
  styles: [`
    .blocking {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
    }
    .blocking__title { margin: 0 0 4px; font-size: 16px; font-weight: 600; color: var(--text-primary); }
    .blocking__subtitle { margin: 0 0 14px; font-size: var(--font-size-body-sm); color: var(--text-secondary); }
    .blocking__metrics {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(7.5rem, 1fr));
      gap: 10px;
      margin: 0 0 14px;
    }
    .blocking__metrics dt {
      margin: 0;
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      color: var(--text-muted);
    }
    .blocking__metrics dd {
      margin: 2px 0 0;
      font-size: 18px;
      font-weight: 700;
      color: var(--text-primary);
    }
    .blocking__reasons {
      margin: 0 0 14px;
      padding-left: 1.2rem;
      color: var(--text-secondary);
      font-size: 13px;
      line-height: 1.45;
    }
    .blocking__nav {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
    .blocking__link {
      font-size: 12px;
      font-weight: 600;
      color: var(--primary-color);
      text-decoration: none;
      padding: 6px 10px;
      border: 1px solid var(--border-color);
      border-radius: 8px;
      background: var(--bg-secondary);
    }
    .blocking__link:hover { text-decoration: underline; }
  `],
})
export class CmStageBlockingComponent {
  blocking = input<StageBlockingSummaryModel | null>(null);
}
