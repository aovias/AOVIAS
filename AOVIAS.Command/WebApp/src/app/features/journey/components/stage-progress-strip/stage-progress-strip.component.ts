import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  maturityStageStatusLabel,
  type MaturityStageStateModel,
} from '../../../../shared/models/maturity-stage.model';

@Component({
  selector: 'app-stage-progress-strip',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="stage-strip" aria-labelledby="stage-strip-heading">
      <h2 id="stage-strip-heading" class="stage-strip__title">Maturity Stage Progress</h2>
      <p class="stage-strip__subtitle">Stage readiness from the unlock engine — same source as Compliance Maturity</p>

      <div class="stage-strip__grid">
        @for (stage of stages(); track stage.stage) {
          <a
            class="stage-card"
            [class.stage-card--locked]="stage.status === 'locked'"
            [attr.data-stage]="stage.stage"
            [attr.data-status]="stage.status"
            [routerLink]="stage.status === 'locked' ? null : ['/app/journey/execution']"
            [queryParams]="stage.status === 'locked' ? null : { stage: stage.stage }"
            [attr.aria-disabled]="stage.status === 'locked' ? true : null"
            [attr.tabindex]="stage.status === 'locked' ? -1 : null"
            aria-label="View {{ stage.label }} stage in execution workspace"
          >
            <div class="stage-card__header">
              <span class="stage-card__label">{{ stage.label }}</span>
              <span class="stage-card__status" [attr.data-status]="stage.status">
                {{ statusLabel(stage.status) }}
              </span>
            </div>

            <div class="stage-card__scores">
              <span class="stage-card__readiness" [attr.data-tone]="scoreTone(displayReadiness(stage))">
                {{ displayReadiness(stage) | number:'1.0-0' }}% readiness
              </span>
              @if (stage.score !== displayReadiness(stage)) {
                <span class="stage-card__oe-score">{{ stage.score | number:'1.0-0' }}% OE score</span>
              }
            </div>

            <div class="stage-card__bar-track" aria-hidden="true">
              <div
                class="stage-card__bar-fill"
                [style.width.%]="displayReadiness(stage)"
                [attr.data-tone]="scoreTone(displayReadiness(stage))"
              ></div>
            </div>

            @if (stage.blockingReasons?.length && stage.status === 'locked') {
              <p class="stage-card__blocker">{{ stage.blockingReasons![0] }}</p>
            }

            <div class="stage-card__meta">
              @if (stage.status !== 'locked') {
                <span class="stage-card__cta">View →</span>
              } @else {
                <span class="stage-card__locked-label">Locked</span>
              }
            </div>
          </a>
        }
      </div>
    </section>
  `,
  styles: [`
    .stage-strip {
      background: var(--card-bg, var(--color-bg-card));
      border: 1px solid var(--border-color, var(--color-border-subtle));
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
    }

    .stage-strip__title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary, var(--color-text-primary));
      margin: 0 0 4px;
    }

    .stage-strip__subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--text-secondary, var(--color-text-secondary));
      margin: 0 0 16px;
    }

    .stage-strip__grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 12px;

      @media (max-width: 1024px) { grid-template-columns: repeat(2, 1fr); }
      @media (max-width: 640px)  { grid-template-columns: 1fr; }
    }

    .stage-card {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 14px;
      border-radius: 10px;
      border: 1px solid var(--color-border-subtle);
      background: var(--color-bg-surface);
      text-decoration: none;
      color: inherit;
      transition: border-color 0.15s, box-shadow 0.15s;
    }

    .stage-card:not(.stage-card--locked):hover {
      border-color: color-mix(in srgb, var(--color-brand-primary) 40%, var(--color-border-subtle));
      box-shadow: var(--shadow-sm);
    }

    .stage-card--locked {
      opacity: 0.85;
      cursor: not-allowed;
    }

    .stage-card[data-stage='core']       { --stage-color: var(--color-brand-primary, var(--color-primary)); }
    .stage-card[data-stage='commitment'] { --stage-color: var(--color-success, #16a34a); }
    .stage-card[data-stage='achievement'] { --stage-color: var(--color-warning, #d97706); }
    .stage-card[data-stage='excellence'] { --stage-color: var(--color-primary); }

    .stage-card__header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
    }

    .stage-card__label {
      font-size: 14px;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .stage-card__status {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      padding: 2px 8px;
      border-radius: 999px;
    }

    .stage-card__status[data-status='completed'] {
      background: color-mix(in srgb, var(--color-success) 12%, transparent);
      color: var(--color-success);
    }

    .stage-card__status[data-status='inProgress'],
    .stage-card__status[data-status='available'] {
      background: color-mix(in srgb, var(--color-brand-primary) 12%, transparent);
      color: var(--color-brand-primary);
    }

    .stage-card__status[data-status='locked'] {
      background: color-mix(in srgb, var(--color-text-secondary) 10%, transparent);
      color: var(--color-text-secondary);
    }

    .stage-card__scores {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: baseline;
    }

    .stage-card__readiness {
      font-size: 18px;
      font-weight: 700;
    }

    .stage-card__readiness[data-tone='success'] { color: var(--color-success); }
    .stage-card__readiness[data-tone='warning'] { color: var(--color-warning); }
    .stage-card__readiness[data-tone='danger']  { color: var(--color-danger); }

    .stage-card__oe-score {
      font-size: 11px;
      color: var(--color-text-secondary);
    }

    .stage-card__bar-track {
      height: 6px;
      border-radius: 999px;
      background: color-mix(in srgb, var(--color-text-primary) 8%, transparent);
      overflow: hidden;
    }

    .stage-card__bar-fill {
      height: 100%;
      border-radius: inherit;
      background: var(--stage-color, var(--color-brand-primary));
      transition: width 0.3s ease;
    }

    .stage-card__blocker {
      font-size: 11px;
      color: var(--color-text-secondary);
      margin: 0;
      line-height: 1.35;
    }

    .stage-card__meta {
      display: flex;
      justify-content: flex-end;
      font-size: 12px;
      font-weight: 600;
      color: var(--color-brand-primary);
    }

    .stage-card__locked-label {
      color: var(--color-text-secondary);
    }
  `],
})
export class StageProgressStripComponent {
  stages = input.required<MaturityStageStateModel[]>();

  statusLabel = maturityStageStatusLabel;

  displayReadiness(stage: MaturityStageStateModel): number {
    if (typeof stage.readinessPercent === 'number' && Number.isFinite(stage.readinessPercent)) {
      return stage.readinessPercent;
    }
    return stage.score;
  }

  scoreTone(score: number): 'success' | 'warning' | 'danger' {
    if (score >= 75) return 'success';
    if (score >= 50) return 'warning';
    return 'danger';
  }
}
