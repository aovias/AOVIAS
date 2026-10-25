import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FrameworkLookup } from '../../../shared/models/lookup.models';
import { CommonLookupService } from '../../../shared/services/common-lookup.service';
import { FrameworkCardIconComponent } from '../../../shared/ui/framework-card-icon/framework-card-icon.component';

@Component({
  selector: 'app-framework-selector-step',
  standalone: true,
  imports: [CommonModule, FrameworkCardIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="framework-container">
      <div class="framework-header">
        <h2>Select Compliance Frameworks</h2>
        <p>Choose the accreditation standards your hospital will follow</p>
      </div>

      <div *ngIf="frameworksLoading" class="loading-state">
        Loading frameworks…
      </div>

      <div *ngIf="frameworksError" class="error-state">
        <div class="error-message">{{ frameworksError }}</div>
        <button type="button" class="btn-secondary" (click)="retryLoadFrameworks()">Retry</button>
      </div>

      <div
        *ngIf="!frameworksLoading && !frameworksError && frameworks.length === 0"
        class="empty-state">
        No frameworks available.
      </div>

      <div class="framework-grid" *ngIf="!frameworksLoading && !frameworksError && frameworks.length > 0">
        <div
          *ngFor="let framework of frameworks; trackBy: trackByCode"
          class="framework-card"
          [class.selected]="isSelected(framework.code)"
          (click)="toggleFramework(framework.code)">

          <div class="framework-icon">
            <app-framework-card-icon [iconKey]="framework.iconKey" />
          </div>

          <h3>{{ framework.name }}</h3>
          <p *ngIf="framework.authority" class="framework-authority">{{ framework.authority }}</p>

          <div class="framework-check">
            <div class="checkbox" [class.checked]="isSelected(framework.code)">
              <svg *ngIf="isSelected(framework.code)" width="16" height="16" viewBox="0 0 16 16" fill="none">
                <path d="M13.5 4L6 11.5L2.5 8" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span>{{ isSelected(framework.code) ? 'Selected' : 'Select' }}</span>
          </div>
        </div>
      </div>

      <div class="framework-actions">
        <button class="btn-secondary" (click)="onBack.emit()">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M10 12L6 8L10 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Back
        </button>
        <button
          class="btn-primary"
          [disabled]="continueDisabled"
          (click)="handleNext()">
          Continue with {{ localSelectedCodes.length }} framework{{ localSelectedCodes.length !== 1 ? 's' : '' }}
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M6 12L10 8L6 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .loading-state,
    .empty-state {
      padding: 12px 0;
      color: var(--color-text-secondary, #6b7280);
      font-size: 13px;
      margin-bottom: 16px;
    }

    [data-theme="dark"] .loading-state,
    [data-theme="dark"] .empty-state {
      color: var(--color-text-secondary, #9ca3af);
    }

    .error-state {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 12px 14px;
      border: 1px solid rgba(239, 68, 68, 0.35);
      background: rgba(239, 68, 68, 0.06);
      border-radius: 10px;
      margin-bottom: 16px;
    }

    .error-message {
      color: #b91c1c;
      font-size: 13px;
      font-weight: 500;
    }

    [data-theme="dark"] .error-message {
      color: #fca5a5;
    }

    .framework-container {
      padding: 32px;
      max-width: 1000px;
      margin: 0 auto;
    }

    .framework-header {
      margin-bottom: 32px;
      text-align: center;
    }

    .framework-header h2 {
      margin: 0 0 8px 0;
      font-size: var(--font-size-section-title);
      font-weight: 700;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .framework-header h2 {
      color: var(--color-text-primary, #f9fafb);
    }

    .framework-header p {
      margin: 0;
      font-size: 14px;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .framework-header p {
      color: var(--color-text-secondary, #9ca3af);
    }

    .framework-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
      margin-bottom: 32px;
    }

    .framework-card {
      position: relative;
      background: var(--card-bg, #ffffff);
      border: 2px solid var(--border-color, #e5e7eb);
      border-radius: 14px;
      padding: 24px;
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      display: flex;
      flex-direction: column;
      gap: 12px;
      overflow: hidden;
    }

    [data-theme="dark"] .framework-card {
      background: var(--card-bg, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.08));
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
    }

    .framework-card:hover {
      transform: translateY(-4px);
      box-shadow: var(--shadow-lg, 0 10px 25px rgba(0, 0, 0, 0.12));
      border-color: var(--primary-color, var(--color-primary));
    }

    [data-theme="dark"] .framework-card:hover {
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
      border-color: var(--primary-color, var(--color-primary));
    }

    .framework-card.selected {
      border-color: var(--primary-color, var(--color-primary));
      background: var(--card-bg, #ffffff);
      box-shadow: 0 0 0 4px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent)), 0 8px 20px color-mix(in srgb, var(--color-primary) 15%, transparent);
    }

    [data-theme="dark"] .framework-card.selected {
      background: var(--card-bg, #1e293b);
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 4px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 12%, transparent)), 0 8px 20px color-mix(in srgb, var(--color-primary) 20%, transparent);
    }

    .framework-icon {
      width: 72px;
      height: 72px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary, #f3f4f6);
      border-radius: 12px;
      margin-bottom: 4px;
      border: 1px solid var(--border-color, #e5e7eb);
      transition: all 0.25s ease;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .framework-icon {
      background: var(--bg-tertiary, #0f172a);
      border-color: var(--border-color, rgba(255, 255, 255, 0.08));
      color: var(--color-text-primary, #f9fafb);
    }

    .framework-card.selected .framework-icon {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
      border-color: var(--primary-color, var(--color-primary));
    }

    [data-theme="dark"] .framework-card.selected .framework-icon {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent));
      border-color: var(--primary-color, var(--color-primary));
    }

    .framework-card h3 {
      margin: 0;
      font-size: 20px;
      font-weight: 700;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .framework-card h3 {
      color: var(--color-text-primary, #f9fafb);
    }

    .framework-authority {
      margin: 0;
      font-size: 13px;
      line-height: 1.5;
      color: var(--color-text-secondary, #6b7280);
      flex: 1;
    }

    [data-theme="dark"] .framework-authority {
      color: var(--color-text-secondary, #9ca3af);
    }

    .framework-check {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 8px;
      padding-top: 16px;
      border-top: 1px solid var(--color-border, #e5e7eb);
      font-size: 14px;
      font-weight: 600;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .framework-check {
      border-top-color: var(--color-border, #4b5563);
      color: var(--color-text-secondary, #9ca3af);
    }

    .framework-card.selected .framework-check {
      color: var(--color-primary);
    }

    .checkbox {
      width: 20px;
      height: 20px;
      border: 2px solid var(--color-border, #d1d5db);
      border-radius: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }

    [data-theme="dark"] .checkbox {
      border-color: var(--color-border, #4b5563);
    }

    .checkbox.checked {
      background: var(--color-primary);
      border-color: var(--color-primary);
    }

    .framework-actions {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      padding-top: 24px;
      border-top: 1px solid var(--color-border, #e5e7eb);
    }

    [data-theme="dark"] .framework-actions {
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
      .framework-container {
        padding: 24px 16px;
      }

      .framework-grid {
        grid-template-columns: 1fr;
      }

      .framework-actions {
        flex-direction: column-reverse;
      }

      .btn-primary,
      .btn-secondary {
        width: 100%;
        justify-content: center;
      }
    }
  `],
})
export class FrameworkSelectorStepComponent implements OnInit, OnChanges {
  private readonly lookup = inject(CommonLookupService);
  private readonly cdr = inject(ChangeDetectorRef);

  /** Parent-bound list of selected framework `code` values (API `code` field). */
  @Input() selectedFrameworks: string[] = [];
  @Output() onNext = new EventEmitter<{ frameworkCodes: string[]; displayNames: string[] }>();
  @Output() onBack = new EventEmitter<void>();

  frameworks: FrameworkLookup[] = [];
  frameworksLoading = false;
  frameworksError: string | null = null;
  localSelectedCodes: string[] = [];

  ngOnInit(): void {
    this.syncLocalFromInput();
    this.loadFrameworks();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedFrameworks']) {
      this.syncLocalFromInput();
    }
  }

  trackByCode(_index: number, row: FrameworkLookup): string {
    return row.code;
  }

  private syncLocalFromInput(): void {
    this.localSelectedCodes = [...(this.selectedFrameworks ?? [])];
  }

  get continueDisabled(): boolean {
    return (
      this.frameworksLoading ||
      !!this.frameworksError ||
      this.frameworks.length === 0 ||
      this.localSelectedCodes.length === 0
    );
  }

  loadFrameworks(): void {
    this.frameworksLoading = true;
    this.frameworksError = null;
    this.lookup.loadFrameworks().subscribe({
      next: (rows) => {
        this.frameworks = rows ?? [];
        this.frameworksLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.frameworksLoading = false;
        this.frameworksError = 'Could not load frameworks. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  retryLoadFrameworks(): void {
    this.frameworksLoading = true;
    this.frameworksError = null;
    this.lookup.refreshFrameworks().subscribe({
      next: (rows) => {
        this.frameworks = rows ?? [];
        this.frameworksLoading = false;
        this.frameworksError = null;
        this.cdr.markForCheck();
      },
      error: () => {
        this.frameworksLoading = false;
        this.frameworksError = 'Could not load frameworks. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  isSelected(code: string): boolean {
    return this.localSelectedCodes.includes(code);
  }

  toggleFramework(code: string): void {
    if (this.frameworksLoading || this.frameworksError) {
      return;
    }
    const index = this.localSelectedCodes.indexOf(code);
    if (index > -1) {
      this.localSelectedCodes.splice(index, 1);
    } else {
      this.localSelectedCodes.push(code);
    }
    this.cdr.markForCheck();
  }

  handleNext(): void {
    if (this.continueDisabled) {
      return;
    }
    const displayNames = this.localSelectedCodes.map((c) => {
      const row = this.frameworks.find((f) => f.code === c);
      return row?.name ?? 'Framework';
    });
    this.onNext.emit({
      frameworkCodes: [...this.localSelectedCodes],
      displayNames,
    });
  }
}
