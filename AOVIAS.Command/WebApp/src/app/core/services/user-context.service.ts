import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { TokenService } from '../auth/token.service';
import { normalizeRoleId } from '../auth/roles/role-definitions';

export interface User {
  id: string;
  username: string;
  email: string;
  /** Canonical role ids from the API (e.g. `ORG_ADMIN`). Use `RoleRegistryService.labelOf` for UI. */
  roles: string[];
}

export interface Organization {
  id: string;
  name: string;
}

export interface Hospital {
  id: string;
  name: string;
  organizationId: string;
}

@Injectable({
  providedIn: 'root',
})
export class UserContextService {
  /** Standard .NET role claim URI emitted by IdentityService. */
  private static readonly ROLE_CLAIM =
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  /** Standard .NET email claim URI, used as the display fallback. */
  private static readonly EMAIL_CLAIM =
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

  private readonly tokenService = inject(TokenService);

  private userSubject = new BehaviorSubject<User | null>(null);
  private organizationSubject = new BehaviorSubject<Organization | null>(null);
  private hospitalsSubject = new BehaviorSubject<Hospital[]>([]);
  private rolesSubject = new BehaviorSubject<string[]>([]);

  public user$ = this.userSubject.asObservable();
  public organization$ = this.organizationSubject.asObservable();
  public hospitals$ = this.hospitalsSubject.asObservable();
  public roles$ = this.rolesSubject.asObservable();

  setUser(user: User): void {
    this.userSubject.next(user);
    this.rolesSubject.next(user.roles || []);
  }

  setOrganization(organization: Organization): void {
    this.organizationSubject.next(organization);
  }

  setHospitals(hospitals: Hospital[]): void {
    this.hospitalsSubject.next(hospitals);
  }

  getUser(): User | null {
    return this.userSubject.value;
  }

  getOrganization(): Organization | null {
    return this.organizationSubject.value;
  }

  getHospitals(): Hospital[] {
    return this.hospitalsSubject.value;
  }

  getUserRoles(): string[] {
    return this.rolesSubject.value;
  }

  /**
   * Rebuild identity and roles from the persisted access token.
   *
   * `setUser` is only reached on the login round-trip, so without this the context is empty after
   * every reload, deep link, bookmark, browser restart or new tab — and `roleGuard` then rejects
   * users who do hold the required role. The JWT is already the source of truth for the API, so it
   * is the right source here too.
   *
   * Idempotent and cheap: a no-op once populated, so it is safe to call from a guard on every
   * navigation. Returns `true` when the context holds a user afterwards.
   */
  hydrateFromToken(): boolean {
    if (this.userSubject.value) {
      return true;
    }

    const token = this.tokenService.getAccessToken();
    if (!token) {
      return false;
    }

    const claims = this.tokenService.decodeJwt(token);
    if (!claims) {
      return false;
    }

    const id: string = claims.sub ?? claims.nameid ?? '';
    if (!id) {
      return false;
    }

    const email: string = claims.email ?? claims[UserContextService.EMAIL_CLAIM] ?? '';

    // The role claim is a single string for one role and an array for several.
    const rawRoles: string[] = [claims[UserContextService.ROLE_CLAIM] ?? claims.role ?? []]
      .flat()
      .filter((r): r is string => typeof r === 'string' && r.trim().length > 0);

    // Keep the raw claim alongside the canonical id — `roleGuard` matches either, and retaining the
    // raw value preserves diagnostics for roles that are not yet in the registry.
    const roles = [...new Set(rawRoles.flatMap((r) => [normalizeRoleId(r) ?? r, r]))];

    this.userSubject.next({
      id,
      email,
      username: claims.preferred_username ?? email,
      roles,
    });
    this.rolesSubject.next(roles);
    return true;
  }

  clear(): void {
    this.userSubject.next(null);
    this.organizationSubject.next(null);
    this.hospitalsSubject.next([]);
    this.rolesSubject.next([]);
  }
}
