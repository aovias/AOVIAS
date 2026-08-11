import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  MaturityStageModel,
  MaturityStageStatus,
  MaturityStage,
} from '../../models/compliance-maturity.model';

export type TimelineVisualState = 'completed' | 'active' | 'locked' | 'notStarted';

interface TimelineStep extends MaturityStageModel {
  visualState: TimelineVisualState;
}

@Component({
  selector: 'app-cm-journey-timeline',
  standalone: true,
  imports: [CommonModule, RouterLink, TitleCasePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="timeline-card" aria-label="NABH maturity journey timeline">
      <div class="timeline-card__header">
        <h2 class="timeline-card__title">Stage Governance Timeline</h2>
        <p class="timeline-card__subtitle">Unlock progress and blocking criteria from StageUnlockingService</p>
      </div>

      <ol class="timeline-track" role="list">
        @for (step of steps(); track step.stage; let i = $index; let last = $last) {
          <li
            class="timeline-step"
            [class]="'timeline-step--' + step.visualState"
            [attr.aria-current]="step.visualState === 'active' ? 'step' : null"
          >
            <div class="timeline-step__rail">
              <div
                class="timeline-step__node"
                [attr.data-state]="step.visualState"
                [attr.aria-label]="step.label + ': ' + stateLabel(step.visualState)"
              >
                @switch (step.visualState) {
                  @case ('completed') {
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
                      <polyline points="20 6 9 17 4 12"/>
                    </svg>
                  }
                  @case ('active') {
                    <span class="timeline-step__pulse" aria-hidden="true"></span>
                  }
                  @case ('locked') {
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
                      <rect x="3" y="11" width="18" height="11" rx="2"/>
                      <path d="M7 11V7a5 5 0 0110 0v4"/>
                    </svg>
                  }
                  @default {
                    <span class="timeline-step__dot" aria-hidden="true"></span>
                  }
                }
              </div>
              @if (!last) {
                <div
                  class="timeline-step__connector"
                  [class.timeline-step__connector--filled]="step.visualState === 'completed'"
                  aria-hidden="true"
                ></div>
              }
            </div>

            <div class="timeline-step__body">
              <div class="timeline-step__meta">
                <span class="timeline-step__name">{{ step.label }}</span>
                <span class="timeline-step__badge" [attr.data-state]="step.visualState">
                  {{ stateLabel(step.visualState) }}
                </span>
              </div>
              <div class="timeline-step__metrics">
                <span class="timeline-step__score">{{ step.score | number: '1.0-0' }}%</span>
                @if (step.gapCount > 0) {
                  <span class="timeline-step__gaps">{{ step.gapCount }} deficiencies</span>
                }
              </div>
              @if (step.blockingReasons?.length) {
                <ul class="timeline-step__blockers">
                  @for (reason of step.blockingReasons!; track reason) {
                    <li>{{ reason }}</li>
                  }
                </ul>
              } @else if (step.visualState === 'locked' || step.visualState === 'notStarted') {
                <p class="timeline-step__unlock">{{ step.unlockMessage }}</p>
              }
              @if (step.visualState === 'active' && isNearUnlock(step)) {
                <div class="timeline-step__unlock-hint">
                  <span class="timeline-step__unlock-badge">Unlock criteria almost met!</span>
                  <a
                    class="timeline-step__unlock-cta"
                    [routerLink]="['/app/journey/execution']"
                    [queryParams]="{ stage: step.stage }"
                    aria-label="Remediate remaining {{ step.stage }} compliance deficiencies to unlock the next stage"
                  >Remediate Deficiencies →</a>
                </div>
              }
              @if (isThresholdMet(step)) {
                <div class="timeline-step__unlock-hint timeline-step__unlock-hint--met">
                  <span class="timeline-step__unlock-badge timeline-step__unlock-badge--met">✓ Unlock criteria met</span>
                  @if (nextStageFor(step.stage); as nextStage) {
                    <a
                      class="timeline-step__unlock-cta timeline-step__unlock-cta--met"
                      [routerLink]="['/app/journey/execution']"
                      [queryParams]="{ stage: nextStage }"
                      aria-label="Begin {{ nextStage | titlecase }} stage"
                    >Begin {{ nextStage | titlecase }} Stage →</a>
                  }
                </div>
              }
            </div>
          </li>
        }
      </ol>
    </section>
  `,
  styles: [`
    .timeline-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px 24px;
      box-shadow: var(--shadow-sm);
    }

    .timeline-card__header {
      margin-bottom: 20px;
    }

    .timeline-card__title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 4px;
    }

    .timeline-card__subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--text-secondary);
      margin: 0;
    }

    .timeline-track {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 0;
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .timeline-step {
      display: flex;
      flex-direction: column;
      align-items: stretch;
      padding: 0 12px;
      position: relative;
    }

    .timeline-step:first-child { padding-left: 0; }
    .timeline-step:last-child { padding-right: 0; }

    .timeline-step__rail {
      display: flex;
      align-items: center;
      margin-bottom: 14px;
    }

    .timeline-step__node {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      border: 2px solid var(--border-color);
      background: var(--bg-secondary);
      color: var(--text-secondary);
      position: relative;
      z-index: 1;
    }

    .timeline-step__node[data-state='completed'] {
      background: color-mix(in srgb, var(--success-color) 15%, var(--card-bg));
      border-color: var(--success-color);
      color: var(--success-color);
    }

    .timeline-step__node[data-state='active'] {
      background: color-mix(in srgb, var(--primary-color) 15%, var(--card-bg));
      border-color: var(--primary-color);
      color: var(--primary-color);
    }

    .timeline-step__node[data-state='locked'] {
      background: var(--bg-tertiary);
      border-color: var(--border-color);
      color: var(--text-muted);
    }

    .timeline-step__node[data-state='notStarted'] {
      background: var(--bg-secondary);
      border-style: dashed;
      color: var(--text-muted);
    }

    .timeline-step__pulse {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: var(--primary-color);
      animation: pulse-ring 2s ease-in-out infinite;
    }

    @keyframes pulse-ring {
      0%, 100% { box-shadow: 0 0 0 0 color-mix(in srgb, var(--primary-color) 40%, transparent); }
      50% { box-shadow: 0 0 0 8px transparent; }
    }

    .timeline-step__dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: var(--text-muted);
    }

    .timeline-step__connector {
      flex: 1;
      height: 2px;
      background: var(--border-color);
      margin: 0 8px;
      min-width: 16px;
    }

    .timeline-step__connector--filled {
      background: var(--success-color);
    }

    .timeline-step__body {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .timeline-step__meta {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .timeline-step__name {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .timeline-step--active .timeline-step__name {
      color: var(--primary-color);
    }

    .timeline-step__badge {
      display: inline-flex;
      align-self: flex-start;
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      padding: 2px 8px;
      border-radius: 999px;
    }

    .timeline-step__badge[data-state='completed'] {
      background: color-mix(in srgb, var(--success-color) 12%, transparent);
      color: var(--success-color);
    }

    .timeline-step__badge[data-state='active'] {
      background: color-mix(in srgb, var(--primary-color) 12%, transparent);
      color: var(--primary-color);
    }

    .timeline-step__badge[data-state='locked'] {
      background: var(--bg-tertiary);
      color: var(--text-secondary);
    }

    .timeline-step__badge[data-state='notStarted'] {
      background: var(--bg-secondary);
      color: var(--text-muted);
    }

    .timeline-step__metrics {
      display: flex;
      align-items: baseline;
      gap: 8px;
    }

    .timeline-step__score {
      font-size: 22px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
    }

    .timeline-step__gaps {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .timeline-step__unlock {
      font-size: 12px;
      color: var(--text-muted);
      margin: 0;
      line-height: 1.4;
    }

    .timeline-step__blockers {
      margin: 0;
      padding-left: 1rem;
      font-size: 11px;
      color: var(--text-secondary);
      line-height: 1.4;
    }

    .timeline-step__unlock-hint {
      margin-top: 8px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .timeline-step__unlock-badge {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--warning-color, #d97706);
    }

    .timeline-step__unlock-badge--met {
      color: var(--success-color, #16a34a);
    }

    .timeline-step__unlock-hint--met .timeline-step__unlock-badge {
      color: var(--success-color, #16a34a);
    }

    .timeline-step__unlock-cta {
      font-size: 11px;
      font-weight: 600;
      color: var(--primary-color);
      text-decoration: none;
      transition: opacity 0.15s;
    }

    .timeline-step__unlock-cta:hover {
      opacity: 0.75;
      text-decoration: underline;
    }

    .timeline-step__unlock-cta--met {
      color: var(--success-color, #16a34a);
    }

    .timeline-step__unlock-hint--met .timeline-step__unlock-cta {
      color: var(--success-color, #16a34a);
    }

    @media (max-width: 1280px) {
      .timeline-track {
        grid-template-columns: repeat(2, 1fr);
        gap: 20px;
      }

      .timeline-step__connector { display: none; }
    }

    @media (max-width: 640px) {
      .timeline-track {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class CmJourneyTimelineComponent {
  stages = input.required<MaturityStageModel[]>();
  currentStage = input.required<MaturityStage>();

  steps = computed<TimelineStep[]>(() => {
    const stages = this.stages();
    const current = this.currentStage();
    const currentIdx = stages.findIndex((s) => s.stage === current);

    return stages.map((stage, idx) => ({
      ...stage,
      visualState: this.resolveVisualState(stage.status, idx, currentIdx),
    }));
  });

  private resolveVisualState(
    status: MaturityStageStatus,
    index: number,
    currentIdx: number,
  ): TimelineVisualState {
    if (status === 'completed') return 'completed';
    if (status === 'inProgress') return 'active';
    if (status === 'locked') {
      return index === currentIdx + 1 ? 'locked' : 'notStarted';
    }
    return 'notStarted';
  }

  stateLabel(state: TimelineVisualState): string {
    switch (state) {
      case 'completed': return 'Completed';
      case 'active': return 'Active';
      case 'locked': return 'Locked';
      case 'notStarted': return 'Not Started';
    }
  }

  /** True when stage readiness is within 10% of unlock threshold but not yet met. */
  isNearUnlock(step: TimelineStep): boolean {
    const threshold = step.unlockThreshold ?? 100;
    const readiness = step.readinessPercent ?? step.score;
    return readiness >= threshold - 10 && readiness < threshold;
  }

  /** True when stage readiness meets unlock threshold. */
  isThresholdMet(step: TimelineStep): boolean {
    if (step.stage === 'excellence') return false;
    const threshold = step.unlockThreshold ?? 100;
    const readiness = step.readinessPercent ?? step.score;
    return readiness >= threshold;
  }

  nextStageFor(stage: MaturityStage): MaturityStage | null {
    const map: Partial<Record<MaturityStage, MaturityStage>> = {
      core: 'commitment',
      commitment: 'achievement',
      achievement: 'excellence',
    };
    return map[stage] ?? null;
  }
}
