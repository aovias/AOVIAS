import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, catchError, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserContextService } from './user-context.service';

export interface TrialLicenseStatus {
  status: string;
  licenseType?: string;
  daysRemaining?: number | null;
  expiresAtUtc?: string | null;
  isOperational?: boolean;
  maxSeats?: number;
  usedSeats?: number;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class TrialExperienceService {
  private readonly http = inject(HttpClient);
  private readonly userContext = inject(UserContextService);
  readonly status = signal<TrialLicenseStatus | null>(null);
  readonly error = signal<string | null>(null);
  readonly loaded = signal(false);

  readonly isReadOnly = computed(() => {
    const s = this.status()?.status?.toLowerCase();
    return s === 'expired' || s === 'revoked' || s === 'suspended' || s === 'archived';
  });

  readonly isGrace = computed(() => this.status()?.status?.toLowerCase() === 'grace');

  readonly daysRemaining = computed(() => {
    const st = this.status();
    if (st?.daysRemaining != null) return st.daysRemaining;
    if (!st?.expiresAtUtc) return null;
    const ms = new Date(st.expiresAtUtc).getTime() - Date.now();
    return Math.max(0, Math.ceil(ms / (24 * 60 * 60 * 1000)));
  });

  refresh() {
    const orgId = this.userContext.getOrganization()?.id;
    if (!orgId) {
      this.loaded.set(true);
      return of(null);
    }
    return this.http
      .get<any>(`${environment.apiBaseUrl}/licenses/status`, {
        params: { organizationId: orgId },
      })
      .pipe(
        map((body) => (body?.data ?? body) as any),
        tap((data) => {
          const expires = data?.expiresAtUtc ?? data?.ExpiresAtUtc;
          let days: number | null = data?.daysRemaining ?? null;
          if (days == null && expires) {
            days = Math.max(0, Math.ceil((new Date(expires).getTime() - Date.now()) / 86400000));
          }
          this.status.set({
            status: data?.status ?? data?.Status ?? 'Unknown',
            licenseType: data?.licenseType ?? data?.LicenseType,
            daysRemaining: days,
            expiresAtUtc: expires,
            isOperational: data?.isOperational ?? data?.IsOperational,
            maxSeats: data?.maxSeats ?? data?.MaxSeats,
            usedSeats: data?.usedSeats ?? data?.UsedSeats,
            message: data?.message ?? data?.Message,
          });
          this.loaded.set(true);
          this.error.set(null);
        }),
        catchError((e) => {
          this.error.set(e?.message || 'License status unavailable');
          this.loaded.set(true);
          return of(null);
        }),
      );
  }
}
