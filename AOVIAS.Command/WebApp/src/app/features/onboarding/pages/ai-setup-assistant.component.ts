import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { OnboardingStepperComponent } from '../components/onboarding-stepper.component';
import { AiAssistantPanelComponent } from '../components/ai-assistant-panel.component';
import { WelcomeStepComponent } from '../components/welcome-step.component';
import { HospitalProfileStepComponent } from '../components/hospital-profile-step.component';
import { FrameworkSelectorStepComponent } from '../components/framework-selector-step.component';
import { AssessmentStepComponent } from '../components/assessment-step.component';
import { TeamInvitationStepComponent } from '../components/team-invitation-step.component';
import { CompletionStepComponent } from '../components/completion-step.component';
import { ThemeToggleComponent } from '../../../shared/ui/theme-toggle/theme-toggle.component';
import { HospitalProfileData, AssessmentAnswer, TeamMember } from '../models/onboarding-state.model';
import { CompletionStepSummary } from '../models/completion-step-summary.model';
import { OnboardingStore } from '../state/onboarding.store';
import { AiOnboardingWizardStore } from '../state/ai-onboarding-wizard.store';
import { AiOnboardingValidationService } from '../services/ai-onboarding-validation.service';
import { OnboardingService } from '../services/onboarding.service';
import { OnboardingJourneyService } from '../services/onboarding-journey.service';
import { OnboardingTelemetryService } from '../services/onboarding-telemetry.service';
import { resolveRoleGuidance } from '../services/onboarding-role-guidance';
import { OnboardingJourneyDto } from '../models/onboarding-journey.model';
import { TrialStatusPanelComponent } from '../components/trial-status-panel.component';
import { CommonLookupService } from '../../../shared/services/common-lookup.service';
import { DepartmentLookup } from '../../../shared/models/lookup.models';
import { OrganizationService } from '../../../core/services/organization.service';
import { RoleRegistryService } from '../../../core/auth/roles/role-registry.service';
import { SessionService } from '../../../core/auth/session.service';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { buildOnboardSubmitRequest } from '../services/onboard-submit.mapper';
import { HeaderComponent } from '../../../core/navigation/header/app-header.component';

