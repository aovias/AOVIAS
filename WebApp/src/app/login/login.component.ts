import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, of, switchMap } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { UserContextService } from '../core/services/user-context.service';
import { ComplianceContextService } from '../core/services/compliance-context.service';
import { SessionService } from '../core/auth/session.service';
import { LoggerService } from '../core/services/logger.service';
import { NotificationService } from '../core/ui/services/notification.service';
import { OrganizationService } from '../core/services/organization.service';
import { RoleRegistryService } from '../core/auth/roles/role-registry.service';
import { OnboardingStateIsolationService } from '../core/services/onboarding-state-isolation.service';
import { PerformanceTimelineService } from '../core/performance/performance-timeline.service';
import { RoleExperienceService } from '../core/services/role-experience.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private userContext = inject(UserContextService);
  private complianceContext = inject(ComplianceContextService);
  private sessionService = inject(SessionService);
  private logger = inject(LoggerService);
  private notificationService = inject(NotificationService);
  private organizationService = inject(OrganizationService);
  private roleRegistry = inject(RoleRegistryService);
  private onboardingIsolation = inject(OnboardingStateIsolationService);
  private perf = inject(PerformanceTimelineService);
  private roleExperience = inject(RoleExperienceService);
  private fb = inject(FormBuilder);
  private titleService = inject(Title);

  ngOnInit(): void {
    this.titleService.setTitle(`Sign In | ${environment.appName}`);
  }

  protected showPassword = signal(false);
  protected isSubmitting = signal(false);
  protected emailTouched = signal(false);

  protected loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false],
  });

  get emailControl() {
    return this.loginForm.get('email')!;
  }

  get passwordControl() {
    return this.loginForm.get('password')!;
  }

  togglePasswordVisibility() {
    this.showPassword.set(!this.showPassword());
  }

  onEmailBlur() {
    this.emailTouched.set(true);
    if (this.emailControl.invalid && this.emailControl.touched) {
      if (this.emailControl.hasError('required')) {
        this.notificationService.showError('Email is required');
      } else if (this.emailControl.hasError('email')) {
        this.notificationService.showError('Please enter a valid email address');
      }
    }
  }

  onInputChange() {
    // Clear errors when user starts typing
    if (this.emailTouched()) {
      this.emailTouched.set(false);
    }
  }

  private handleError(error: HttpErrorResponse): void {
    const apiMessage = this.extractApiErrorMessage(error);
    let message: string;
    if (apiMessage) {
      message = apiMessage;
    } else if (error.status === 401) {
      message = 'Invalid email or password';
    } else if (error.status >= 500) {
      message = 'Unable to login. Please try again.';
    } else if (error.status === 0) {
      message = 'Network error. Please check your connection.';
    } else {
      message = 'Login failed. Please try again.';
    }

    this.notificationService.showError(message);
    this.logger.error('Login failed', error);
  }

  private extractApiErrorMessage(error: HttpErrorResponse): string | null {
    const body = error.error;
    if (!body || typeof body !== 'object') {
      return null;
    }

    if (typeof body.message === 'string' && body.message.trim().length > 0) {
      return body.message;
    }

    const firstError = Array.isArray(body.errors) ? body.errors[0] : null;
    if (firstError && typeof firstError.message === 'string' && firstError.message.trim().length > 0) {
      return firstError.message;
    }

    return null;
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      if (this.emailControl.invalid) {
        this.emailTouched.set(true);
        if (this.emailControl.hasError('required')) {
          this.notificationService.showError('Email is required');
        } else if (this.emailControl.hasError('email')) {
          this.notificationService.showError('Please enter a valid email address');
        }
      }
      if (this.passwordControl.invalid) {
        this.notificationService.showError('Password is required');
      }
      return;
    }

    this.isSubmitting.set(true);

    const { email, password } = this.loginForm.value;
    const priorUserId = this.sessionService.getUserId();
    const deviceId = this.sessionService.getOrCreateDeviceId();

    this.authService
      .login({ email, password, deviceId })
      .pipe(
        switchMap((response) => {
          if (response.requiresMfa && response.mfaChallengeToken) {
            this.isSubmitting.set(false);
            this.router.navigate(['/login/mfa'], {
              state: {
                mfaChallengeToken: response.mfaChallengeToken,
                email: response.email,
              },
            });
            return of(null);
          }
          this.onboardingIsolation.handleLoginIdentity(priorUserId, response.userId);

          this.sessionService.saveTokens(
            response.token,
            response.refreshToken,
            Date.now() + response.expiresIn * 1000,
            response.organizationId,
            response.hospitalId,
            response.userId,
            response.sessionId,
          );
          this.perf.mark('token_storage');

          // Update UserContextService — store canonical role id; legacy display strings mapped via registry
          const rawRole = response.role?.trim();
          const canonical = rawRole ? this.roleRegistry.normalize(rawRole) : null;
          this.userContext.setUser({
            id: response.userId,
            email: response.email,
            username: response.email,
            roles: rawRole ? [canonical ?? rawRole] : [],
          });

          // Use normalized tenant context from session (ignores empty GUID sentinels).
          this.complianceContext.setOrganizationId(this.sessionService.getOrganizationId());
          this.complianceContext.setHospitalId(this.sessionService.getHospitalId());

          this.logger.info('Login successful');
          this.perf.mark('auth_complete');

          return this.organizationService.loadOrganizationDetails().pipe(
            catchError((error) => {
              this.logger.warn('Organization initialization failed after login', error);
              return of(null);
            }),
          );
        }),
      )
      .subscribe({
        next: (organization) => {
          this.isSubmitting.set(false);

          if (!this.sessionService.getOrganizationId()) {
            this.notificationService.showWarning(
              'Your account is not linked to an organization yet. Complete onboarding to continue.',
            );
            this.router.navigate(['/onboarding']);
            return;
          }

          if (organization === null) {
            this.notificationService.showSuccess('Login successful');
            this.router.navigate(['/onboarding']);
            return;
          }

          const defaultHospital = this.organizationService.getDefaultHospital();
          if (defaultHospital) {
            this.complianceContext.setHospitalId(defaultHospital.id);
          }

          const hasOnboardedHospital = this.organizationService.hasAnyOnboardedHospital();
          this.logger.info('Post-login routing decision', {
            hasOnboardedHospital,
            hospitalsCount: organization?.hospitals?.length ?? 0,
            selectedHospitalId: defaultHospital?.id ?? null,
          });

          this.notificationService.showSuccess('Login successful');
          this.perf.mark('login_navigation_start');
          if (!hasOnboardedHospital) {
            void this.router.navigate(['/onboarding']);
            return;
          }
          this.roleExperience.resolveDefaultHomeRoute().subscribe((route) => {
            const [path, query] = route.split('?');
            const queryParams: Record<string, string> = {};
            if (query) {
              for (const part of query.split('&')) {
                const [k, v] = part.split('=');
                if (k) queryParams[decodeURIComponent(k)] = decodeURIComponent(v ?? '');
              }
            }
            void this.router.navigate([path || '/app/dashboard'], {
              queryParams: Object.keys(queryParams).length ? queryParams : undefined,
            });
          });
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmitting.set(false);
          this.handleError(error);
        },
      });
  }

  signInWithMicrosoft() {
    this.logger.debug('SSO: Microsoft');
  }

  signInWithGoogle() {
    this.logger.debug('SSO: Google');
  }

  biometricLogin() {
    this.logger.debug('Biometric authentication requested');
  }
}
