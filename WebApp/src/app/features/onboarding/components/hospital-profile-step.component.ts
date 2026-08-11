import { HttpErrorResponse } from '@angular/common/http';
import { Component, Input, Output, EventEmitter, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HospitalProfileData } from '../models/onboarding-state.model';
import { OnboardingDataService } from '../services/onboarding-data.service';
import { GeoSelectorService } from '../../../shared/services/geo-selector.service';
import { GEO_CONFIG } from '../../../core/config/geo.config';
import { GeoCountryOption, GeoStateOption } from '../../../shared/models/geo.models';
import { CommonLookupService } from '../../../shared/services/common-lookup.service';
import { DepartmentLookup } from '../../../shared/models/lookup.models';
import { OrganizationHospital, OrganizationService } from '../../../core/services/organization.service';

@Component({
  selector: 'app-hospital-profile-step',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="profile-container">
      <div class="profile-header">
        <h2>Hospital Profile</h2>
        <p>Tell us about your hospital to personalize your compliance setup</p>
      </div>

      <div class="profile-form">
        <div class="form-section">
          <div class="form-group">
            <label for="hospitalId">Hospital Name <span class="required">*</span></label>
            <select
              id="hospitalId"
              [(ngModel)]="profileData.hospitalId"
              class="form-control"
              [disabled]="hospitalsLoading || hospitals.length === 0">
              <option value="">
                {{
                  hospitalsLoading
                    ? 'Loading hospitals...'
                    : hospitals.length === 0
                      ? 'No hospitals available'
                      : 'Select Hospital'
                }}
              </option>
              <option *ngFor="let hospital of hospitals" [value]="hospital.id">
                {{ hospital.name }}
              </option>
            </select>
            <div *ngIf="hospitalsError" class="field-error">{{ hospitalsError }}</div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="hospitalType">Hospital Type <span class="required">*</span></label>
              <select
                id="hospitalType"
                [(ngModel)]="profileData.hospitalType"
                class="form-control">
                <option value="">Select type</option>
                <option *ngFor="let type of hospitalTypes" [value]="type">{{ type }}</option>
              </select>
            </div>

            <div class="form-group">
              <label for="bedCapacity">Bed Capacity <span class="required">*</span></label>
              <select
                id="bedCapacity"
                [(ngModel)]="profileData.bedCapacity"
                class="form-control">
                <option value="">Select capacity</option>
                <option *ngFor="let capacity of bedCapacities" [value]="capacity">{{ capacity }}</option>
              </select>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="country">Country <span class="required">*</span></label>
              <select
                id="country"
                [(ngModel)]="profileData.countryCode"
                (ngModelChange)="onCountryChanged($event)"
                class="form-control"
                [disabled]="!allowCountrySelection">
                <option value="" disabled>Select country</option>
                <option *ngFor="let c of countries" [value]="c.code">{{ c.name }}</option>
              </select>
            </div>

            <div class="form-group">
              <label for="state">State/Province <span class="required">*</span></label>
              <select
                id="state"
                [(ngModel)]="profileData.stateCode"
                class="form-control">
                <option value="" disabled>Select state</option>
                <option *ngFor="let s of states" [value]="s.code">{{ s.name }}</option>
              </select>
            </div>
          </div>
        </div>

        <div class="form-section">
          <h3>Departments</h3>
          <p class="section-description">Select all departments in your hospital</p>

          <div *ngIf="departmentsLoading" class="loading-state">
            Loading departments…
          </div>

          <div *ngIf="departmentsError" class="error-state">
            <div class="error-message">{{ departmentsError }}</div>
            <button type="button" class="btn-secondary" (click)="retryLoadDepartments()">Retry</button>
          </div>

          <div *ngIf="!departmentsLoading && !departmentsError && departments.length === 0" class="empty-state">
            No departments available.
          </div>

          <div *ngIf="missingSelectedDepartmentIds.length > 0" class="warning-state">
            <div class="warning-message">
              Some previously selected departments are no longer available ({{ missingSelectedDepartmentIds.length }}).
            </div>
            <button type="button" class="btn-secondary" (click)="clearUnavailableDepartments()">
              Clear unavailable selections
            </button>
          </div>

          <div *ngIf="!departmentsLoading && !departmentsError && departments.length > 0" class="multi-select-grid">
            <label
              *ngFor="let dept of departments"
              class="checkbox-card"
              [class.selected]="isDepartmentSelected(dept.id)">
              <input
                type="checkbox"
                [checked]="isDepartmentSelected(dept.id)"
                (change)="toggleDepartment(dept.id)">
              <span>{{ dept.name }}</span>
            </label>
          </div>
        </div>

      </div>

      <div class="profile-actions">
        <button class="btn-secondary" (click)="onBack.emit()">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M10 12L6 8L10 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Back
        </button>
        <button
          class="btn-primary"
          [disabled]="!isValid()"
          (click)="handleNext()">
          Continue
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M6 12L10 8L6 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .profile-container {
      padding: 32px;
      max-width: 800px;
      margin: 0 auto;
    }

    .profile-header {
      margin-bottom: 32px;
    }

    .profile-header h2 {
      margin: 0 0 8px 0;
      font-size: var(--font-size-section-title);
      font-weight: 700;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .profile-header h2 {
      color: var(--color-text-primary, #f9fafb);
    }

    .profile-header p {
      margin: 0;
      font-size: 14px;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .profile-header p {
      color: var(--color-text-secondary, #9ca3af);
    }

    .profile-form {
      display: flex;
      flex-direction: column;
      gap: 32px;
    }

    .form-section {
      background: var(--card-bg, #ffffff);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 12px;
      padding: 24px;
      box-shadow: var(--shadow-sm, 0 1px 2px rgba(0, 0, 0, 0.05));
    }

    [data-theme="dark"] .form-section {
      background: var(--card-bg, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.06));
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
    }

    .form-section h3 {
      margin: 0 0 8px 0;
      font-size: 16px;
      font-weight: 600;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .form-section h3 {
      color: var(--color-text-primary, #f9fafb);
    }

    .section-description {
      margin: 0 0 20px 0;
      font-size: 13px;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .section-description {
      color: var(--color-text-secondary, #9ca3af);
    }

    .form-group {
      margin-bottom: 20px;
    }

    .form-group:last-child {
      margin-bottom: 0;
    }

    .field-error {
      margin-top: 8px;
      font-size: 12px;
      color: #b91c1c;
      font-weight: 500;
    }

    [data-theme="dark"] .field-error {
      color: #fca5a5;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
    }

    label {
      display: block;
      margin-bottom: 8px;
      font-size: 14px;
      font-weight: 500;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] label {
      color: var(--color-text-primary, #f9fafb);
    }

    .required {
      color: #ef4444;
    }

    .form-control {
      width: 100%;
      padding: 11px 14px;
      border: 1.5px solid var(--color-border, #d1d5db);
      border-radius: 8px;
      font-size: 14px;
      background: var(--card-bg, #ffffff);
      color: var(--text-primary, #111827);
      transition: all 0.2s ease;
      font-family: inherit;
    }

    [data-theme="dark"] .form-control {
      background: var(--bg-tertiary, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.1));
      color: var(--text-primary, #f1f5f9);
    }

    .form-control:hover {
      border-color: var(--primary-color, var(--color-primary));
    }

    .form-control:focus {
      outline: none;
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
    }

    [data-theme="dark"] .form-control:focus {
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent));
    }

    .form-control::placeholder {
      color: var(--text-muted, #9ca3af);
    }

    [data-theme="dark"] .form-control::placeholder {
      color: var(--text-muted, #64748b);
    }

    .multi-select-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 12px;
    }

    .loading-state,
    .empty-state {
      padding: 12px 0;
      color: var(--color-text-secondary, #6b7280);
      font-size: 13px;
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
      margin-bottom: 12px;
    }

    .error-message {
      color: #b91c1c;
      font-size: 13px;
      font-weight: 500;
    }

    [data-theme="dark"] .error-message {
      color: #fca5a5;
    }

    .warning-state {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 10px 14px;
      border: 1px solid rgba(245, 158, 11, 0.35);
      background: rgba(245, 158, 11, 0.06);
      border-radius: 10px;
      color: #92400e;
      font-size: 12px;
      margin-bottom: 12px;
    }

    [data-theme="dark"] .warning-state {
      color: #fcd34d;
    }

    .warning-message {
      font-weight: 500;
    }

    .checkbox-card {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 14px 16px;
      background: var(--card-bg, #ffffff);
      border: 2px solid var(--border-color, #e5e7eb);
      border-radius: 10px;
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary, #111827);
      position: relative;
      overflow: hidden;
    }

    [data-theme="dark"] .checkbox-card {
      background: var(--bg-tertiary, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.08));
      color: var(--text-primary, #f1f5f9);
    }

    .checkbox-card::before {
      content: '';
      position: absolute;
      inset: 0;
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 5%, transparent));
      opacity: 0;
      transition: opacity 0.25s ease;
    }

    .checkbox-card:hover {
      border-color: var(--primary-color, var(--color-primary));
      transform: translateY(-1px);
      box-shadow: var(--shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.1));
    }

    .checkbox-card:hover::before {
      opacity: 1;
    }

    [data-theme="dark"] .checkbox-card:hover {
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
    }

    .checkbox-card.selected {
      border-color: var(--primary-color, var(--color-primary));
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 8%, transparent));
      color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 8%, transparent));
    }

    [data-theme="dark"] .checkbox-card.selected {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 12%, transparent));
      border-color: var(--primary-color, var(--color-primary));
      color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
    }

    .checkbox-card input[type="checkbox"] {
      width: 18px;
      height: 18px;
      cursor: pointer;
      accent-color: var(--primary-color, var(--color-primary));
      flex-shrink: 0;
      position: relative;
      z-index: 1;
    }

    .checkbox-card span {
      position: relative;
      z-index: 1;
    }

    .profile-actions {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      margin-top: 32px;
      padding-top: 24px;
      border-top: 1px solid var(--color-border, #e5e7eb);
    }

    [data-theme="dark"] .profile-actions {
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
      .profile-container {
        padding: 24px 16px;
      }

      .form-row {
        grid-template-columns: 1fr;
      }

      .multi-select-grid {
        grid-template-columns: 1fr;
      }

      .profile-actions {
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
export class HospitalProfileStepComponent implements OnInit {
  private dataService = inject(OnboardingDataService);
  private geoService = inject(GeoSelectorService);
  private geoConfig = inject(GEO_CONFIG);
  private commonLookupService = inject(CommonLookupService);
  private organizationService = inject(OrganizationService);
  private cdr = inject(ChangeDetectorRef);

  @Input() profileData: HospitalProfileData = {
    hospitalId: '',
    hospitalType: '',
    bedCapacity: '',
    countryCode: '',
    stateCode: '',
    departmentIds: []
  };
  @Output() onNext = new EventEmitter<HospitalProfileData>();
  @Output() onBack = new EventEmitter<void>();

  hospitalTypes: string[] = [];
  bedCapacities: string[] = [];
  departments: DepartmentLookup[] = [];
  hospitals: OrganizationHospital[] = [];

  countries: GeoCountryOption[] = [];
  states: GeoStateOption[] = [];
  allowCountrySelection = false;

  hospitalsLoading = false;
  hospitalsError: string | null = null;
  departmentsLoading = false;
  departmentsError: string | null = null;
  missingSelectedDepartmentIds: string[] = [];

  ngOnInit(): void {
    this.hospitalTypes = this.dataService.getHospitalTypes();
    this.bedCapacities = this.dataService.getBedCapacityOptions();
    this.loadHospitals();
    this.loadDepartments();

    this.allowCountrySelection = !!this.geoConfig.allowCountrySelection;
    this.countries = this.geoService.getActiveCountries();

    if (!this.profileData.countryCode) {
      this.profileData.countryCode = this.geoConfig.defaultCountryCode || 'IN';
    }

    this.states = this.geoService.getStates(this.profileData.countryCode);
    if (this.profileData.stateCode && !this.states.some((s) => s.code === this.profileData.stateCode)) {
      this.profileData.stateCode = '';
    }
  }

  /** Global auth interceptor redirects on 401; avoid misleading inline errors for auth failures. */
  private isAuthHttpError(err: unknown): boolean {
    return err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403);
  }

  private loadHospitals(): void {
    this.hospitalsLoading = true;
    this.hospitalsError = null;

    this.organizationService.loadOrganizationDetails().subscribe({
      next: (organization) => {
        this.hospitals = organization?.hospitals ?? [];

        if (
          this.profileData.hospitalId &&
          !this.hospitals.some((hospital) => hospital.id === this.profileData.hospitalId)
        ) {
          this.profileData.hospitalId = '';
        }

        this.hospitalsLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.hospitalsLoading = false;
        if (!this.isAuthHttpError(err)) {
          this.hospitalsError = 'Failed to load hospitals';
        }
        this.cdr.detectChanges();
      },
    });
  }

  private loadDepartments(forceReload = false): void {
    this.departmentsLoading = true;
    this.departmentsError = null;
    this.missingSelectedDepartmentIds = [];
    console.log('[HospitalProfileStep] loadDepartments called. forceReload=', forceReload);

    this.commonLookupService.loadDepartments(forceReload).subscribe({
      next: (depts) => {
        console.log('[HospitalProfileStep] Departments received in component:', depts);
        this.departments = depts ?? [];
        console.log('[HospitalProfileStep] Departments bound to template count:', this.departments.length);
        const existingIds = new Set(this.departments.map((d) => d.id));
        this.missingSelectedDepartmentIds = (this.profileData.departmentIds || []).filter((id) => !existingIds.has(id));
        this.departmentsLoading = false;
        console.log('[HospitalProfileStep] Loading complete. missingSelectedDepartmentIds=', this.missingSelectedDepartmentIds);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.departmentsLoading = false;
        if (!this.isAuthHttpError(err)) {
          this.departmentsError = 'Failed to load departments (timeout or network error). Please retry.';
        }
        console.error('[HospitalProfileStep] Departments subscription error:', err);
        this.cdr.detectChanges();
      },
    });
  }

  retryLoadDepartments(): void {
    this.loadDepartments(true);
  }

  clearUnavailableDepartments(): void {
    if (this.missingSelectedDepartmentIds.length === 0) return;
    const missing = new Set(this.missingSelectedDepartmentIds);
    this.profileData.departmentIds = (this.profileData.departmentIds || []).filter((id) => !missing.has(id));
    this.missingSelectedDepartmentIds = [];
  }

  onCountryChanged(countryCode: string): void {
    this.profileData.countryCode = countryCode;
    this.states = this.geoService.getStates(countryCode);
    this.profileData.stateCode = '';
  }

  isDepartmentSelected(deptId: string): boolean {
    return this.profileData.departmentIds.includes(deptId);
  }

  toggleDepartment(deptId: string): void {
    const index = this.profileData.departmentIds.indexOf(deptId);
    if (index > -1) {
      this.profileData.departmentIds.splice(index, 1);
    } else {
      this.profileData.departmentIds.push(deptId);
    }
  }

  isValid(): boolean {
    return !!(
      this.profileData.hospitalId &&
      this.profileData.hospitalType &&
      this.profileData.bedCapacity &&
      this.profileData.countryCode &&
      this.profileData.stateCode &&
      !this.departmentsLoading &&
      !this.departmentsError &&
      this.missingSelectedDepartmentIds.length === 0 &&
      this.profileData.departmentIds.length > 0
    );
  }

  handleNext(): void {
    if (this.isValid()) {
      this.onNext.emit(this.profileData);
    }
  }
}
