import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { CONFIGURATION_CATEGORY } from './audit-configuration.catalogs';
import { ConfigurationApiService } from './configuration-api.service';
import { PlatformConfigurationService } from './platform-configuration.service';

/**
 * Tenant isolation for platform configuration — Stage 5C.2, Phase 3.
 *
 * <p>
 * Configuration is tenant-scoped and cached for the whole session, which makes the *end* of a
 * session the moment isolation is won or lost. These tests pin that boundary: what a tenant's
 * configuration must do when the session ends, and what it must not do while the session runs.
 * </p>
 *
 * <p>
 * The regression they guard is DEF-CFG-001. `reset()` existed and was correct; nothing called it.
 * Signing out of one organization and into another left the first organization's audit types,
 * statuses, labels and workflow in the store — and because loading is guarded to once per session,
 * the second organization never fetched its own.
 * </p>
 */
describe('platform configuration tenant isolation', () => {
  let service: PlatformConfigurationService;
  let api: { getCategoryCodes: ReturnType<typeof vi.fn>; getCatalog: ReturnType<typeof vi.fn> };

  /** One organization's configured audit-type catalog. */
  const tenantCatalog = (label: string, version: string) => ({
    code: CONFIGURATION_CATEGORY.auditType,
    version,
    values: [
      {
        id: 'InternalAudit',
        label,
        sortOrder: 10,
        attributes: { selectableWhenScheduling: true },
        compound: {},
      },
    ],
  });

  const signIn = (label: string, version: string) => {
    api.getCategoryCodes.mockReturnValue(of([CONFIGURATION_CATEGORY.auditType]));
    api.getCatalog.mockReturnValue(of(tenantCatalog(label, version)));
    service.load();
  };

  beforeEach(() => {
    api = {
      getCategoryCodes: vi.fn(() => of([])),
      getCatalog: vi.fn(() => of(null)),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        PlatformConfigurationService,
        { provide: ConfigurationApiService, useValue: api },
      ],
    });
    service = TestBed.inject(PlatformConfigurationService);
  });

  describe('session boundary', () => {
    it('does not carry one organization’s configuration into the next session', () => {
      signIn('Acme Internal Review', 'acme-v1');
      expect(service.auditTypeLabel('InternalAudit')).toBe('Acme Internal Review');

      // What logout must do.
      service.reset();

      // Before the next organization loads, the platform default is what shows — never Acme's.
      expect(service.auditTypeLabel('InternalAudit')).toBe('Internal');
      expect(service.isConfigurationLoaded()).toBe(false);
    });

    it('lets the next organization load its own configuration', () => {
      // The load-once guard is what made the leak total rather than transient: without a reset the
      // second organization never even asked for its own catalog.
      signIn('Acme Internal Review', 'acme-v1');
      service.reset();
      signIn('Beta Internal Audit', 'beta-v1');

      expect(service.auditTypeLabel('InternalAudit')).toBe('Beta Internal Audit');
    });

    it('reloads for an organization whose configuration happens to carry the same version', () => {
      // Versions are per-tenant hashes. Two tenants can collide, and a version check that survived
      // the reset would treat the second tenant's catalog as "unchanged" and keep the first's.
      signIn('Acme Internal Review', 'shared-version');
      service.reset();
      signIn('Beta Internal Audit', 'shared-version');

      expect(service.auditTypeLabel('InternalAudit')).toBe('Beta Internal Audit');
    });
  });

  describe('within a session', () => {
    it('loads once however many consumers ask', () => {
      signIn('Acme Internal Review', 'acme-v1');
      service.load();
      service.load();
      expect(api.getCategoryCodes).toHaveBeenCalledTimes(1);
    });

    it('picks up an administrator’s edit on refresh, without a reload', () => {
      signIn('Acme Internal Review', 'acme-v1');

      api.getCatalog.mockReturnValue(of(tenantCatalog('Acme Internal Audit', 'acme-v2')));
      service.refresh();

      expect(service.auditTypeLabel('InternalAudit')).toBe('Acme Internal Audit');
    });

    it('ignores a refresh that returns the same version, so bound screens do not churn', () => {
      signIn('Acme Internal Review', 'acme-v1');
      const before = service.allAuditTypes();

      service.refresh();

      // Same array instance: the store was not rewritten, so no consumer re-renders.
      expect(service.allAuditTypes()).toBe(before);
    });

    it('keeps platform defaults when the configuration service is unreachable', () => {
      api.getCategoryCodes.mockReturnValue(of([]));
      service.load();

      expect(service.auditTypeLabel('InternalAudit')).toBe('Internal');
      expect(service.isConfigurationLoaded()).toBe(false);
    });
  });
});
