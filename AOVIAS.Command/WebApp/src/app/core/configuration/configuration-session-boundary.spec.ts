import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { PlatformConfigurationService } from './platform-configuration.service';
import { TokenService } from '../auth/token.service';
import { SessionService } from '../auth/session.service';
import { OrganizationService } from '../services/organization.service';
import { ComplianceContextService } from '../services/compliance-context.service';
import { AppContextService } from '../services/app-context.service';
import { CommonLookupService } from '../../shared/services/common-lookup.service';
import { OnboardingStateIsolationService } from '../services/onboarding-state-isolation.service';
import { UserContextService } from '../services/user-context.service';
import { HttpClient } from '@angular/common/http';
import { ConfigurationApiService } from './configuration-api.service';
import { CONFIGURATION_CATEGORY } from './audit-configuration.catalogs';

/**
 * The session boundary, tested where the defect actually was — Stage 5C.2, DEF-CFG-001.
 *
 * <p>
 * `PlatformConfigurationService.reset()` was correct all along; nothing called it. That is a class
 * of defect a unit test on the service cannot reach, because the service passes its own tests while
 * the application never invokes it. The assertion has to be about the *call site*, so this spec
 * drives the real `AuthService.logoutLocal()` and asserts configuration was dropped.
 * </p>
 *
 * <p>
 * Collaborators are stubbed to nothing because none of them are under test here: the question is
 * narrow and specific — does ending a session clear tenant configuration.
 * </p>
 */
describe('configuration at the session boundary', () => {
  let auth: AuthService;
  let configuration: PlatformConfigurationService;
  let api: { getCategoryCodes: ReturnType<typeof vi.fn>; getCatalog: ReturnType<typeof vi.fn> };

  const noop = () => ({});

  beforeEach(() => {
    api = {
      getCategoryCodes: vi.fn(() => of([])),
      getCatalog: vi.fn(() => of(null)),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        PlatformConfigurationService,
        { provide: ConfigurationApiService, useValue: api },
        { provide: HttpClient, useValue: { get: () => of(null), post: () => of(null) } },
        { provide: TokenService, useValue: { getRefreshToken: () => null, clearTokens: noop } },
        {
          provide: SessionService,
          useValue: { clearSession: noop, getOrCreateDeviceId: () => 'device' },
        },
        { provide: OrganizationService, useValue: { clearOrganization: noop } },
        { provide: ComplianceContextService, useValue: { clear: noop } },
        { provide: AppContextService, useValue: { clearPersistedSelection: noop } },
        { provide: CommonLookupService, useValue: { clearCaches: noop } },
        { provide: OnboardingStateIsolationService, useValue: { logoutCleanup: noop } },
        { provide: UserContextService, useValue: { clear: noop } },
      ],
    });

    auth = TestBed.inject(AuthService);
    configuration = TestBed.inject(PlatformConfigurationService);
  });

  it('drops tenant configuration when the local session is cleared', () => {
    const reset = vi.spyOn(configuration, 'reset');

    auth.logoutLocal();

    expect(reset).toHaveBeenCalledTimes(1);
  });

  it('leaves the store on platform defaults after logout', () => {
    // Not just "reset was called" — the observable consequence a user would see on the next login.
    api.getCategoryCodes.mockReturnValue(of([CONFIGURATION_CATEGORY.auditType]));
    api.getCatalog.mockReturnValue(
      of({
        code: CONFIGURATION_CATEGORY.auditType,
        version: 'acme-v1',
        values: [
          {
            id: 'InternalAudit',
            label: 'Acme Internal Review',
            sortOrder: 10,
            attributes: {},
            compound: {},
          },
        ],
      }),
    );
    configuration.load();
    expect(configuration.auditTypeLabel('InternalAudit')).toBe('Acme Internal Review');

    auth.logoutLocal();

    expect(configuration.auditTypeLabel('InternalAudit')).toBe('Internal');
    expect(configuration.isConfigurationLoaded()).toBe(false);
  });
});
