import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { TokenService } from './token.service';
import { SessionService } from './session.service';
import { OrganizationService } from '../services/organization.service';
import { ComplianceContextService } from '../services/compliance-context.service';
import { AppContextService } from '../services/app-context.service';
import { LoginRequest, MfaLoginRequest } from './models/login-request.model';
import { LoginResponse, RefreshTokenResponse, UserSessionDto } from './models/login-response.model';
import { BaseApiService } from '../../shared/services/base-api.service';
import { CommonLookupService } from '../../shared/services/common-lookup.service';
import { OnboardingStateIsolationService } from '../services/onboarding-state-isolation.service';
import { UserContextService } from '../services/user-context.service';
import { PlatformConfigurationService } from '../configuration/platform-configuration.service';
import { normalizeGuid } from '../../shared/utils/guid.utils';

@Injectable({
  providedIn: 'root',
})
export class AuthService extends BaseApiService {
  private tokenService = inject(TokenService);
  private sessionService = inject(SessionService);
  private organizationService = inject(OrganizationService);
  private complianceContextService = inject(ComplianceContextService);
  private appContextService = inject(AppContextService);
  private commonLookupService = inject(CommonLookupService);
  private onboardingIsolation = inject(OnboardingStateIsolationService);
  private userContext = inject(UserContextService);
  private platformConfiguration = inject(PlatformConfigurationService);

  login(credentials: LoginRequest): Observable<LoginResponse> {
    const payload = {
      ...credentials,
      deviceId: credentials.deviceId ?? this.sessionService.getOrCreateDeviceId(),
    };
    return this.post<LoginResponse>('/identity/login', payload).pipe(
      tap((response) => this.persistLoginResponse(response)),
    );
  }

  completeMfaLogin(request: MfaLoginRequest): Observable<LoginResponse> {
    const payload = {
      ...request,
      deviceId: request.deviceId ?? this.sessionService.getOrCreateDeviceId(),
    };
    return this.post<LoginResponse>('/identity/login/mfa', payload).pipe(
      tap((response) => this.persistLoginResponse(response)),
    );
  }

  logout(): Observable<unknown> {
    const refreshToken = this.tokenService.getRefreshToken();
    return this.post('/identity/logout', { refreshToken }).pipe(
      tap(() => this.clearLocalSession()),
    );
  }

  logoutLocal(): void {
    this.clearLocalSession();
  }

  refreshToken(hospitalId?: string | null): Observable<RefreshTokenResponse> {
    const refreshToken = this.tokenService.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const preferredHospital =
      normalizeGuid(hospitalId) ??
      normalizeGuid(this.complianceContextService.getHospitalId()) ??
      normalizeGuid(this.appContextService.getPersistedHospitalId()) ??
      normalizeGuid(this.sessionService.getHospitalId());

    const payload: { refreshToken: string; hospitalId?: string } = { refreshToken };
    if (preferredHospital) {
      payload.hospitalId = preferredHospital;
    }

    return this.post<RefreshTokenResponse>('/identity/refresh', payload).pipe(
      tap((response) => {
        const expirationTime = Date.now() + response.expiresIn * 1000;
        this.sessionService.saveTokens(
          response.token,
          response.refreshToken ?? refreshToken,
          expirationTime,
          undefined,
          undefined,
          this.sessionService.getUserId() ?? undefined,
          this.sessionService.getSessionId() ?? undefined,
        );
        this.complianceContextService.setOrganizationId(this.sessionService.getOrganizationId());
        this.complianceContextService.setHospitalId(this.sessionService.getHospitalId());
        const jwtHospital = this.sessionService.getHospitalId();
        if (jwtHospital) {
          sessionStorage.setItem('selected_hospital_id', jwtHospital);
        }
      }),
    );
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.post('/identity/forgot-password', { email });
  }

  resetPassword(token: string, newPassword: string): Observable<{ message: string }> {
    return this.post('/identity/reset-password', { token, newPassword });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<{ message: string }> {
    return this.put('/identity/change-password', { currentPassword, newPassword });
  }

  verifyEmail(token: string): Observable<{ message: string }> {
    return this.post('/identity/verify-email', { token });
  }

  register(payload: { email: string; password: string; role?: string }): Observable<{
    userId: string;
    email: string;
    platformId: string;
    message: string;
    requiresEmailVerification: boolean;
    emailConfirmed?: boolean;
    emailVerificationMode?: string;
  }> {
    return this.post('/identity/self-register', payload);
  }

  resendVerification(email: string): Observable<{ message: string }> {
    return this.post('/identity/resend-verification', { email });
  }

  listSessions(): Observable<UserSessionDto[]> {
    return this.get<UserSessionDto[]>('/identity/sessions');
  }

  revokeSession(sessionId: string): Observable<{ message: string }> {
    return this.delete(`/identity/sessions/${sessionId}`);
  }

  logoutAll(): Observable<{ message: string }> {
    return this.post('/identity/logout-all', {});
  }

  isAuthenticated(): boolean {
    return this.sessionService.isSessionValid();
  }

  private persistLoginResponse(response: LoginResponse): void {
    if (response.requiresMfa) {
      return;
    }
    const expirationTime = Date.now() + response.expiresIn * 1000;
    this.sessionService.saveTokens(
      response.token,
      response.refreshToken,
      expirationTime,
      response.organizationId,
      response.hospitalId,
      response.userId,
      response.sessionId,
    );
  }

  private clearLocalSession(): void {
    this.onboardingIsolation.logoutCleanup();
    this.appContextService.clearPersistedSelection();
    this.sessionService.clearSession();
    // Must be cleared explicitly: the context is rehydrated from the token and short-circuits once
    // populated, so a stale identity would otherwise survive a logout / login-as-someone-else.
    this.userContext.clear();
    this.organizationService.clearOrganization();
    this.complianceContextService.clear();
    this.commonLookupService.clearCaches();
    // Tenant-scoped and loaded once per session (DEF-CFG-001). Without this the next organization
    // both sees the previous one's audit types, statuses, labels and workflow *and* never fetches
    // its own, because the load-once guard would still be set.
    this.platformConfiguration.reset();
  }
}
