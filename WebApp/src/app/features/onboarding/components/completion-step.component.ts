import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CompletionStepSummary } from '../models/completion-step-summary.model';

@Component({
  selector: 'app-completion-step',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="completion-container">
      <div class="completion-icon">
        <svg width="80" height="80" viewBox="0 0 80 80" fill="none">
          <circle cx="40" cy="40" r="40" fill="#D1FAE5" />
          <circle cx="40" cy="40" r="30" fill="#10B981" />
          <path
            d="M28 40L36 48L52 32"
            stroke="white"
            stroke-width="4"
            stroke-linecap="round"
            stroke-linejoin="round" />
        </svg>
      </div>

      <h1>Review &amp; submit</h1>
      <p class="completion-subtitle">
        Confirm your details below, then submit to finish onboarding and open the Compliance Command Center.
      </p>

      <div *ngIf="incompleteHint" class="banner warn">{{ incompleteHint }}</div>
      <div *ngIf="submitError" class="banner error">{{ submitError }}</div>

      <div class="review-section">
        <h4>Hospital</h4>
        <dl class="review-dl">
          <dt>Name</dt>
          <dd>{{ summary.hospitalName || '—' }}</dd>
          <dt>Type</dt>
          <dd>{{ summary.hospitalType || '—' }}</dd>
          <dt>Bed capacity</dt>
          <dd>{{ summary.bedCapacity || '—' }}</dd>
          <dt>Location</dt>
          <dd>{{ summary.geoLabel || '—' }}</dd>
          <dt>Departments</dt>
          <dd>
            <span *ngIf="summary.departmentNames.length === 0">—</span>
            <span *ngFor="let n of summary.departmentNames; trackBy: trackByIndex" class="dept-chip">{{ n }}</span>
          </dd>
        </dl>
      </div>

      <div class="summary-grid">
        <div class="summary-card">
          <div class="summary-icon">📋</div>
          <div class="summary-content">
            <h3>{{ summary.frameworksCount }}</h3>
            <p>Frameworks</p>
          </div>
        </div>
        <div class="summary-card">
          <div class="summary-icon">🏥</div>
          <div class="summary-content">
            <h3>{{ summary.departmentsCount }}</h3>
            <p>Departments</p>
          </div>
        </div>
        <div class="summary-card">
          <div class="summary-icon">📊</div>
          <div class="summary-content">
            <h3>{{ summary.initialScore }}%</h3>
            <p>Initial score</p>
          </div>
        </div>
      </div>

      <div class="frameworks-list">
        <h4>Frameworks</h4>
        <div class="framework-badges">
          <div *ngFor="let framework of summary.frameworks; trackBy: trackByIndex" class="framework-badge">
            {{ framework }}
          </div>
        </div>
      </div>

      <div class="frameworks-list">
        <h4>Assessment</h4>
        <ul class="assessment-list">
          <li *ngFor="let a of summary.assessmentAnswers; trackBy: trackByQuestion">
            <span class="q">{{ a.question }}</span>
            <span class="a">{{ formatAnswer(a.answer) }}</span>
          </li>
        </ul>
      </div>

      <div class="frameworks-list" *ngIf="summary.teamRows.length > 0">
        <h4>Team invitations</h4>
        <table class="review-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Department</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of summary.teamRows; trackBy: trackByIndex">
              <td>{{ row.name }}</td>
              <td>{{ row.email }}</td>
              <td>{{ row.role }}</td>
              <td>{{ row.departmentLabel }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="next-steps">
        <h4>What's next</h4>
        <ul class="steps-list">
          <li>
            <span>After submit, you'll land in the Compliance Command Center.</span>
          </li>
        </ul>
      </div>

      <button
        type="button"
        class="btn-primary"
        [disabled]="submitting || !canSubmit"
        (click)="onSubmit.emit()">
        <span *ngIf="!submitting">Submit &amp; go to Compliance Command Center</span>
        <span *ngIf="submitting">Submitting…</span>
        <svg *ngIf="!submitting" width="20" height="20" viewBox="0 0 20 20" fill="none">
          <path
            d="M7.5 15L12.5 10L7.5 5"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round" />
        </svg>
      </button>
    </div>
  `,
  styles: [
    `
      .completion-container {
        padding: 48px 32px;
        max-width: 720px;
        margin: 0 auto;
        text-align: center;
        animation: fadeIn 0.5s ease;
      }
      @keyframes fadeIn {
        from {
          opacity: 0;
          transform: translateY(20px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }
      .completion-icon {
        display: inline-flex;
        margin-bottom: 24px;
      }
      h1 {
        margin: 0 0 12px 0;
        font-size: var(--font-size-page-title);
        font-weight: 700;
        color: var(--color-text-primary, #111827);
      }
      [data-theme='dark'] h1 {
        color: var(--color-text-primary, #f9fafb);
      }
      .completion-subtitle {
        margin: 0 0 24px 0;
        font-size: var(--font-size-subtitle);
        line-height: 1.6;
        color: var(--color-text-secondary, #6b7280);
      }
      [data-theme='dark'] .completion-subtitle {
        color: var(--color-text-secondary, #9ca3af);
      }
      .banner {
        text-align: left;
        padding: 12px 14px;
        border-radius: 10px;
        margin-bottom: 16px;
        font-size: 13px;
      }
      .banner.warn {
        background: rgba(251, 191, 36, 0.15);
        border: 1px solid rgba(245, 158, 11, 0.4);
        color: #92400e;
      }
      .banner.error {
        background: rgba(239, 68, 68, 0.08);
        border: 1px solid rgba(239, 68, 68, 0.35);
        color: #b91c1c;
      }
      [data-theme='dark'] .banner.warn {
        color: #fcd34d;
      }
      [data-theme='dark'] .banner.error {
        color: #fca5a5;
      }
      .review-section {
        text-align: left;
        margin-bottom: 24px;
        padding: 20px;
        border: 1px solid var(--color-border, #e5e7eb);
        border-radius: 12px;
        background: var(--color-background, #f9fafb);
      }
      [data-theme='dark'] .review-section {
        background: #111827;
        border-color: #374151;
      }
      .review-section h4 {
        margin: 0 0 12px 0;
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--color-text-secondary, #6b7280);
      }
      .review-dl {
        display: grid;
        grid-template-columns: 140px 1fr;
        gap: 8px 16px;
        margin: 0;
        font-size: 14px;
      }
      .review-dl dt {
        color: var(--color-text-secondary, #6b7280);
        font-weight: 500;
      }
      .review-dl dd {
        margin: 0;
        color: var(--color-text-primary, #111827);
      }
      [data-theme='dark'] .review-dl dd {
        color: #f9fafb;
      }
      .dept-chip {
        display: inline-block;
        padding: 4px 10px;
        margin: 2px 4px 2px 0;
        border-radius: 8px;
        background: var(--color-primary-50);
        color: var(--color-primary-hover);
        font-size: 12px;
        font-weight: 600;
      }
      [data-theme='dark'] .dept-chip {
        background: var(--color-primary-background);
        color: var(--color-primary-text);
      }
      .summary-grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 16px;
        margin-bottom: 24px;
      }
      .summary-card {
        background: var(--color-background, #f9fafb);
        border: 1px solid var(--color-border, #e5e7eb);
        border-radius: 12px;
        padding: 20px 12px;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 8px;
      }
      [data-theme='dark'] .summary-card {
        background: #111827;
        border-color: #374151;
      }
      .summary-content h3 {
        margin: 0;
        font-size: 26px;
        font-weight: 700;
        color: var(--color-primary);
      }
      .summary-content p {
        margin: 4px 0 0 0;
        font-size: 12px;
        font-weight: 500;
        color: var(--color-text-secondary, #6b7280);
      }
      .frameworks-list {
        text-align: left;
        margin-bottom: 20px;
        padding: 20px;
        border: 1px solid var(--color-border, #e5e7eb);
        border-radius: 12px;
        background: var(--color-background, #f9fafb);
      }
      [data-theme='dark'] .frameworks-list {
        background: #111827;
        border-color: #374151;
      }
      .frameworks-list h4 {
        margin: 0 0 12px 0;
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--color-text-primary, #111827);
      }
      [data-theme='dark'] .frameworks-list h4 {
        color: #f9fafb;
      }
      .framework-badges {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .framework-badge {
        padding: 8px 14px;
        background: var(--color-primary-50);
        color: var(--color-primary);
        border: 1px solid var(--color-primary-100);
        border-radius: 8px;
        font-size: 13px;
        font-weight: 600;
      }
      [data-theme='dark'] .framework-badge {
        background: var(--color-primary-background);
        color: var(--color-primary-text);
        border-color: var(--color-primary-active);
      }
      .assessment-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 10px;
      }
      .assessment-list li {
        display: flex;
        flex-direction: column;
        gap: 4px;
        font-size: 13px;
        padding-bottom: 10px;
        border-bottom: 1px solid var(--color-border, #e5e7eb);
      }
      [data-theme='dark'] .assessment-list li {
        border-color: #374151;
      }
      .assessment-list .q {
        color: var(--color-text-primary, #111827);
        font-weight: 500;
      }
      .assessment-list .a {
        color: var(--color-text-secondary, #6b7280);
        text-transform: capitalize;
      }
      .review-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;
      }
      .review-table th,
      .review-table td {
        padding: 8px 10px;
        border-bottom: 1px solid var(--color-border, #e5e7eb);
        text-align: left;
      }
      [data-theme='dark'] .review-table th,
      [data-theme='dark'] .review-table td {
        border-color: #374151;
      }
      .next-steps {
        text-align: left;
        margin-bottom: 24px;
        padding: 16px;
        border-radius: 12px;
        border: 1px solid var(--color-border, #e5e7eb);
      }
      [data-theme='dark'] .next-steps {
        border-color: #374151;
      }
      .next-steps h4 {
        margin: 0 0 8px 0;
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
      }
      .steps-list {
        list-style: none;
        padding: 0;
        margin: 0;
        font-size: 14px;
        color: var(--color-text-secondary, #6b7280);
      }
      .btn-primary {
        padding: 16px 28px;
        background: var(--color-primary);
        color: white;
        border: none;
        border-radius: 8px;
        font-size: 15px;
        font-weight: 600;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        width: 100%;
        max-width: 420px;
      }
      .btn-primary:hover:not(:disabled) {
        background: var(--color-primary-hover);
      }
      .btn-primary:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      @media (max-width: 768px) {
        .summary-grid {
          grid-template-columns: 1fr;
        }
        .review-dl {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class CompletionStepComponent {
  @Input() summary: CompletionStepSummary = {
    frameworksCount: 0,
    departmentsCount: 0,
    initialScore: 0,
    frameworks: [],
    hospitalName: '',
    hospitalType: '',
    bedCapacity: '',
    geoLabel: '',
    departmentNames: [],
    assessmentAnswers: [],
    teamRows: [],
  };
  @Input() submitting = false;
  @Input() submitError: string | null = null;
  @Input() canSubmit = true;
  @Input() incompleteHint: string | null = null;

  @Output() onSubmit = new EventEmitter<void>();

  trackByIndex(i: number): number {
    return i;
  }

  trackByQuestion(_i: number, a: { questionId: string }): string {
    return a.questionId;
  }

  formatAnswer(a: string): string {
    if (a === 'not_sure') return 'Not sure';
    return a || '—';
  }
}