@Component({
  selector: 'app-ai-setup-assistant',
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    OnboardingStepperComponent,
    AiAssistantPanelComponent,
    WelcomeStepComponent,
    HospitalProfileStepComponent,
    FrameworkSelectorStepComponent,
    AssessmentStepComponent,
    TeamInvitationStepComponent,
    CompletionStepComponent,
    ThemeToggleComponent,
    TrialStatusPanelComponent,
  ],
  template: `
    <div class="onboarding-layout">
      <app-header [onboardingMode]="true"></app-header>
      <app-trial-status-panel [trial]="journey()?.trial ?? null"></app-trial-status-panel>
      <app-onboarding-stepper
        [currentStep]="currentStep()"
        [progressPercent]="journey()?.progressPercent ?? null"
        (stepSelect)="goToStep($event)">
      </app-onboarding-stepper>

      <div class="onboarding-content">
        <div class="ai-panel">
          <app-ai-assistant-panel [messageData]="getAIMessage()"></app-ai-assistant-panel>
        </div>

        <div class="interaction-panel">
          <div class="journey-error" *ngIf="journeyError()">
            Could not refresh onboarding status.
            <button type="button" (click)="reloadJourney()">Retry</button>
          </div>

          <app-welcome-step
            *ngIf="currentStep() === 1"
            [journey]="journey()"
            [subtitle]="roleGuidance().welcomeSubtitle"
            [supportEmail]="supportEmail"
            [docsUrl]="docsUrl"
            [skipMessage]="skipMessage()"
            (onStart)="handleWelcomeStart()"
            (onSkip)="handleSkip()">
          </app-welcome-step>

          <app-hospital-profile-step
            *ngIf="currentStep() === 2"
            [profileData]="wizard.state().hospitalProfile"
            (onNext)="handleProfileNext($event)"
            (onBack)="previousStep()">
          </app-hospital-profile-step>

          <app-framework-selector-step
            *ngIf="currentStep() === 3"
            [selectedFrameworks]="wizard.state().selectedFrameworks"
            (onNext)="handleFrameworksNext($event)"
            (onBack)="previousStep()">
          </app-framework-selector-step>

          <app-assessment-step
            *ngIf="currentStep() === 4"
            [answers]="wizard.state().assessmentAnswers"
            [frameworkCodes]="frameworkCodesForAssessment()"
            (questionsLoaded)="handleQuestionsLoaded($event)"
            (onNext)="handleAssessmentNext($event)"
            (onBack)="previousStep()">
          </app-assessment-step>

          <app-team-invitation-step
            *ngIf="currentStep() === 5"
            [teamMembers]="wizard.state().invitedUsers"
            [allowedDepartments]="allowedDepartments()"
            (onNext)="handleTeamNext($event)"
            (onBack)="previousStep()">
          </app-team-invitation-step>

          <app-completion-step
            *ngIf="currentStep() === 6"
            [summary]="getCompletionSummary()"
            [submitting]="submitting()"
            [submitError]="submitError()"
            [canSubmit]="submitReadiness().valid"
            [incompleteHint]="submitReadiness().hint"
            (onSubmit)="submitOnboard()">
          </app-completion-step>
        </div>
      </div>

      <app-theme-toggle></app-theme-toggle>
    </div>
  `,
  styles: [
    `
      .onboarding-layout {
        min-height: 100vh;
        background: var(--color-background, #ffffff);
        display: flex;
        flex-direction: column;
      }
      [data-theme='dark'] .onboarding-layout {
        background: var(--color-background, #0f172a);
      }
      .onboarding-content {
        flex: 1;
        display: grid;
        grid-template-columns: 35% 65%;
        /* header (64) + trial/stepper chrome (~160) */
        min-height: calc(100vh - 224px);
      }
      .ai-panel {
        position: sticky;
        top: var(--header-height, 64px);
        height: calc(100vh - 224px);
        overflow-y: auto;
      }
      .interaction-panel {
        background: var(--color-surface, #ffffff);
        overflow-y: auto;
      }
      .journey-error {
        margin: 12px 24px;
        padding: 10px 12px;
        border-radius: 8px;
        background: #fef2f2;
        color: #991b1b;
        font-size: 13px;
        display: flex;
        gap: 12px;
        align-items: center;
      }
      .journey-error button {
        border: 1px solid #991b1b;
        background: transparent;
        color: #991b1b;
        border-radius: 6px;
        padding: 4px 10px;
        cursor: pointer;
      }
      [data-theme='dark'] .interaction-panel {
        background: var(--color-surface, #1f2937);
      }
      @media (max-width: 1024px) {
        .onboarding-content {
          grid-template-columns: 40% 60%;
        }
      }
      @media (max-width: 768px) {
        .onboarding-content {
          grid-template-columns: 1fr;
        }
        .ai-panel {
          position: relative;
          height: auto;
          max-height: 300px;
        }
      }
    `,
  ],
})
export class AiSetupAssistantComponent implements OnInit {
  currentStep = signal(1);
  readonly wizard = inject(AiOnboardingWizardStore);
  private readonly legacyOnboardingStore = inject(OnboardingStore);
  private readonly router = inject(Router);
  private readonly validation = inject(AiOnboardingValidationService);
  private readonly onboardingService = inject(OnboardingService);
  private readonly journeyService = inject(OnboardingJourneyService);
  private readonly telemetry = inject(OnboardingTelemetryService);
  private readonly commonLookup = inject(CommonLookupService);
  private readonly organizationService = inject(OrganizationService);
  private readonly roleRegistry = inject(RoleRegistryService);
  private readonly sessionService = inject(SessionService);
  private readonly notification = inject(NotificationService);

  journey = signal<OnboardingJourneyDto | null>(null);
  journeyError = signal(false);
  skipMessage = signal<string | null>(null);
  roleGuidance = signal(resolveRoleGuidance('ORG_ADMIN'));
  readonly supportEmail = 'support@aovias.com';
  readonly docsUrl = '/docs';

  private departmentsCache = signal<DepartmentLookup[]>([]);
  assessmentQuestionCount = signal(0);

  frameworkCodesForAssessment = computed(() => this.wizard.state().selectedFrameworks ?? []);

  submitting = signal(false);
  submitError = signal<string | null>(null);

