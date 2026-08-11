import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { OnboardingStore } from '../features/onboarding/state/onboarding.store';
import { OnboardingService } from '../features/onboarding/services/onboarding.service';
import { NotificationService } from '../core/ui/services/notification.service';
import { LoggerService } from '../core/services/logger.service';
import { Organization } from '../features/onboarding/models/organization.model';
import { Hospital } from '../features/onboarding/models/hospital.model';
import { AdminUser } from '../features/onboarding/models/admin-user.model';
import {
  TRIAL_PROVISIONING_STORAGE_KEY,
  TrialProvisioningSnapshot,
  getTrialProvisioningStorageKey,
} from '../features/onboarding/models/signup-response.model';
import { GeoSelectorComponent } from '../shared/ui/geo-selector/geo-selector.component';
import { GeoSelectionValue } from '../shared/models/geo.models';
import { DEFAULT_GEO_CONFIG } from '../core/config/geo.config';
import { SessionService } from '../core/auth/session.service';

@Component({
  selector: 'app-signup',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    GeoSelectorComponent,
  ],
  templateUrl: './signup.html',
  styleUrl: './signup.css',
})
export class SignupComponent implements OnInit {
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private onboardingStore = inject(OnboardingStore);
  private onboardingService = inject(OnboardingService);
  private notificationService = inject(NotificationService);
  private logger = inject(LoggerService);
  private sessionService = inject(SessionService);

  currentStep = signal(1);
  totalSteps = 3;

  showPassword = signal(false);
  showConfirmPassword = signal(false);
  isSubmitting = signal(false);

  organizationTypes = [
    'Private Hospital',
    'Government Hospital',
    'Trust-run Hospital',
    'Corporate Hospital Chain',
    'Medical College Hospital',
    'Specialty Clinic',
    'Multi-Specialty Hospital',
  ];

  nabhLevels = [
    'Entry Level',
    'Progressive Level',
    'Full Accreditation',
    'Not Accredited Yet',
  ];

  organizationForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    type: ['', Validators.required],
    address: ['', [Validators.required, Validators.minLength(3)]],
  });

  hospitalForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    code: ['', [Validators.required, Validators.maxLength(20), Validators.pattern(/^[A-Z0-9-]+$/)]],
    address: ['', [Validators.required, Validators.minLength(3)]],
    nabhLevel: ['', Validators.required],
    geo: [null as GeoSelectionValue | null, Validators.required],
  });

  adminUserForm: FormGroup = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2), this.noNumericOnlyValidator]],
    lastName: ['', [Validators.required, Validators.minLength(2), this.noNumericOnlyValidator]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
    agreeToTerms: [false, Validators.requiredTrue],
  }, { validators: this.passwordMatchValidator });

  ngOnInit(): void {
    // Load state from sessionStorage if available
    this.onboardingStore.loadFromSessionStorage();
    this.currentStep.set(this.onboardingStore.currentStep());

    // Restore form data from store
    const org = this.onboardingStore.organization();
    if (org) {
      this.organizationForm.patchValue(org);
    }

    const hosp = this.onboardingStore.hospital();
    if (hosp) {
      this.hospitalForm.patchValue({
        name: hosp.name,
        code: hosp.code,
        address: hosp.address,
        nabhLevel: hosp.nabhLevel,
        geo: {
          countryCode: hosp.countryCode || DEFAULT_GEO_CONFIG.defaultCountryCode,
          stateCode: hosp.stateCode || '',
          cityName: hosp.cityName || '',
        },
      });
    }

    const user = this.onboardingStore.adminUser();
    if (user) {
      this.adminUserForm.patchValue({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email,
        password: '',
        confirmPassword: '',
      });
    }
  }

  private noNumericOnlyValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value?.trim();
    if (!value) return null; // Let `required` handle empty
    return /^\d+$/.test(value) ? { numericOnly: true } : null;
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  togglePasswordVisibility() {
    this.showPassword.set(!this.showPassword());
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword.set(!this.showConfirmPassword());
  }

  getStepTitle(): string {
    const titles = ['Organization Details', 'Hospital Information', 'Administrator Account'];
    return titles[this.currentStep() - 1];
  }

  getStepDescription(): string {
    const descriptions = [
      'Information about the organization operating the hospital',
      'Details about the hospital facility',
      'Create the primary compliance administrator account',
    ];
    return descriptions[this.currentStep() - 1];
  }

  isStep1Valid(): boolean {
    return this.organizationForm.valid;
  }

  isStep2Valid(): boolean {
    return this.hospitalForm.valid;
  }

  isStep3Valid(): boolean {
    return this.adminUserForm.valid && !this.adminUserForm.hasError('passwordMismatch');
  }

  transformHospitalCode(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.toUpperCase().replace(/[^A-Z0-9-]/g, '');
    this.hospitalForm.patchValue({ code: value }, { emitEvent: false });
  }

  nextStep() {
    if (this.currentStep() === 1) {
      if (!this.isStep1Valid()) {
        this.organizationForm.markAllAsTouched();
        return;
      }
      const orgData: Organization = this.organizationForm.value;
      this.onboardingStore.setOrganization(orgData);
    } else if (this.currentStep() === 2) {
      if (!this.isStep2Valid()) {
        this.hospitalForm.markAllAsTouched();
        return;
      }
      const geo = this.hospitalForm.value.geo as GeoSelectionValue;
      const hospData: Hospital = {
        name: this.hospitalForm.value.name,
        code: this.hospitalForm.value.code,
        address: this.hospitalForm.value.address,
        nabhLevel: this.hospitalForm.value.nabhLevel,
        countryCode: geo.countryCode,
        stateCode: geo.stateCode,
        cityName: geo.cityName,
      };
      this.onboardingStore.setHospital(hospData);
    }

    if (this.currentStep() < this.totalSteps) {
      this.currentStep.set(this.currentStep() + 1);
      this.onboardingStore.setCurrentStep(this.currentStep());
    }
  }

  previousStep() {
    if (this.currentStep() > 1) {
      // Save current step data before going back
      if (this.currentStep() === 3) {
        const userData: AdminUser = {
          firstName: this.adminUserForm.value.firstName?.trim() || '',
          lastName: this.adminUserForm.value.lastName?.trim() || '',
          email: this.adminUserForm.value.email,
          password: this.adminUserForm.value.password,
        };
        this.onboardingStore.setAdminUser(userData);
      } else if (this.currentStep() === 2) {
        const geo = this.hospitalForm.value.geo as GeoSelectionValue;
        const hospData: Hospital = {
          name: this.hospitalForm.value.name,
          code: this.hospitalForm.value.code,
          address: this.hospitalForm.value.address,
          nabhLevel: this.hospitalForm.value.nabhLevel,
          countryCode: geo.countryCode,
          stateCode: geo.stateCode,
          cityName: geo.cityName,
        };
        this.onboardingStore.setHospital(hospData);
      }

      this.currentStep.set(this.currentStep() - 1);
      this.onboardingStore.setCurrentStep(this.currentStep());

      // Restore form data when going back
      if (this.currentStep() === 1) {
        const org = this.onboardingStore.organization();
        if (org) {
          this.organizationForm.patchValue(org);
        }
      } else if (this.currentStep() === 2) {
        const hosp = this.onboardingStore.hospital();
        if (hosp) {
          this.hospitalForm.patchValue({
            name: hosp.name,
            code: hosp.code,
            address: hosp.address,
            nabhLevel: hosp.nabhLevel,
            geo: {
              countryCode: hosp.countryCode || DEFAULT_GEO_CONFIG.defaultCountryCode,
              stateCode: hosp.stateCode || '',
              cityName: hosp.cityName || '',
            },
          });
        }
      }
    }
  }

  onSubmit(event?: Event) {
    if (event) {
      event.preventDefault();
    }

    this.logger.debug('onSubmit called', { step: this.currentStep(), isSubmitting: this.isSubmitting() });

    if (this.isSubmitting()) {
      this.logger.debug('Already submitting, returning');
      return;
    }

    // Only allow submission on step 3
    if (this.currentStep() !== this.totalSteps) {
      this.logger.warn('onSubmit called but not on final step', { currentStep: this.currentStep() });
      return;
    }

    if (!this.isStep3Valid()) {
      this.logger.warn('Step 3 form invalid', { formErrors: this.adminUserForm.errors });
      this.adminUserForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.logger.debug('Starting API call');

    // Save admin user data
    const userData: AdminUser = {
      firstName: this.adminUserForm.value.firstName?.trim() || '',
      lastName: this.adminUserForm.value.lastName?.trim() || '',
      email: this.adminUserForm.value.email,
      password: this.adminUserForm.value.password,
    };
    this.onboardingStore.setAdminUser(userData);

    // Build request payload
    const request = this.onboardingStore.buildRequestPayload();

    // Call API
    this.logger.debug('Calling onboardingService.createHospital', { request: { ...request, adminUser: { ...request.adminUser, password: '[REDACTED]' } } });
    this.onboardingService.createHospital(request).subscribe({
      next: (response) => {
        this.logger.debug('API call successful', {
          ...response,
          token: response?.token ? '[REDACTED]' : undefined,
          refreshToken: response?.refreshToken ? '[REDACTED]' : undefined,
        });
        this.isSubmitting.set(false);

        if (response?.token && response?.refreshToken) {
          const expirationTime = Date.now() + (response.expiresIn || 3600) * 1000;
          this.sessionService.saveTokens(
            response.token,
            response.refreshToken,
            expirationTime,
            response.organizationId,
            response.hospitalId,
            response.userId,
          );
        }

        const trial: TrialProvisioningSnapshot = {
          licenseKeyId: response?.licenseKeyId,
          licenseStatus: response?.licenseStatus,
          trialExpiresAtUtc: response?.trialExpiresAtUtc,
          maxSeats: response?.maxSeats,
          provisioningStatus: response?.provisioningStatus,
          organizationId: response?.organizationId,
        };
        try {
          const key = getTrialProvisioningStorageKey(response?.userId);
          sessionStorage.setItem(key, JSON.stringify(trial));
          sessionStorage.removeItem(TRIAL_PROVISIONING_STORAGE_KEY);
        } catch {
          /* ignore storage quota */
        }

        this.notificationService.showSuccess('Organization created — continue setup to finish onboarding');
        this.logger.info('Signup successful; redirecting to onboarding wizard');
        this.onboardingStore.reset();
        this.router.navigate(['/onboarding']);
      },
      error: (error: HttpErrorResponse) => {
        this.logger.error('API call failed', error);
        this.isSubmitting.set(false);
        this.handleError(error);
      },
    });
  }

  private handleError(error: HttpErrorResponse): void {
    let message: string;
    const errorMessage = error.error?.message || error.message || '';

    if ((error.status === 400 || error.status === 409) && errorMessage.toLowerCase().includes('email')) {
      message = 'Admin email already exists';
    } else if ((error.status === 400 || error.status === 409) && errorMessage.toLowerCase().includes('code')) {
      message = 'Hospital code already registered';
    } else if (error.status >= 500) {
      message = 'Unable to complete onboarding. Please try again.';
    } else if (error.status === 0) {
      message = 'Network error. Please check your connection.';
    } else {
      message = 'Unable to complete onboarding. Please try again.';
    }

    this.notificationService.showError(message);
    // Never log password - create a safe error object
    const safeError = { ...error };
    if (safeError.error) {
      safeError.error = { ...safeError.error };
      if (safeError.error.adminUser) {
        safeError.error.adminUser = { ...safeError.error.adminUser, password: '[REDACTED]' };
      }
    }
    this.logger.error('Onboarding failed', safeError);
  }
}
