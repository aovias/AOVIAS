import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HospitalSnapshot, JourneySnapshot } from '../models/dashboard-snapshot.model';
import { sortByCpFrameworkCodeKeys } from '../../../shared/utils/framework-code-sort';

@Component({
  selector: 'app-hospital-overview',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="hospital-overview">
      <h3 class="section-title">{{ title() }}</h3>
      <div class="hospital-grid">
        @for (hospital of hospitals(); track hospital.hospitalId) {
          <div class="hospital-card" [class.not-onboarded]="!hospital.isOnboarded">
            <!-- Card header -->
            <div class="card-header">
              <div class="hospital-info">
                <span class="hospital-name">{{ hospital.name }}</span>
                <span class="hospital-code">{{ hospital.code }}</span>
              </div>
              <span
                class="onboard-badge"
                [attr.data-status]="hospital.isOnboarded ? 'active' : 'pending'"
              >
                {{ hospital.isOnboarded ? 'Onboarded' : 'Pending' }}
              </span>
            </div>

            <!-- Stats row -->
            <div class="stats-row">
              <div class="stat">
                <span class="stat-value">{{ hospital.openTasks }}</span>
                <span class="stat-label">Open</span>
              </div>
              <div class="stat" [class.has-overdue]="hospital.overdueTasks > 0">
                <span class="stat-value overdue">{{ hospital.overdueTasks }}</span>
                <span class="stat-label">Overdue</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ hospital.journeys.length }}</span>
                <span class="stat-label">Journeys</span>
              </div>
            </div>

            <!-- Journeys -->
            @if (hospital.journeys.length) {
              <div class="journeys-section praman-scrollbar praman-scrollbar--panel">
                @for (journey of sortedJourneys(hospital.journeys); track journey.journeyId) {
                  <div class="journey-row">
                    <div class="journey-info">
                      <span class="journey-framework">{{ journey.frameworkCode }}</span>
                      <span
                        class="journey-status"
                        [attr.data-status]="journey.status"
                      >
                        {{ journey.status }}
                      </span>
                    </div>
                    <div class="journey-progress">
                      <div class="progress-bar">
                        <div
                          class="progress-fill"
                          [style.width.%]="journey.progressPercent"
                          [attr.data-score]="getScoreLevel(journey.score)"
                        ></div>
                      </div>
                      <span class="progress-text">{{ journey.score | number: '1.0-0' }}%</span>
                    </div>
                  </div>
                }
              </div>
            }

            <!-- Footer -->
            <div class="card-footer">
              <span class="last-updated">Updated {{ formatDate(hospital.lastUpdated) }}</span>
              @if (hospital.nabhLevel) {
                <span class="nabh-level">{{ hospital.nabhLevel }}</span>
              }
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .hospital-overview {
        margin-bottom: 0;
      }

      .section-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 12px;
      }

      .hospital-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
        gap: 16px;
      }

      .hospital-card {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        display: flex;
        flex-direction: column;
        gap: 10px;
        height: 100%;
      }

      .hospital-card:hover {
        box-shadow: var(--shadow-md);
        transform: translateY(-2px);
      }

      .hospital-card.not-onboarded {
        opacity: 0.7;
      }

      /* ── Header ── */
      .card-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 12px;
      }

      .hospital-info {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .hospital-name {
        font-size: 15px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .hospital-code {
        font-size: 12px;
        color: var(--text-secondary);
        font-weight: 500;
      }

      .onboard-badge {
        padding: 4px 10px;
        border-radius: 6px;
        font-size: 11px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.3px;
        flex-shrink: 0;
      }

      .onboard-badge[data-status='active'] {
        background: rgba(34, 197, 94, 0.1);
        color: var(--success-color);
      }

      .onboard-badge[data-status='pending'] {
        background: rgba(245, 158, 11, 0.1);
        color: var(--warning-color);
      }

      /* ── Stats ── */
      .stats-row {
        display: flex;
        gap: 16px;
        padding: 8px 0;
        border-top: 1px solid var(--border-color);
        border-bottom: 1px solid var(--border-color);
      }

      .stat {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 2px;
      }

      .stat-value {
        font-size: 18px;
        font-weight: 700;
        color: var(--text-primary);
      }

      .stat-value.overdue {
        color: var(--text-primary);
      }

      .has-overdue .stat-value.overdue {
        color: var(--danger-color);
      }

      .stat-label {
        font-size: 11px;
        font-weight: 500;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.3px;
      }

      /* ── Journeys ── */
      .journeys-section {
        display: flex;
        flex-direction: column;
        gap: 8px;
        max-height: 150px;
        overflow-y: auto;
      }

      /* Footer pinned to bottom so cards in the same row stay equal height */
      .card-footer {
        margin-top: auto;
      }

      .journey-row {
        display: flex;
        flex-direction: column;
        gap: 6px;
      }

      .journey-info {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .journey-framework {
        font-size: 13px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .journey-status {
        font-size: 11px;
        font-weight: 600;
        padding: 2px 8px;
        border-radius: 4px;
        text-transform: uppercase;
        letter-spacing: 0.3px;
      }

      .journey-status[data-status='InProgress'] {
        background: rgba(59, 130, 246, 0.1);
        color: var(--primary-color);
      }

      .journey-status[data-status='Submitted'] {
        background: rgba(245, 158, 11, 0.1);
        color: var(--warning-color);
      }

      .journey-status[data-status='Certified'] {
        background: rgba(34, 197, 94, 0.1);
        color: var(--success-color);
      }

      .journey-status[data-status='NotStarted'] {
        background: var(--hover-bg);
        color: var(--text-secondary);
      }

      .journey-progress {
        display: flex;
        align-items: center;
        gap: 10px;
      }

      .progress-bar {
        flex: 1;
        height: 6px;
        background: var(--border-color);
        border-radius: 3px;
        overflow: hidden;
      }

      .progress-fill {
        height: 100%;
        border-radius: 3px;
        transition: width 0.6s ease;
      }

      .progress-fill[data-score='high'] {
        background: var(--success-color);
      }

      .progress-fill[data-score='medium'] {
        background: var(--warning-color);
      }

      .progress-fill[data-score='low'] {
        background: var(--danger-color);
      }

      .progress-text {
        font-size: 12px;
        font-weight: 600;
        color: var(--text-secondary);
        min-width: 36px;
        text-align: right;
      }

      /* ── Footer ── */
      .card-footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding-top: 6px;
        border-top: 1px solid var(--border-color);
      }

      .last-updated {
        font-size: 11px;
        color: var(--text-secondary);
      }

      .nabh-level {
        font-size: 11px;
        font-weight: 600;
        color: var(--primary-color);
        background: rgba(47, 107, 255, 0.08);
        padding: 2px 8px;
        border-radius: 4px;
      }

      @media (max-width: 768px) {
        .hospital-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class HospitalOverviewComponent {
  title = input<string>('Hospital Overview');
  hospitals = input.required<HospitalSnapshot[]>();

  sortedJourneys(journeys: JourneySnapshot[]): JourneySnapshot[] {
    return sortByCpFrameworkCodeKeys(
      journeys,
      (j) => j.frameworkCode,
      (j) => j.frameworkName,
    );
  }

  getScoreLevel(score: number): string {
    if (score >= 75) return 'high';
    if (score >= 60) return 'medium';
    return 'low';
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    if (diffHours < 1) return 'just now';
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays === 1) return 'yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