  /** Reused on retry after failure so duplicate submits map to one logical operation server-side. */
  private onboardIdempotencyKey: string | null = null;

  allowedDepartments = computed(() => {
    const ids = new Set(this.wizard.state().hospitalProfile.departmentIds ?? []);
    return this.departmentsCache()
      .filter((d) => ids.has(d.id))
      .map((d) => ({ id: d.id, name: d.name }));
  });

  submitReadiness = computed(() => {
    const state = this.wizard.state();
    const r = this.validation.canSubmitWizard(state, this.assessmentQuestionCount());
    return {
      valid: r.valid,
      hint: r.valid ? null : r.errors.join(' '),
    };
  });

  ngOnInit(): void {
    this.commonLookup.loadDepartments().subscribe({
      next: (rows) => this.departmentsCache.set(rows ?? []),
    });
    this.organizationService.loadOrganizationDetails().subscribe();
    const s = this.wizard.state();
    this.currentStep.set(s.currentStep);
    if (s.selectedFrameworks?.length) {
      this.legacyOnboardingStore.setFrameworkCodes(s.selectedFrameworks);
    }
    this.reloadJourney();
    this.telemetry.track('OnboardingOpened', 'Welcome');
  }

  reloadJourney(): void {
    this.journeyError.set(false);
    this.journeyService.getJourney(true).subscribe({
      next: (j) => {
        this.journey.set(j);
        this.roleGuidance.set(resolveRoleGuidance(j.role));
      },
      error: () => this.journeyError.set(true),
    });
  }

  getAIMessage() {
    const g = this.roleGuidance();
    const idx = this.currentStep() - 1;
    return {
      title: g.aiTitles[idx] || g.aiTitles[0],
      message: g.aiMessages[idx] || g.aiMessages[0],
      insights: idx === 0 ? [g.nextHint] : [],
    };
  }

  handleWelcomeStart(): void {
    this.telemetry.track('StageEntered', 'HospitalProfile');
    this.nextStep();
  }

  handleSkip(): void {
    this.telemetry.track('StepSkipped', 'Welcome', { reason: 'resume_later' });
    this.skipMessage.set('Your progress is saved. Continue setup anytime from this page — the app opens after onboarding completes.');
    this.notification.showSuccess('Progress saved. Resume setup when you are ready.');
  }

  goToStep(step: number): void {
    if (step >= 1 && step < this.currentStep()) {
      this.currentStep.set(step);
      this.wizard.setCurrentStep(step);
      this.telemetry.track('StageEntered', `Step${step}`);
    }
  }

  handleProfileNext(profileData: HospitalProfileData): void {
    const r = this.validation.validateHospitalProfile(profileData);
    if (!r.valid) {
      this.notification.showError(r.errors[0]);
      return;
    }
    this.wizard.setHospitalProfile(profileData);
    this.nextStep();
  }

  handleFrameworksNext(payload: { frameworkCodes: string[]; displayNames: string[] }): void {
    const r = this.validation.validateFrameworks(payload.frameworkCodes);
    if (!r.valid) {
      this.notification.showError(r.errors[0]);
      return;
    }
    this.wizard.setFrameworks(payload.frameworkCodes, payload.displayNames);
    this.nextStep();
  }

  handleQuestionsLoaded(count: number): void {
    this.assessmentQuestionCount.set(count);
  }

  handleAssessmentNext(answers: AssessmentAnswer[]): void {
    const r = this.validation.validateAssessment(answers, this.assessmentQuestionCount());
    if (!r.valid) {
      this.notification.showError(r.errors[0]);
      return;
    }
    this.wizard.setAssessmentAnswers(answers);
    this.telemetry.track('FirstAssessment', 'ComplianceAssessment');
    this.telemetry.track('StageCompleted', 'ComplianceAssessment');
    this.nextStep();
  }

  handleTeamNext(teamMembers: TeamMember[]): void {
    const allowed = new Set(this.wizard.state().hospitalProfile.departmentIds ?? []);
    const r = this.validation.validateTeamMembers(teamMembers, allowed);
    if (!r.valid) {
      this.notification.showError(r.errors[0]);
      return;
    }
    this.wizard.setInvitedUsers(teamMembers);
    if (teamMembers.length > 0) {
      this.telemetry.track('FirstUserInvited', 'TeamSetup');
    }
    this.nextStep();
  }

