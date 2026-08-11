import { Component, ChangeDetectionStrategy, signal, output, input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Hospital {
  id: string;
  name: string;
  organizationName: string;
  icon?: string;
}

@Component({
  selector: 'app-hospital-selector-modal',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isOpen()) {
      <div class="modal-overlay" (click)="handleOverlayClick($event)">
        <div class="modal-container" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title">Select Hospital</h2>
            <p class="modal-subtitle">Choose a hospital to continue</p>
          </div>

          <div class="modal-body">
            <div class="hospital-list">
              @for (hospital of hospitals(); track hospital.id) {
                <button
                  class="hospital-item"
                  [class.selected]="selectedHospitalId() === hospital.id"
                  (click)="selectHospital(hospital.id)"
                >
                  <div class="hospital-icon">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                      <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                      <path d="M9 22V12h6v10" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                  </div>
                  <div class="hospital-info">
                    <span class="hospital-name">{{ hospital.name }}</span>
                    <span class="hospital-org">{{ hospital.organizationName }}</span>
                  </div>
                  @if (selectedHospitalId() === hospital.id) {
                    <svg class="check-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                      <path d="M20 6L9 17l-5-5" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                    </svg>
                  }
                </button>
              }
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-secondary" (click)="handleCancel()">
              Cancel
            </button>
            <button
              class="btn btn-primary"
              (click)="handleConfirm()"
              [disabled]="!selectedHospitalId()"
            >
              Confirm Selection
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.6);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      animation: fadeIn 0.2s ease;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
      }
      to {
        opacity: 1;
      }
    }

    .modal-container {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 16px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      width: 90%;
      max-width: 560px;
      max-height: 80vh;
      display: flex;
      flex-direction: column;
      animation: scaleIn 0.3s ease;
    }

    @keyframes scaleIn {
      from {
        transform: scale(0.9);
        opacity: 0;
      }
      to {
        transform: scale(1);
        opacity: 1;
      }
    }

    .modal-header {
      padding: 24px 28px 20px;
      border-bottom: 1px solid var(--border-color);
    }

    .modal-title {
      font-size: var(--font-size-section-title);
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 8px 0;
      line-height: 1.2;
    }

    .modal-subtitle {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.4;
    }

    .modal-body {
      padding: 20px 28px;
      overflow-y: auto;
      flex: 1;
    }

    .modal-body::-webkit-scrollbar {
      width: 6px;
    }

    .modal-body::-webkit-scrollbar-track {
      background: transparent;
    }

    .modal-body::-webkit-scrollbar-thumb {
      background: var(--border-color);
      border-radius: 3px;
    }

    .hospital-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .hospital-item {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px;
      background: transparent;
      border: 2px solid var(--border-color);
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.2s ease;
      text-align: left;
      width: 100%;
    }

    .hospital-item:hover {
      background: var(--hover-bg);
      border-color: var(--primary-color);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px color-mix(in srgb, var(--color-primary) 15%, transparent);
    }

    .hospital-item.selected {
      background: var(--primary-bg-light);
      border-color: var(--primary-color);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary) 10%, transparent);
    }

    .hospital-icon {
      width: 44px;
      height: 44px;
      background: var(--primary-bg-light);
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--primary-color);
      flex-shrink: 0;
      transition: all 0.2s ease;
    }

    .hospital-item:hover .hospital-icon {
      background: var(--primary-color);
      color: white;
    }

    .hospital-item.selected .hospital-icon {
      background: var(--primary-color);
      color: white;
    }

    .hospital-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .hospital-name {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
      line-height: 1.3;
    }

    .hospital-org {
      font-size: 13px;
      color: var(--text-secondary);
      line-height: 1.3;
    }

    .check-icon {
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .modal-footer {
      padding: 20px 28px 24px;
      border-top: 1px solid var(--border-color);
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }

    .btn {
      padding: 11px 20px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      border: none;
      cursor: pointer;
      transition: all 0.2s ease;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 120px;
    }

    .btn-secondary {
      background: transparent;
      border: 1px solid var(--border-color);
      color: var(--text-primary);
    }

    .btn-secondary:hover {
      background: var(--hover-bg);
      border-color: var(--text-secondary);
    }

    .btn-primary {
      background: var(--primary-color);
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--primary-dark);
      box-shadow: 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    @media (max-width: 640px) {
      .modal-container {
        width: 95%;
        max-height: 90vh;
      }

      .modal-header {
        padding: 20px 20px 16px;
      }

      .modal-title {
        font-size: 20px;
      }

      .modal-body {
        padding: 16px 20px;
      }

      .modal-footer {
        padding: 16px 20px 20px;
      }

      .hospital-item {
        padding: 14px;
      }
    }
  `],
})
export class HospitalSelectorModalComponent implements OnInit {
  readonly isOpen = input.required<boolean>();
  readonly hospitals = input.required<Hospital[]>();
  readonly currentHospitalId = input.required<string>();

  readonly confirm = output<string>();
  readonly cancel = output<void>();

  readonly selectedHospitalId = signal<string | null>(null);

  ngOnInit(): void {
    this.selectedHospitalId.set(this.currentHospitalId());
  }

  selectHospital(hospitalId: string): void {
    this.selectedHospitalId.set(hospitalId);
  }

  handleConfirm(): void {
    const id = this.selectedHospitalId();
    if (id) {
      this.confirm.emit(id);
    }
  }

  handleCancel(): void {
    this.selectedHospitalId.set(this.currentHospitalId());
    this.cancel.emit();
  }

  handleOverlayClick(event: MouseEvent): void {
    this.handleCancel();
  }
}
