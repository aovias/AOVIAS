import { Component, ChangeDetectionStrategy, Inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { A11yModule } from '@angular/cdk/a11y';

export interface HospitalSelectorDialogData {
  hospitals: HospitalSelectorItem[];
  hospitalJourneys: Record<string, FrameworkSelectorItem[]>;
  selectedHospitalId: string | null;
  selectedJourneyId: string | null;
}

export interface HospitalSelectorItem {
  id: string;
  name: string;
  organizationName: string;
  isOnboarded: boolean;
}

/**
 * A selectable row in the framework column.
 *
 * `id` is the **journey** id — a hospital enrolls in a framework via a journey, and that is what
 * the dialog selects. `name` / `code` describe the framework behind it, which is what the user
 * recognises. See `ComplianceContextService` for why the two ids must not be interchanged.
 */
export interface FrameworkSelectorItem {
  /** Journey id, not framework id. */
  id: string;
  name: string;
  code: string;
}

export interface HospitalFrameworkSelection {
  hospitalId: string;
  /**
   * The selected **journey** id. Callers that need the framework id must resolve it via
   * `OrganizationService.getFrameworkIdForJourney()` — this field was previously named
   * `frameworkId` while carrying a journey id, which is what caused every framework-filtered
   * audit query to silently match nothing.
   */
  journeyId: string;
}

@Component({
  selector: 'app-hospital-selector-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, A11yModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="hospital-selector-dialog" data-testid="context-dialog">
      <div class="dialog-header">
        <div class="dialog-header-text">
          <h2>Select Hospital & Framework</h2>
          <p class="dialog-subtitle">Choose a hospital and framework to continue</p>
        </div>
        <button class="dialog-close" data-testid="context-close" (click)="onClose()" title="Close">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M18 6L6 18M6 6l12 12" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>

      <div class="dialog-content">
        <!-- Hospital Section -->
        <div class="section-label">Hospital</div>
        <div class="hospital-list" data-testid="context-hospital-list">
          @for (hospital of data.hospitals; track hospital.id) {
            <button
              class="hospital-item"
              data-testid="context-hospital-option"
              [attr.data-hospital-id]="hospital.id"
              [attr.data-selected]="selectedHospitalId() === hospital.id"
              [attr.data-onboarded]="hospital.isOnboarded"
              [class.selected]="selectedHospitalId() === hospital.id"
              [class.disabled]="!hospital.isOnboarded"
              (click)="selectHospital(hospital)"
              [disabled]="!hospital.isOnboarded"
            >
              <div class="item-radio">
                <div class="radio-outer">
                  @if (selectedHospitalId() === hospital.id) {
                    <div class="radio-inner"></div>
                  }
                </div>
              </div>
              <div class="hospital-item-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <path d="M9 22V12h6v10" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="item-content">
                <span class="item-name">{{ hospital.name }}</span>
                <span class="item-sub">{{ hospital.organizationName }}</span>
              </div>
              @if (selectedHospitalId() === hospital.id) {
                <div class="item-check">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path d="M20 6L9 17l-5-5" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
              }
              @if (!hospital.isOnboarded) {
                <span class="item-badge">Not Onboarded</span>
              }
            </button>
          }
        </div>

        <!-- Divider -->
        <div class="content-divider"></div>

        <!-- Framework Section -->
        <div class="section-label">Framework</div>
        <div class="framework-list" data-testid="context-framework-list">
          @for (framework of availableFrameworks(); track framework.id) {
            <button
              class="framework-item"
              data-testid="context-framework-option"
              [attr.data-journey-id]="framework.id"
              [attr.data-framework-code]="framework.code"
              [attr.data-selected]="selectedJourneyId() === framework.id"
              [class.selected]="selectedJourneyId() === framework.id"
              (click)="selectFramework(framework)"
            >
              <div class="item-radio">
                <div class="radio-outer">
                  @if (selectedJourneyId() === framework.id) {
                    <div class="radio-inner"></div>
                  }
                </div>
              </div>
              <div class="item-content">
                <span class="item-name">{{ framework.name }}</span>
                <span class="item-sub">{{ framework.code }}</span>
              </div>
              @if (selectedJourneyId() === framework.id) {
                <div class="item-check">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path d="M20 6L9 17l-5-5" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
              }
            </button>
          }
          @if (availableFrameworks().length === 0 && selectedHospitalId()) {
            <div class="empty-frameworks" data-testid="context-framework-empty">
              <span>No frameworks available for this hospital</span>
            </div>
          }
        </div>
      </div>

      <div class="dialog-actions">
        <button class="btn-cancel" data-testid="context-cancel" (click)="onCancel()">Cancel</button>
        <button
          class="btn-confirm"
          data-testid="context-confirm"
          [disabled]="!selectedHospitalId() || !selectedJourneyId() || (selectedHospitalId() === data.selectedHospitalId && selectedJourneyId() === data.selectedJourneyId)"
          (click)="onConfirm()"
          cdkFocusInitial
        >
          Confirm Selection
        </button>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .hospital-selector-dialog {
      min-width: 480px;
      max-width: 560px;
      max-height: 80vh;
      display: flex;
      flex-direction: column;
      background: var(--card-bg);
      border-radius: 16px;
      overflow: hidden;
    }

    /* Header */
    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 24px 24px 16px;
      border-bottom: 1px solid var(--border-color);
    }

    .dialog-header-text {
      flex: 1;
      min-width: 0;
    }

    .dialog-header h2 {
      font-size: var(--font-size-section-title);
      font-weight: 700;
      color: var(--text-primary);
      margin: 0;
    }

    .dialog-subtitle {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 4px 0 0;
    }

    .dialog-close {
      width: 36px;
      height: 36px;
      border-radius: 8px;
      border: none;
      background: transparent;
      color: var(--text-muted);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
      flex-shrink: 0;
      margin-left: 12px;
      margin-top: -2px;
    }

    .dialog-close:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .dialog-close:active {
      transform: scale(0.92);
    }

    /* Content */
    .dialog-content {
      flex: 1;
      overflow-y: auto;
      padding: 16px 24px;
    }

    /* Section Labels */
    .section-label {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      color: var(--text-muted);
      margin-bottom: 12px;
    }

    /* Content Divider */
    .content-divider {
      height: 1px;
      background: var(--border-color);
      margin: 20px 0;
    }

    /* Hospital List */
    .hospital-list,
    .framework-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    /* Hospital Items */
    .hospital-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 14px 16px;
      background: transparent;
      border: 1px solid var(--border-color);
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.2s ease;
      text-align: left;
      width: 100%;
      position: relative;
      font-family: inherit;
    }

    .hospital-item:hover:not(.disabled) {
      background: var(--hover-bg);
      border-color: var(--primary-color);
      box-shadow: 0 2px 12px color-mix(in srgb, var(--color-primary) 15%, transparent);
    }

    .hospital-item.selected {
      background: var(--primary-bg-light);
      border-color: var(--primary-color);
      box-shadow: 0 0 0 1px var(--primary-color), 0 4px 16px color-mix(in srgb, var(--color-primary) 20%, transparent);
    }

    .hospital-item.disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    /* Framework Items */
    .framework-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      background: transparent;
      border: 1px solid var(--border-color);
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.2s ease;
      text-align: left;
      width: 100%;
      font-family: inherit;
    }

    .framework-item:hover {
      background: var(--hover-bg);
      border-color: var(--primary-color);
      box-shadow: 0 2px 12px color-mix(in srgb, var(--color-primary) 15%, transparent);
    }

    .framework-item.selected {
      background: var(--primary-bg-light);
      border-color: var(--primary-color);
      box-shadow: 0 0 0 1px var(--primary-color), 0 4px 16px color-mix(in srgb, var(--color-primary) 20%, transparent);
    }

    /* Radio buttons (shared) */
    .item-radio {
      flex-shrink: 0;
    }

    .radio-outer {
      width: 20px;
      height: 20px;
      border-radius: 50%;
      border: 2px solid var(--border-color);
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }

    .hospital-item.selected .radio-outer,
    .framework-item.selected .radio-outer {
      border-color: var(--primary-color);
    }

    .radio-inner {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: var(--primary-color);
      animation: radioScale 0.2s ease;
    }

    @keyframes radioScale {
      from { transform: scale(0); }
      to { transform: scale(1); }
    }

    /* Hospital icon */
    .hospital-item-icon {
      width: 36px;
      height: 36px;
      border-radius: 8px;
      background: var(--primary-bg-light);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      color: var(--primary-color);
    }

    /* Content (shared) */
    .item-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
    }

    .item-name {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .item-sub {
      font-size: 12px;
      font-weight: 500;
      color: var(--text-secondary);
    }

    .item-check {
      flex-shrink: 0;
      color: var(--primary-color);
    }

    .item-badge {
      font-size: 10px;
      font-weight: 600;
      color: var(--text-muted);
      background: var(--hover-bg);
      padding: 2px 8px;
      border-radius: 4px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    /* Dialog actions */
    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      padding: 16px 24px;
      border-top: 1px solid var(--border-color);
    }

    .btn-cancel {
      padding: 10px 20px;
      background: transparent;
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-secondary);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      font-family: inherit;
    }

    .btn-cancel:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .btn-confirm {
      padding: 10px 24px;
      background: var(--primary-color);
      border: none;
      border-radius: 8px;
      color: white;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      font-family: inherit;
    }

    .btn-confirm:hover:not(:disabled) {
      filter: brightness(1.1);
      box-shadow: 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
    }

    .btn-confirm:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .empty-frameworks {
      padding: 16px;
      text-align: center;
      color: var(--text-muted);
      font-size: 13px;
      border: 1px dashed var(--border-color);
      border-radius: 12px;
    }

    /* Responsive */
    @media (max-width: 640px) {
      .hospital-selector-dialog {
        min-width: unset;
        width: calc(100vw - 48px);
      }
    }
  `],
})
export class HospitalSelectorDialogComponent {
  readonly selectedHospitalId = signal<string | null>(null);
  readonly selectedJourneyId = signal<string | null>(null);

  /** Frameworks available for the currently selected hospital */
  readonly availableFrameworks = computed<FrameworkSelectorItem[]>(() => {
    const hospitalId = this.selectedHospitalId();
    if (!hospitalId) return [];
    return this.data.hospitalJourneys[hospitalId] ?? [];
  });

  constructor(
    public readonly dialogRef: MatDialogRef<HospitalSelectorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public readonly data: HospitalSelectorDialogData,
  ) {
    this.selectedHospitalId.set(data.selectedHospitalId);
    this.selectedJourneyId.set(data.selectedJourneyId);
  }

  selectHospital(hospital: HospitalSelectorItem): void {
    if (!hospital.isOnboarded) return;
    this.selectedHospitalId.set(hospital.id);

    // If current framework selection is not in the new hospital's journeys, clear it
    const journeys = this.data.hospitalJourneys[hospital.id] ?? [];
    const currentJourneyId = this.selectedJourneyId();
    if (currentJourneyId && !journeys.some((j) => j.id === currentJourneyId)) {
      this.selectedJourneyId.set(null);
    }
  }

  selectFramework(framework: FrameworkSelectorItem): void {
    this.selectedJourneyId.set(framework.id);
  }

  onConfirm(): void {
    const hospitalId = this.selectedHospitalId();
    const journeyId = this.selectedJourneyId();
    if (!hospitalId || !journeyId) return;
    if (hospitalId === this.data.selectedHospitalId && journeyId === this.data.selectedJourneyId) return;

    const result: HospitalFrameworkSelection = { hospitalId, journeyId };
    this.dialogRef.close(result);
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  onClose(): void {
    this.dialogRef.close(null);
  }
}
