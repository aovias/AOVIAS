import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface SecurityEventItem {
  id: string;
  eventType: string;
  severity: string;
  userId?: string;
  summary: string;
  occurredAt: string;
  payloadJson?: string;
}

export interface SecurityTimelineItem {
  id: string;
  source: string;
  eventType: string;
  summary: string;
  occurredAt: string;
}

export interface UserRiskScore {
  userId: string;
  score: number;
  level: string;
  factorsJson: string;
  calculatedAt: string;
}

export interface TrustedDeviceItem {
  id: string;
  deviceId: string;
  nickname?: string;
  platform?: string;
  browser?: string;
  riskLevel: string;
  verificationStatus: string;
  trustedUntil: string;
  lastSeenAt: string;
}

export interface SecurityDashboard {
  activeSessions: number;
  lockedAccounts: number;
  highRiskUsers: number;
  failedLogins24h: number;
  mfaEnabledUsers: number;
  totalUsers: number;
  trustedDevices: number;
  openIncidents: number;
}

@Injectable({ providedIn: 'root' })
export class SecurityApiService extends BaseApiService {
  getDashboard(): Observable<SecurityDashboard> {
    return this.get<SecurityDashboard>('/security/dashboard');
  }

  getEvents(page = 1, pageSize = 20, userId?: string): Observable<SecurityEventItem[]> {
    return this.get<SecurityEventItem[]>('/security/events', { page, pageSize, userId });
  }

  getRisk(userId?: string): Observable<UserRiskScore> {
    return this.get<UserRiskScore>('/security/risk', { userId });
  }

  getDevices(userId?: string): Observable<TrustedDeviceItem[]> {
    return this.get<TrustedDeviceItem[]>('/security/devices', { userId });
  }

  getTimeline(userId?: string, take = 100): Observable<SecurityTimelineItem[]> {
    return this.get<SecurityTimelineItem[]>('/security/timeline', { userId, take });
  }

  updatePolicy(body: Record<string, unknown>): Observable<unknown> {
    return this.post('/security/policies', body);
  }

  revokeDevice(deviceRecordId: string, userId?: string): Observable<unknown> {
    const qs = userId ? `?userId=${userId}` : '';
    return this.post(`/security/device/revoke${qs}`, { deviceRecordId });
  }

  revokeSession(sessionId: string, userId?: string): Observable<unknown> {
    const qs = userId ? `?userId=${userId}` : '';
    return this.post(`/security/session/revoke${qs}`, { sessionId });
  }
}
