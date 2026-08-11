import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ComplianceMaturityDashboardModel,
  MaturityStage,
  MaturityStageModel,
} from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-cm-journey-sidebar',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="journey-sidebar" aria-label="Journey roadmap">
      <div class="journey-sidebar__card">
        <h2 class="journey-sidebar__title">Journey Roadmap</h2>

        <div class="roadmap-block">
          <span class="roadmap-label">Current Stage</span>
          <p class="roadmap-value roadmap-value--stage">{{ currentStageLabel() }}</p>
          <div class="roadmap-progress" role="progressbar"
               [attr.aria-valuenow]="currentStageScore()"
               aria-valuemin="0"
               aria-valuemax="100"
               [attr.aria-label]="'Current stage progress ' + currentStageScore() + ' percent'">
            <div class="roadmap-progress__fill" [style.width.%]="currentStageScore()"></div>
          </div>
          <span class="roadmap-hint">{{ currentStageScore() }}% complete</span>
        </div>

        @if (nextMilestone(); as milestone) {
          <div class="roadmap-block">
            <span class="roadmap-label">Next Milestone</span>
            <p class="roadmap-value">{{ milestone.label }}</p>
            <p class="roadmap-hint">{{ milestone.unlockMessage }}</p>
            <div class="roadmap-threshold">
              <span>Unlock at {{ milestone.unlockThreshold }}%</span>
              <span class="roadmap-threshold__gap">
                {{ milestone.unlockThreshold - currentStageScore() > 0
                  ? (milestone.unlockThreshold - currentStageScore()) + '% remaining'
                  : 'Threshold met' }}
              </span>
            </div>
          </div>
        }

        <div class="roadmap-block">
          <span class="roadmap-label">Progress Summary</span>
          <dl class="roadmap-stats">
            <div class="roadmap-stat">
              <dt>CMI Score</dt>
              <dd>{{ dashboard().complianceMaturityIndex.value | number: '1.0-0' }}</dd>
            </div>
            <div class="roadmap-stat">
              <dt>Core Readiness</dt>
              <dd>{{ dashboard().coreSummary.readinessScore | number: '1.0-0' }}%</dd>
            </div>
            <div class="roadmap-stat">
              <dt>Open Deficiencies</dt>
              <dd>{{ dashboard().coreSummary.openGapCount }}</dd>
            </div>
            <div class="roadmap-stat">
              <dt>Predicted Gaps</dt>
              <dd>{{ totalPredicted() }}</dd>
            </div>
          </dl>
        </div>

        <div class="roadmap-block roadmap-block--framework">
          <span class="roadmap-label">Framework</span>
          <p class="roadmap-framework">{{ dashboard().frameworkName }}</p>
          @if (dashboard().assessmentDate) {
            <p class="roadmap-hint">Last assessed {{ dashboard().assessmentDate | date: 'dd MMM yyyy' }}</p>
          } @else {
            <p class="roadmap-hint">Assessment in progress</p>
          }
        </div>
      </div>
    </aside>
  `,
  styles: [`
    .journey-sidebar {
      display: flex;
      flex-direction: column;
      gap: var(--cm-gap, 16px);
    }

    .journey-sidebar__card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .journey-sidebar__title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
      padding-bottom: 12px;
      border-bottom: 1px solid var(--border-color);
    }

    .roadmap-block {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .roadmap-block--framework {
      padding-top: 12px;
      border-top: 1px solid var(--border-color);
    }

    .roadmap-label {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--text-muted);
    }

    .roadmap-value {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .roadmap-value--stage {
      font-size: 18px;
      color: var(--primary-color);
    }

    .roadmap-hint {
      font-size: 12px;
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.45;
    }

    .roadmap-framework {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin: 0;
    }

    .roadmap-progress {
      height: 8px;
      background: var(--bg-tertiary);
      border-radius: 999px;
      overflow: hidden;
      margin-top: 4px;
    }

    .roadmap-progress__fill {
      height: 100%;
      background: var(--primary-color);
      border-radius: 999px;
      transition: width 0.4s ease;
    }

    .roadmap-threshold {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 12px;
      color: var(--text-secondary);
      margin-top: 4px;
      padding: 8px 10px;
      background: var(--bg-secondary);
      border-radius: 8px;
    }

    .roadmap-threshold__gap {
      font-weight: 600;
      color: var(--warning-color);
    }

    .roadmap-stats {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
      margin: 4px 0 0;
    }

    .roadmap-stat {
      display: flex;
      flex-direction: column;
      gap: 2px;
      padding: 10px;
      background: var(--bg-secondary);
      border-radius: 8px;
    }

    .roadmap-stat dt {
      font-size: 11px;
      color: var(--text-muted);
      margin: 0;
    }

    .roadmap-stat dd {
      font-size: 18px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0;
    }
  `],
})
export class CmJourneySidebarComponent {
  dashboard = input.required<ComplianceMaturityDashboardModel>();

  currentStageLabel = computed(() => {
    const stage = this.dashboard().currentStage;
    return stage.charAt(0).toUpperCase() + stage.slice(1);
  });

  currentStageScore = computed(() => {
    const d = this.dashboard();
    const stage = d.stages.find((s) => s.stage === d.currentStage);
    return stage?.score ?? 0;
  });

  nextMilestone = computed((): MaturityStageModel | null => {
    const d = this.dashboard();
    const idx = d.stages.findIndex((s) => s.stage === d.currentStage);
    if (idx < 0 || idx >= d.stages.length - 1) return null;
    const next = d.stages[idx + 1];
    return next.status === 'locked' ? next : null;
  });

  totalPredicted = computed(() => {
    const d = this.dashboard();
    return (
      d.commitmentSummary.predictedGapCount +
      d.achievementSummary.predictedGapCount +
      d.excellenceSummary.predictedGapCount
    );
  });
}