  submitOnboard(): void {
    const state = this.wizard.state();
    const ready = this.validation.canSubmitWizard(state, this.assessmentQuestionCount());
    if (!ready.valid) {
      this.notification.showError(ready.errors[0]);
      return;
    }
    const orgId = this.sessionService.getOrganizationId();
    if (!orgId) {
      this.notification.showError('Missing organization context. Please sign in again.');
      return;
    }
    const body = buildOnboardSubmitRequest(state, orgId);
    if (!body) {
      this.notification.showError('Missing hospital selection.');
      return;
    }
    this.submitting.set(true);
    this.submitError.set(null);
    if (!this.onboardIdempotencyKey) {
      this.onboardIdempotencyKey = globalThis.crypto.randomUUID();
    }
    this.onboardingService.submitOnboard(body, this.onboardIdempotencyKey).subscribe({
      next: () => {
        this.submitting.set(false);
        this.onboardIdempotencyKey = null;
        this.wizard.setCompleted(true);
        this.wizard.clearWizardSession();
        this.legacyOnboardingStore.setFrameworkCodes([]);
        this.telemetry.track('OnboardingCompleted', 'OperationalReadiness');
        // Refresh org-detail so guards see isOnboarded: true, then navigate
        this.organizationService.loadOrganizationDetails(true).subscribe({
          next: () => this.router.navigate(['/app/dashboard']),
          error: () => this.router.navigate(['/app/dashboard']),
        });
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const msg =
          (err.error && typeof err.error === 'object' && 'message' in err.error && (err.error as { message?: string }).message) ||
          err.message ||
          'Could not complete onboarding. Please try again.';
        this.submitError.set(typeof msg === 'string' ? msg : 'Could not complete onboarding.');
        this.notification.showError(typeof msg === 'string' ? msg : 'Could not complete onboarding.');
      },
    });
  }

  nextStep(): void {
    if (this.currentStep() < 6) {
      const n = this.currentStep() + 1;
      this.currentStep.set(n);
      this.wizard.setCurrentStep(n);
    }
  }

  previousStep(): void {
    if (this.currentStep() > 1) {
      const n = this.currentStep() - 1;
      this.currentStep.set(n);
      this.wizard.setCurrentStep(n);
    }
  }

  getCompletionSummary(): CompletionStepSummary {
    const state = this.wizard.state();
    const compliantCount = state.assessmentAnswers.filter((a) => a.answer === 'compliant').length;
    const totalQuestions = state.assessmentAnswers.length;
    const initialScore =
      totalQuestions > 0 ? Math.round((compliantCount / totalQuestions) * 100) : 0;

    const display =
      state.selectedFrameworkDisplayNames &&
      state.selectedFrameworkDisplayNames.length === state.selectedFrameworks.length
        ? state.selectedFrameworkDisplayNames
        : state.selectedFrameworks;

    const org = this.organizationService.getOrganization();
    const hospital = org?.hospitals?.find((h) => h.id === state.hospitalProfile.hospitalId);

    const departmentNames = (state.hospitalProfile.departmentIds ?? [])
      .map((id) => this.departmentsCache().find((d) => d.id === id)?.name)
      .filter((n): n is string => !!n);

    const teamRows = state.invitedUsers.map((m) => ({
      name: m.name,
      email: m.email,
      role: this.roleRegistry.labelOf(m.role),
      departmentLabel:
        this.departmentsCache().find((d) => d.id === m.departmentId)?.name ?? m.departmentId,
    }));

    return {
      frameworksCount: state.selectedFrameworks.length,
      departmentsCount: state.hospitalProfile.departmentIds.length,
      initialScore,
      frameworks: display,
      hospitalName: hospital?.name ?? '',
      hospitalType: state.hospitalProfile.hospitalType,
      bedCapacity: state.hospitalProfile.bedCapacity,
      geoLabel: [state.hospitalProfile.countryCode, state.hospitalProfile.stateCode].filter(Boolean).join(' · '),
      departmentNames,
      assessmentAnswers: state.assessmentAnswers,
      teamRows,
    };
  }
}
