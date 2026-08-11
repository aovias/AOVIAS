import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssessmentQuestion, AssessmentAnswer } from '../models/onboarding-state.model';
import { OnboardingAssessmentApiService, CORE_ASSESSMENT_MAX_QUESTIONS } from '../services/onboarding-assessment-api.service';

@Component({
  selector: 'app-assessment-step',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="assessment-container">
      <div class="assessment-header">
        <h2>AI Compliance Assessment</h2>
        <p>Help us understand your current compliance readiness</p>
        <div class="progress-bar" *ngIf="!loading() && questions.length > 0">
          <div class="progress-fill" [style.width.%]="getProgress()"></div>
        </div>
        <span class="progress-text" *ngIf="!loading() && questions.length > 0">
          {{ currentQuestionIndex + 1 }} of {{ questions.length }}
        </span>
      </div>

      <div *ngIf="loading()" class="loading-state">Loading assessment questions…</div>

      <div *ngIf="!loading() && error()" class="error-state">
        <div class="error-message">{{ error() }}</div>
        <button type="button" class="btn-secondary" (click)="loadCoreAssessment()">Retry</button>
      </div>

      <div *ngIf="!loading() && !error() && questions.length === 0" class="empty-state">
        No assessment questions are available for the selected frameworks.
      </div>

      <div class="question-card" *ngIf="!loading() && !error() && currentQuestion">
        <div class="question-header">
          <div class="question-category">{{ currentQuestion.category }}</div>
          <span *ngIf="currentQuestion.isMandatory" class="mandatory-badge" title="Critical — Non-compliance triggers immediate gap flag">
            <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
              <circle cx="6" cy="6" r="5.5" stroke="currentColor" stroke-width="1"/>
              <path d="M6 3v3.5M6 8.5v.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            Mandatory
          </span>
        </div>
        <h3>{{ currentQuestion.question }}</h3>
        <p class="help-text" *ngIf="currentQuestion.helpText">{{ currentQuestion.helpText }}</p>

        <div class="answer-options answer-options-4">
          <button
            class="answer-btn compliant"
            [class.selected]="getCurrentAnswer() === 'compliant'"
            (click)="selectAnswer('compliant')">
            <div class="answer-icon">✓</div>
            <span>Compliant</span>
          </button>

          <button
            class="answer-btn partial"
            [class.selected]="getCurrentAnswer() === 'partially_compliant'"
            (click)="selectAnswer('partially_compliant')">
            <div class="answer-icon">~</div>
            <span>Partially Compliant</span>
          </button>

          <button
            class="answer-btn non-compliant"
            [class.selected]="getCurrentAnswer() === 'non_compliant'"
            (click)="selectAnswer('non_compliant')">
            <div class="answer-icon">✗</div>
            <span>Non-Compliant</span>
          </button>

          <button
            class="answer-btn not-applicable"
            [class.selected]="getCurrentAnswer() === 'not_applicable'"
            (click)="selectAnswer('not_applicable')">
            <div class="answer-icon">—</div>
            <span>Not Applicable</span>
          </button>
        </div>

        <div class="navigation-buttons">
          <button
            class="btn-nav"
            [disabled]="currentQuestionIndex === 0"
            (click)="previousQuestion()">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M10 12L6 8L10 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Previous
          </button>

          <button
            class="btn-nav"
            *ngIf="currentQuestionIndex < questions.length - 1"
            [disabled]="!getCurrentAnswer()"
            (click)="nextQuestion()">
            Next
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M6 12L10 8L6 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </button>
        </div>
      </div>

      <div class="assessment-actions">
        <button class="btn-secondary" (click)="onBack.emit()">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M10 12L6 8L10 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Back
        </button>
        <button
          class="btn-primary"
          [disabled]="!allQuestionsAnswered() || questions.length === 0"
          (click)="handleNext()">
          Complete Assessment
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M6 12L10 8L6 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .assessment-container {
      padding: 32px;
      max-width: 700px;
      margin: 0 auto;
    }

    .loading-state,
    .empty-state {
      text-align: center;
      padding: 48px 24px;
      color: var(--color-text-secondary, #6b7280);
      font-size: 14px;
    }

    .error-state {
      text-align: center;
      padding: 32px 24px;
    }

    .error-message {
      color: var(--danger-color, #ef4444);
      margin-bottom: 16px;
      font-size: 14px;
    }

    .assessment-header {
      margin-bottom: 32px;
      text-align: center;
    }

    .assessment-header h2 {
      margin: 0 0 8px 0;
      font-size: var(--font-size-section-title);
      font-weight: 700;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .assessment-header h2 {
      color: var(--color-text-primary, #f9fafb);
    }

    .assessment-header p {
      margin: 0 0 24px 0;
      font-size: 14px;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .assessment-header p {
      color: var(--color-text-secondary, #9ca3af);
    }

    .progress-bar {
      height: 8px;
      background: var(--color-background, #f3f4f6);
      border-radius: 4px;
      overflow: hidden;
      margin-bottom: 8px;
    }

    [data-theme="dark"] .progress-bar {
      background: var(--color-background, #374151);
    }

    .progress-fill {
      height: 100%;
      background: linear-gradient(90deg, var(--color-primary) 0%, #3b82f6 100%);
      transition: width 0.3s ease;
    }

    .progress-text {
      font-size: 13px;
      font-weight: 600;
      color: var(--color-text-secondary, #6b7280);
    }

    .question-card {
      background: var(--card-bg, #ffffff);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 16px;
      padding: 32px;
      margin-bottom: 24px;
      animation: fadeIn 0.4s ease;
      box-shadow: var(--shadow-md, 0 4px 6px rgba(0, 0, 0, 0.07));
    }

    [data-theme="dark"] .question-card {
      background: var(--card-bg, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.06));
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .question-category {
      display: inline-block;
      padding: 6px 14px;
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
      color: var(--primary-color, var(--color-primary));
      font-size: 11px;
      font-weight: 700;
      border-radius: 8px;
      margin-bottom: 16px;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      border: 1px solid var(--primary-color, color-mix(in srgb, var(--color-primary) 20%, transparent));
    }

    [data-theme="dark"] .question-category {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 12%, transparent));
      color: var(--primary-color, var(--color-primary));
      border-color: var(--primary-color, color-mix(in srgb, var(--color-primary) 30%, transparent));
    }

    .question-card h3 {
      margin: 0 0 8px 0;
      font-size: 20px;
      font-weight: 600;
      line-height: 1.4;
      color: var(--color-text-primary, #111827);
    }

    .help-text {
      margin: 0 0 24px 0;
      font-size: 14px;
      line-height: 1.5;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .question-card h3 {
      color: var(--color-text-primary, #f9fafb);
    }

    [data-theme="dark"] .help-text {
      color: var(--color-text-secondary, #9ca3af);
    }

    .question-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }

    .question-header .question-category {
      margin-bottom: 0;
    }

    .mandatory-badge {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 4px 10px;
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
      font-size: 11px;
      font-weight: 700;
      border-radius: 8px;
      border: 1px solid rgba(239, 68, 68, 0.25);
      text-transform: uppercase;
      letter-spacing: 0.6px;
    }

    [data-theme="dark"] .mandatory-badge {
      background: rgba(248, 113, 113, 0.12);
      color: #f87171;
      border-color: rgba(248, 113, 113, 0.3);
    }

    .answer-options {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 12px;
      margin-bottom: 24px;
    }

    .answer-options-4 {
      grid-template-columns: repeat(4, 1fr);
    }

    @media (max-width: 900px) {
      .answer-options-4 {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    .answer-btn {
      padding: 22px 18px;
      background: var(--card-bg, #ffffff);
      border: 2px solid var(--border-color, #e5e7eb);
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
      font-size: 14px;
      font-weight: 600;
      color: var(--text-secondary, #6b7280);
      position: relative;
      overflow: hidden;
    }

    [data-theme="dark"] .answer-btn {
      background: var(--bg-tertiary, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.08));
      color: var(--text-secondary, #94a3b8);
    }

    .answer-btn::before {
      content: '';
      position: absolute;
      inset: 0;
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 5%, transparent));
      opacity: 0;
      transition: opacity 0.25s ease;
    }

    .answer-btn:hover {
      transform: translateY(-3px);
      box-shadow: var(--shadow-md, 0 4px 12px rgba(0, 0, 0, 0.1));
      border-color: var(--primary-color, var(--color-primary));
    }

    .answer-btn:hover::before {
      opacity: 1;
    }

    [data-theme="dark"] .answer-btn:hover {
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.4);
      border-color: var(--primary-color, var(--color-primary));
    }

    .answer-btn.selected {
      border-color: var(--primary-color, var(--color-primary));
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
      color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
    }

    [data-theme="dark"] .answer-btn.selected {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 12%, transparent));
      color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
    }

    /* Legacy yes/no kept for backward compat */
    .answer-btn.yes.selected {
      border-color: var(--success-color, #10b981);
      background: rgba(16, 185, 129, 0.1);
      color: var(--success-color, #10b981);
      box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
    }

    [data-theme="dark"] .answer-btn.yes.selected {
      background: rgba(52, 211, 153, 0.12);
      color: var(--success-color, #34d399);
      border-color: var(--success-color, #34d399);
      box-shadow: 0 0 0 3px rgba(52, 211, 153, 0.1);
    }

    .answer-btn.no.selected {
      border-color: var(--danger-color, #ef4444);
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color, #ef4444);
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }

    [data-theme="dark"] .answer-btn.no.selected {
      background: rgba(248, 113, 113, 0.12);
      color: var(--danger-color, #f87171);
      border-color: var(--danger-color, #f87171);
      box-shadow: 0 0 0 3px rgba(248, 113, 113, 0.1);
    }

    /* Core 4-answer button states */
    .answer-btn.compliant.selected {
      border-color: #10b981;
      background: rgba(16, 185, 129, 0.1);
      color: #10b981;
      box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
    }

    [data-theme="dark"] .answer-btn.compliant.selected {
      background: rgba(52, 211, 153, 0.12);
      color: #34d399;
      border-color: #34d399;
      box-shadow: 0 0 0 3px rgba(52, 211, 153, 0.1);
    }

    .answer-btn.partial.selected {
      border-color: #f59e0b;
      background: rgba(245, 158, 11, 0.1);
      color: #f59e0b;
      box-shadow: 0 0 0 3px rgba(245, 158, 11, 0.1);
    }

    [data-theme="dark"] .answer-btn.partial.selected {
      background: rgba(251, 191, 36, 0.12);
      color: #fbbf24;
      border-color: #fbbf24;
      box-shadow: 0 0 0 3px rgba(251, 191, 36, 0.1);
    }

    .answer-btn.non-compliant.selected {
      border-color: #ef4444;
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }

    [data-theme="dark"] .answer-btn.non-compliant.selected {
      background: rgba(248, 113, 113, 0.12);
      color: #f87171;
      border-color: #f87171;
      box-shadow: 0 0 0 3px rgba(248, 113, 113, 0.1);
    }

    .answer-btn.not-applicable.selected {
      border-color: #6b7280;
      background: rgba(107, 114, 128, 0.1);
      color: #6b7280;
      box-shadow: 0 0 0 3px rgba(107, 114, 128, 0.1);
    }

    [data-theme="dark"] .answer-btn.not-applicable.selected {
      background: rgba(156, 163, 175, 0.12);
      color: #9ca3af;
      border-color: #9ca3af;
      box-shadow: 0 0 0 3px rgba(156, 163, 175, 0.1);
    }

    .answer-icon {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      background: var(--bg-tertiary, #f3f4f6);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      font-weight: bold;
      transition: all 0.25s ease;
      position: relative;
      z-index: 1;
    }

    [data-theme="dark"] .answer-icon {
      background: var(--bg-tertiary, #0f172a);
    }

    .answer-btn.selected .answer-icon {
      background: currentColor;
      color: white;
      transform: scale(1.1);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }

    .navigation-buttons {
      display: flex;
      justify-content: space-between;
      gap: 12px;
    }

    .btn-nav {
      padding: 10px 20px;
      background: white;
      border: 1px solid var(--color-border, #e5e7eb);
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      color: var(--color-text-secondary, #6b7280);
      cursor: pointer;
      transition: all 0.2s ease;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }

    [data-theme="dark"] .btn-nav {
      background: #1f2937;
      border-color: var(--color-border, #4b5563);
      color: var(--color-text-secondary, #9ca3af);
    }

    .btn-nav:hover:not(:disabled) {
      background: var(--color-background, #f9fafb);
      border-color: var(--color-primary);
      color: var(--color-primary);
    }

    [data-theme="dark"] .btn-nav:hover:not(:disabled) {
      background: var(--color-background, #111827);
    }

    .btn-nav:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }

    .assessment-actions {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      padding-top: 24px;
      border-top: 1px solid var(--color-border, #e5e7eb);
    }

    [data-theme="dark"] .assessment-actions {
      border-top-color: var(--color-border, #374151);
    }

    .btn-primary,
    .btn-secondary {
      padding: 12px 24px;
      border-radius: 10px;
      font-size: 14px;
      font-weight: 600;
      border: none;
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      display: inline-flex;
      align-items: center;
      gap: 8px;
      font-family: inherit;
    }

    .btn-primary {
      background: var(--primary-color, var(--color-primary));
      color: white;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    [data-theme="dark"] .btn-primary {
      background: var(--primary-color, var(--color-primary));
      color: #0f172a;
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--primary-hover, var(--color-primary-hover));
      transform: translateY(-2px);
      box-shadow: 0 6px 16px color-mix(in srgb, var(--color-primary) 35%, transparent);
    }

    [data-theme="dark"] .btn-primary:hover:not(:disabled) {
      background: var(--primary-hover, #3b82f6);
      box-shadow: 0 6px 20px color-mix(in srgb, var(--color-primary) 40%, transparent);
    }

    .btn-primary:active:not(:disabled) {
      transform: translateY(-1px);
    }

    .btn-primary:disabled {
      background: var(--text-muted, #9ca3af);
      cursor: not-allowed;
      opacity: 0.5;
      box-shadow: none;
    }

    .btn-secondary {
      background: var(--card-bg, #ffffff);
      color: var(--text-secondary, #6b7280);
      border: 1.5px solid var(--border-color, #e5e7eb);
    }

    [data-theme="dark"] .btn-secondary {
      background: var(--bg-tertiary, #1e293b);
      color: var(--text-secondary, #94a3b8);
      border-color: var(--border-color, rgba(255, 255, 255, 0.1));
    }

    .btn-secondary:hover {
      background: var(--hover-bg, #f3f4f6);
      border-color: var(--primary-color, var(--color-primary));
      color: var(--primary-color, var(--color-primary));
      transform: translateY(-1px);
    }

    [data-theme="dark"] .btn-secondary:hover {
      background: var(--hover-bg, rgba(255, 255, 255, 0.05));
      border-color: var(--primary-color, var(--color-primary));
      color: var(--primary-color, var(--color-primary));
    }

    .btn-secondary:active {
      transform: translateY(0);
    }

    @media (max-width: 768px) {
      .assessment-container {
        padding: 24px 16px;
      }

      .question-card {
        padding: 24px 20px;
      }

      .answer-options {
        grid-template-columns: 1fr;
      }

      .assessment-actions {
        flex-direction: column-reverse;
      }

      .btn-primary,
      .btn-secondary {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class AssessmentStepComponent implements OnInit {
  private readonly assessmentApi = inject(OnboardingAssessmentApiService);

  @Input() answers: AssessmentAnswer[] = [];
  /** Selected framework codes from wizard step 3 (`FrameworkLookup.code`). */
  @Input() frameworkCodes: string[] = [];
  @Output() onNext = new EventEmitter<AssessmentAnswer[]>();
  @Output() onBack = new EventEmitter<void>();
  @Output() questionsLoaded = new EventEmitter<number>();

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  questions: AssessmentQuestion[] = [];
  currentQuestionIndex = 0;
  currentQuestion: AssessmentQuestion | null = null;

  ngOnInit(): void {
    this.loadCoreAssessment();
  }

  loadCoreAssessment(): void {
    this.loading.set(true);
    this.error.set(null);
    this.answers = [];
    this.assessmentApi.loadCoreQuestions(this.frameworkCodes).subscribe({
      next: (questions) => {
        if (questions.length > CORE_ASSESSMENT_MAX_QUESTIONS) {
          this.error.set(
            `Unexpected question count (${questions.length}). Clear browser cache and reload — Core assessment should load at most ${CORE_ASSESSMENT_MAX_QUESTIONS} questions via Gateway.`,
          );
          this.questions = [];
          this.currentQuestion = null;
          this.questionsLoaded.emit(0);
          this.loading.set(false);
          return;
        }

        this.questions = questions;
        this.currentQuestionIndex = 0;
        this.currentQuestion = questions[0] ?? null;
        this.syncAnswersWithQuestions();
        this.questionsLoaded.emit(questions.length);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load assessment questions. Please try again.');
        this.questions = [];
        this.currentQuestion = null;
        this.questionsLoaded.emit(0);
        this.loading.set(false);
      },
    });
  }

  private syncAnswersWithQuestions(): void {
    const byId = new Map(this.answers.map((a) => [a.questionId, a]));
    this.answers = this.questions.map((q) => {
      const existing = byId.get(q.id);
      if (existing) {
        return { ...existing, question: q.question, objectiveElementCode: q.objectiveElementCode };
      }
      return {
        questionId: q.id,
        question: q.question,
        answer: '' as AssessmentAnswer['answer'],
        objectiveElementCode: q.objectiveElementCode,
      };
    });
  }

  getCurrentAnswer(): AssessmentAnswer['answer'] | '' {
    const answer = this.answers.find(a => a.questionId === this.currentQuestion?.id);
    return answer?.answer || '';
  }

  selectAnswer(answer: AssessmentAnswer['answer']): void {
    const answerIndex = this.answers.findIndex(a => a.questionId === this.currentQuestion?.id);
    if (answerIndex > -1) {
      this.answers[answerIndex].answer = answer;
    }
  }

  nextQuestion(): void {
    if (this.currentQuestionIndex < this.questions.length - 1) {
      this.currentQuestionIndex++;
      this.currentQuestion = this.questions[this.currentQuestionIndex];
    }
  }

  previousQuestion(): void {
    if (this.currentQuestionIndex > 0) {
      this.currentQuestionIndex--;
      this.currentQuestion = this.questions[this.currentQuestionIndex];
    }
  }

  getProgress(): number {
    if (this.questions.length === 0) {
      return 0;
    }
    const answeredCount = this.answers.filter((a) => a.answer).length;
    return (answeredCount / this.questions.length) * 100;
  }

  allQuestionsAnswered(): boolean {
    if (this.questions.length === 0) {
      return false;
    }
    return this.answers.every((a) => a.answer);
  }

  handleNext(): void {
    if (this.allQuestionsAnswered()) {
      this.onNext.emit(this.answers);
    }
  }
}
