import { Injectable } from '@angular/core';
import { normalizeGuid } from '../../shared/utils/guid.utils';
import { TokenService } from './token.service';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private readonly EXPIRATION_KEY = 'token_expiration';
  private readonly ORGANIZATION_ID_KEY = 'organization_id';
  private readonly HOSPITAL_ID_KEY = 'hospital_id';
  private readonly USER_ID_KEY = 'user_id';
  private readonly SESSION_ID_KEY = 'session_id';
  private readonly DEVICE_ID_KEY = 'device_id';

  constructor(private tokenService: TokenService) {}

  saveTokens(
    accessToken: string,
    refreshToken: string,
    expirationTime?: number,
    organizationId?: string | null,
    hospitalId?: string | null,
    userId?: string,
    sessionId?: string,
  ): void {
    this.tokenService.setAccessToken(accessToken);
    this.tokenService.setRefreshToken(refreshToken);

    if (expirationTime) {
      localStorage.setItem(this.EXPIRATION_KEY, expirationTime.toString());
    }

    const decoded = this.tokenService.decodeJwt(accessToken);
    const resolvedOrganizationId =
      normalizeGuid(organizationId) ?? normalizeGuid(decoded?.organization_id);
    const resolvedHospitalId = normalizeGuid(hospitalId) ?? normalizeGuid(decoded?.hospital_id);

    this.setStoredGuid(this.ORGANIZATION_ID_KEY, resolvedOrganizationId);
    this.setStoredGuid(this.HOSPITAL_ID_KEY, resolvedHospitalId);

    const resolvedUserId = normalizeGuid(userId) ?? normalizeGuid(decoded?.sub);
    if (resolvedUserId) {
      localStorage.setItem(this.USER_ID_KEY, resolvedUserId);
    }

    if (sessionId) {
      localStorage.setItem(this.SESSION_ID_KEY, sessionId);
    }
  }

  getSessionId(): string | null {
    return localStorage.getItem(this.SESSION_ID_KEY);
  }

  getOrCreateDeviceId(): string {
    let deviceId = localStorage.getItem(this.DEVICE_ID_KEY);
    if (!deviceId) {
      deviceId = crypto.randomUUID();
      localStorage.setItem(this.DEVICE_ID_KEY, deviceId);
    }
    return deviceId;
  }

  clearSession(): void {
    this.tokenService.clearTokens();
    localStorage.removeItem(this.EXPIRATION_KEY);
    localStorage.removeItem(this.ORGANIZATION_ID_KEY);
    localStorage.removeItem(this.HOSPITAL_ID_KEY);
    localStorage.removeItem(this.USER_ID_KEY);
    localStorage.removeItem(this.SESSION_ID_KEY);
  }

  getOrganizationId(): string | null {
    return this.getStoredGuid(this.ORGANIZATION_ID_KEY, 'organization_id');
  }

  getHospitalId(): string | null {
    return this.getStoredGuid(this.HOSPITAL_ID_KEY, 'hospital_id');
  }

  getUserId(): string | null {
    return (
      normalizeGuid(localStorage.getItem(this.USER_ID_KEY)) ??
      normalizeGuid(this.tokenService.decodeJwt(this.tokenService.getAccessToken() ?? '')?.sub)
    );
  }

  getExpiration(): number | null {
    const expiration = localStorage.getItem(this.EXPIRATION_KEY);
    return expiration ? parseInt(expiration, 10) : null;
  }

  isSessionValid(): boolean {
    const accessToken = this.tokenService.getAccessToken();
    if (!accessToken) {
      return false;
    }
    return !this.tokenService.isTokenExpired(accessToken);
  }

  private getStoredGuid(storageKey: string, claimName: string): string | null {
    const stored = normalizeGuid(localStorage.getItem(storageKey));
    if (stored) {
      return stored;
    }

    const token = this.tokenService.getAccessToken();
    if (!token) {
      return null;
    }

    const fromToken = normalizeGuid(this.tokenService.decodeJwt(token)?.[claimName]);
    if (fromToken) {
      localStorage.setItem(storageKey, fromToken);
    } else {
      localStorage.removeItem(storageKey);
    }

    return fromToken;
  }

  private setStoredGuid(storageKey: string, value: string | null): void {
    if (value) {
      localStorage.setItem(storageKey, value);
      return;
    }

    localStorage.removeItem(storageKey);
  }
}
