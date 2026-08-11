import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { CONFIGURATION_CATEGORY } from './audit-configuration.catalogs';
import { ConfigurationApiService } from './configuration-api.service';
import { PlatformConfigurationService } from './platform-configuration.service';

/**
 * These tests exercise the behaviours consumers were migrated onto, not the plumbing.
 *
 * Each one stands in for a defect the pre-migration code actually produced: a status that could not
 * be filtered for, a source column showing a bare ordinal, a Type column blanked by a casing
 * mismatch, an SLA state silently rendered as "closed". Asserting the behaviour rather than the
 * catalog contents means a configuration change that alters ordering or wording does not break the
 * suite, but a change that breaks a screen does.
 */
describe('PlatformConfigurationService', () => {
  let service: PlatformConfigurationService;
  let api: { getCategoryCodes: ReturnType<typeof vi.fn>; getCatalog: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = {
      // Default: the platform reports no categories, so every read runs on the seed.
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

  describe('seeded reads', () => {
    it('serves every registered category before anything is loaded', () => {
      expect(service.values(CONFIGURATION_CATEGORY.findingStatus)().length).toBeGreaterThan(0);
      expect(service.values(CONFIGURATION_CATEGORY.deficiencyStatus)().length).toBeGreaterThan(0);
      expect(service.isConfigurationLoaded()).toBe(false);
    });

    it('returns values in configured order, not declaration order of the store', () => {
      const order = service
        .values(CONFIGURATION_CATEGORY.findingStatus)()
        .map((v) => v.sortOrder);
      expect(order).toEqual([...order].sort((a, b) => a - b));
    });

    it('yields an empty list for a category nobody registered rather than throwing', () => {
      expect(service.values('NotACategory')()).toEqual([]);
      expect(service.label('NotACategory', 'Anything')).toBe('Anything');
    });
  });

  describe('label resolution', () => {
    it('resolves regardless of the casing the API sends', () => {
      const expected = service.auditTypeLabel('InternalAudit');
      expect(service.auditTypeLabel('internalAudit')).toBe(expected);
      expect(service.auditTypeLabel('INTERNAL_AUDIT')).toBe(expected);
    });

    it('humanises a value the platform does not know instead of blanking the cell', () => {
      expect(service.auditTypeLabel('draftReport')).toBe('Draft Report');
    });

    it('falls back only when there is nothing to render', () => {
      expect(service.auditTypeLabel(null)).toBe('—');
      expect(service.auditTypeLabel('')).toBe('—');
    });

    it('resolves a finding source from its backend enum ordinal', () => {
      // FindingRaisedBy is serialised with ToString(), so the wire carries "1", not "InternalAudit".
      expect(service.labelByOrdinalOrCode(CONFIGURATION_CATEGORY.findingSource, '1')).toBe(
        service.label(CONFIGURATION_CATEGORY.findingSource, 'InternalAudit'),
      );
    });

    it('shows the unspecified marker for an ordinal outside the enum', () => {
      // The enum starts at 1; rows holding 0 previously reached the user as a bare "0".
      expect(service.labelByOrdinalOrCode(CONFIGURATION_CATEGORY.findingSource, '0')).toBe('—');
    });
  });

  describe('attribute-driven selection', () => {
    it('offers only schedulable audit types to a planner', () => {
      const schedulable = service.schedulableAuditTypes().map((t) => t.id);
      expect(schedulable).toContain('InternalAudit');
      expect(schedulable).not.toContain('ConsultantReview');
    });

    it('treats "may be scheduled" and "may start a session" as different rules', () => {
      const schedulable = service.schedulableAuditTypes().map((t) => t.id);
      const startable = service
        .valuesWith(CONFIGURATION_CATEGORY.auditType, 'selectableWhenStartingSession')()
        .map((t) => t.id);

      expect(schedulable).toContain('DepartmentAudit');
      expect(startable).not.toContain('DepartmentAudit');
      expect(startable).toContain('ConsultantReview');
    });

    it('identifies surveillance types by attribute, not by a substring of their label', () => {
      expect(service.isSurveillanceAuditType('Surveillance')).toBe(true);
      expect(service.isSurveillanceAuditType('InternalAudit')).toBe(false);
    });

    it('separates the statuses a user may filter by from those a reviewer may set', () => {
      const filterable = service
        .optionsWith(CONFIGURATION_CATEGORY.findingStatus, 'selectableWhenFiltering')()
        .map((o) => o.value);
      const creatable = service
        .optionsWith(CONFIGURATION_CATEGORY.findingStatus, 'selectableWhenCreating')()
        .map((o) => o.value);

      expect(filterable).toContain('Closed');
      expect(creatable).toEqual(['Draft', 'Open']);
    });

    it('keeps Promoted out of every picker — it is a waypoint, not a status', () => {
      for (const attribute of [
        'selectableWhenFiltering',
        'selectableWhenTransitioning',
        'selectableWhenCreating',
      ]) {
        const ids = service
          .valuesWith(CONFIGURATION_CATEGORY.findingStatus, attribute)()
          .map((v) => v.id);
        expect(ids).not.toContain('Promoted');
      }
    });
  });

  describe('attributes', () => {
    it('returns the requested attribute typed by its fallback', () => {
      const days = service.attribute(
        CONFIGURATION_CATEGORY.effectivenessReviewSchedule,
        '60',
        'days',
        0,
      );
      expect(days).toBe(60);
    });

    it('falls back when the attribute is absent or the wrong type', () => {
      expect(service.attribute(CONFIGURATION_CATEGORY.riskLevel, 'High', 'days', 7)).toBe(7);
      expect(service.attribute(CONFIGURATION_CATEGORY.riskLevel, 'Nope', 'badgeClass', '')).toBe(
        '',
      );
    });

    it('resolves an unknown value to the category fallback where one is designated', () => {
      const breached = service.attributeOrFallback(
        CONFIGURATION_CATEGORY.slaStatus,
        'Breached',
        'badgeClass',
      );
      const unknown = service.attributeOrFallback(
        CONFIGURATION_CATEGORY.slaStatus,
        'SomeNewState',
        'badgeClass',
      );

      expect(breached).toContain('breached');
      // The catch-all is explicit configuration now, not the last arm of an if-ladder.
      expect(unknown).toContain('closed');
    });

    it('positions a status within its lifecycle and ignores non-step statuses', () => {
      const category = CONFIGURATION_CATEGORY.deficiencyStatus;
      expect(service.lifecycleIndex(category, 'Identified')).toBe(0);
      expect(service.lifecycleIndex(category, 'Closed')).toBeGreaterThan(0);
      // Cancelled is not a step; index 0 keeps the rail at its start rather than off the end.
      expect(service.lifecycleIndex(category, 'Cancelled')).toBe(0);
    });
  });

  describe('loading', () => {
    it('asks only for categories the platform reports', () => {
      api.getCategoryCodes.mockReturnValue(of(['AuditType']));
      api.getCatalog.mockReturnValue(
        of({
          code: 'AuditType',
          version: 'v1',
          values: [
            {
              id: 'InternalAudit',
              label: 'Internal Review',
              sortOrder: 10,
              attributes: { selectableWhenScheduling: true },
            },
          ],
        }),
      );

      service.load();

      expect(api.getCatalog).toHaveBeenCalledTimes(1);
      expect(api.getCatalog).toHaveBeenCalledWith('AuditType');
      expect(service.auditTypeLabel('InternalAudit')).toBe('Internal Review');
      expect(service.isConfigurationLoaded()).toBe(true);
    });

    it('leaves unreported categories on their seed', () => {
      api.getCategoryCodes.mockReturnValue(of(['AuditType']));
      api.getCatalog.mockReturnValue(of(null));

      const before = service.values(CONFIGURATION_CATEGORY.findingStatus)();
      service.load();
      expect(service.values(CONFIGURATION_CATEGORY.findingStatus)()).toEqual(before);
    });

    it('keeps the seed when the configuration service is unreachable', () => {
      api.getCategoryCodes.mockReturnValue(of([]));
      const before = service.allAuditTypes();
      service.load();
      expect(service.allAuditTypes()).toEqual(before);
    });

    it('loads once per session however many consumers ask', () => {
      api.getCategoryCodes.mockReturnValue(of(['AuditType']));
      service.load();
      service.load();
      expect(api.getCategoryCodes).toHaveBeenCalledTimes(1);
    });

    it('restores the seed on reset so one tenant never sees another’s values', () => {
      api.getCategoryCodes.mockReturnValue(of(['AuditType']));
      api.getCatalog.mockReturnValue(
        of({
          code: 'AuditType',
          version: 'v1',
          values: [{ id: 'InternalAudit', label: 'Tenant Wording', sortOrder: 10, attributes: {} }],
        }),
      );
      service.load();
      expect(service.auditTypeLabel('InternalAudit')).toBe('Tenant Wording');

      service.reset();

      expect(service.auditTypeLabel('InternalAudit')).toBe('Internal');
      expect(service.isConfigurationLoaded()).toBe(false);
    });
  });

  describe('derived signals', () => {
    it('returns the same signal instance for the same request', () => {
      // Components hold these as fields; a fresh array each call would defeat OnPush everywhere.
      expect(service.options(CONFIGURATION_CATEGORY.findingSeverity)).toBe(
        service.options(CONFIGURATION_CATEGORY.findingSeverity),
      );
      expect(service.valuesWith(CONFIGURATION_CATEGORY.auditType, 'surveillance')).toBe(
        service.valuesWith(CONFIGURATION_CATEGORY.auditType, 'surveillance'),
      );
    });

    it('updates bound options when configuration lands', () => {
      const options = service.options(CONFIGURATION_CATEGORY.auditType);
      expect(options().find((o) => o.value === 'InternalAudit')?.label).toBe('Internal');

      api.getCategoryCodes.mockReturnValue(of(['AuditType']));
      api.getCatalog.mockReturnValue(
        of({
          code: 'AuditType',
          version: 'v2',
          values: [{ id: 'InternalAudit', label: 'Renamed', sortOrder: 10, attributes: {} }],
        }),
      );
      service.load();

      expect(options().find((o) => o.value === 'InternalAudit')?.label).toBe('Renamed');
    });
  });
});
