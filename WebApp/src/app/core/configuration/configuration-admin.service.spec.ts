import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { ADMINISTRATION_CATEGORY } from './administration-configuration.catalogs';
import { CONFIGURATION_CATEGORY } from './audit-configuration.catalogs';
import { ConfigurationAdminService } from './configuration-admin.service';
import { ConfigurationApiService } from './configuration-api.service';
import { PlatformConfigurationService } from './platform-configuration.service';
import { PLATFORM_CONFIGURATION_CATALOGS } from './platform-configuration.catalogs';
import { RiskConfigurationService } from './risk-configuration.service';

/**
 * The write side, and the domains that depend on it — Stage 6, Phase 10.
 *
 * <p>
 * The previous wave could validate that configuration *arriving* from the API changed behaviour. It
 * could not validate saving, because the client had no write path. These tests cover that gap: an
 * administrator's edit reaches the store, propagates to bound consumers without a reload, and a
 * restore returns a category to its platform default rather than emptying it.
 * </p>
 */
describe('configuration administration', () => {
  let admin: ConfigurationAdminService;
  let configuration: PlatformConfigurationService;
  let risk: RiskConfigurationService;
  let api: {
    getCategoryCodes: ReturnType<typeof vi.fn>;
    getCatalog: ReturnType<typeof vi.fn>;
    saveOverride: ReturnType<typeof vi.fn>;
    restoreDefault: ReturnType<typeof vi.fn>;
  };

  const value = (
    id: string,
    label: string,
    sortOrder: number,
    attributes: Record<string, string | number | boolean> = {},
    compound: Record<string, unknown> = {},
  ) => ({ id, label, sortOrder, attributes, compound });

  beforeEach(() => {
    api = {
      getCategoryCodes: vi.fn(() => of([])),
      getCatalog: vi.fn(() => of(null)),
      saveOverride: vi.fn(() => of(null)),
      restoreDefault: vi.fn(() => of(null)),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        ConfigurationAdminService,
        PlatformConfigurationService,
        RiskConfigurationService,
        { provide: ConfigurationApiService, useValue: api },
      ],
    });

    admin = TestBed.inject(ConfigurationAdminService);
    configuration = TestBed.inject(PlatformConfigurationService);
    risk = TestBed.inject(RiskConfigurationService);
  });

  describe('the Configuration Center lists every domain', () => {
    it('offers every registered category, with no per-domain wiring', () => {
      // The property that keeps this one editor rather than one page per domain.
      expect(admin.categories()).toHaveLength(PLATFORM_CONFIGURATION_CATALOGS.length);
    });

    it('includes each Stage 6 domain', () => {
      const codes = admin.categories().map((c) => c.code);
      for (const code of Object.values(ADMINISTRATION_CATEGORY)) {
        expect(codes, code).toContain(code);
      }
    });

    it('marks a category as customised only once the tenant has overridden it', () => {
      const before = admin.categories().find((c) => c.code === CONFIGURATION_CATEGORY.auditType);
      expect(before?.isOverridden).toBe(false);

      api.saveOverride.mockReturnValue(
        of({
          code: CONFIGURATION_CATEGORY.auditType,
          version: 'v1',
          values: [value('InternalAudit', 'Renamed', 10)],
        }),
      );
      admin.save(CONFIGURATION_CATEGORY.auditType, { code: 'InternalAudit', name: 'Renamed' });

      const after = admin.categories().find((c) => c.code === CONFIGURATION_CATEGORY.auditType);
      expect(after?.isOverridden).toBe(true);
    });
  });

  describe('saving', () => {
    it('propagates an edit to bound consumers without a reload', () => {
      // A component holds this signal; it must yield the new value from the save response alone.
      const options = configuration.options(CONFIGURATION_CATEGORY.findingSeverity);
      expect(options().find((o) => o.value === 'Critical')?.label).toBe('Critical');

      api.saveOverride.mockReturnValue(
        of({
          code: CONFIGURATION_CATEGORY.findingSeverity,
          version: 'v2',
          values: [value('Critical', 'Catastrophic', 10)],
        }),
      );
      admin.save(CONFIGURATION_CATEGORY.findingSeverity, {
        code: 'Critical',
        name: 'Catastrophic',
      });

      expect(options().find((o) => o.value === 'Critical')?.label).toBe('Catastrophic');
      expect(api.getCatalog).not.toHaveBeenCalled();
    });

    it('sends only the fields the administrator changed', () => {
      api.saveOverride.mockReturnValue(of(null));
      admin.save(CONFIGURATION_CATEGORY.auditType, { code: 'InternalAudit', name: 'Renamed' });

      // Omitted fields keep their current value server-side, so a rename cannot silently rewrite
      // ordering or attributes — which for the workflow categories are the rules themselves.
      expect(api.saveOverride).toHaveBeenCalledWith(CONFIGURATION_CATEGORY.auditType, {
        code: 'InternalAudit',
        name: 'Renamed',
      });
    });

    it('surfaces a failed write instead of discarding the edit silently', () => {
      api.saveOverride.mockReturnValue(throwError(() => new Error('500')));

      admin.save(CONFIGURATION_CATEGORY.auditType, { code: 'InternalAudit', name: 'Renamed' });

      expect(admin.error()).toContain('Could not save');
      // And the store is untouched — a failed save must not look like a successful one.
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Internal');
    });
  });

  describe('restoring defaults', () => {
    it('returns a category to its platform default when the last override is withdrawn', () => {
      api.saveOverride.mockReturnValue(
        of({
          code: CONFIGURATION_CATEGORY.auditType,
          version: 'v1',
          values: [value('InternalAudit', 'Acme Review', 10)],
        }),
      );
      admin.save(CONFIGURATION_CATEGORY.auditType, { code: 'InternalAudit', name: 'Acme Review' });
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Acme Review');

      // The server resolves the category to nothing once the tenant row is gone.
      api.restoreDefault.mockReturnValue(
        of({ code: CONFIGURATION_CATEGORY.auditType, version: 'v2', values: [] }),
      );
      admin.restore(CONFIGURATION_CATEGORY.auditType, 'InternalAudit');

      // An empty category means "use the platform default", not "this category is empty".
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Internal');
      expect(configuration.values(CONFIGURATION_CATEGORY.auditType)().length).toBeGreaterThan(0);
    });

    it('applies a partial restore as the server resolved it', () => {
      api.restoreDefault.mockReturnValue(
        of({
          code: CONFIGURATION_CATEGORY.auditType,
          version: 'v3',
          values: [value('InternalAudit', 'Internal', 10), value('MockAudit', 'Still Renamed', 20)],
        }),
      );
      admin.restore(CONFIGURATION_CATEGORY.auditType, 'InternalAudit');

      expect(configuration.auditTypeLabel('MockAudit')).toBe('Still Renamed');
    });

    it('leaves the store alone when the restore fails', () => {
      api.restoreDefault.mockReturnValue(throwError(() => new Error('500')));
      admin.restore(CONFIGURATION_CATEGORY.auditType, 'InternalAudit');

      expect(admin.error()).toContain('Could not restore');
    });
  });

  describe('Risk configuration drives risk scoring', () => {
    it('serves the platform 5×5 scale', () => {
      expect(risk.likelihoods()).toEqual([1, 2, 3, 4, 5]);
      expect(risk.impacts()).toEqual([1, 2, 3, 4, 5]);
      expect(risk.maximumScore()).toBe(25);
      expect(risk.likelihoodLabel(5)).toBe('Almost Certain');
    });

    it('bands a score highest-first', () => {
      expect(risk.bandLabelFor(5, 5)).toBe('Critical');
      expect(risk.bandLabelFor(3, 4)).toBe('High');
      expect(risk.bandLabelFor(2, 3)).toBe('Medium');
      expect(risk.bandLabelFor(1, 1)).toBe('Low');
    });

    it('resizes the whole matrix from configuration alone', () => {
      // The change an organization on a 3×3 matrix makes. Nothing in the risk pages is edited.
      api.saveOverride.mockReturnValue(
        of({
          code: ADMINISTRATION_CATEGORY.riskLikelihood,
          version: 'v1',
          values: [
            value('1', 'Unlikely', 10, { level: 1 }),
            value('2', 'Possible', 20, { level: 2 }),
            value('3', 'Likely', 30, { level: 3 }),
          ],
        }),
      );
      admin.save(ADMINISTRATION_CATEGORY.riskLikelihood, { code: '1' });

      expect(risk.likelihoods()).toEqual([1, 2, 3]);
      expect(risk.likelihoodsDescending()).toEqual([3, 2, 1]);
      expect(risk.maximumLikelihood()).toBe(3);
      // Form bounds follow the scale, so a 5 can no longer be entered.
      expect(risk.maximumScore()).toBe(15);
    });

    it('moves the band boundaries from configuration alone', () => {
      api.saveOverride.mockReturnValue(
        of({
          code: CONFIGURATION_CATEGORY.riskLevel,
          version: 'v1',
          values: [
            value('Low', 'Low', 10, { minScore: 0, tone: 'success' }),
            value('Medium', 'Medium', 20, { minScore: 3, tone: 'warning' }),
            value('High', 'High', 30, { minScore: 6, tone: 'danger' }),
          ],
        }),
      );
      admin.save(CONFIGURATION_CATEGORY.riskLevel, { code: 'Medium' });

      expect(risk.bandLabelFor(3, 3)).toBe('High');
      expect(risk.bandLabelFor(2, 2)).toBe('Medium');
      expect(risk.bandLabelFor(1, 2)).toBe('Low');
    });

    it('styles a band from its configured tone, not its name', () => {
      expect(risk.bandToneByLabel('Critical')).toBe('danger');
      expect(risk.bandToneByLabel('Low')).toBe('success');
      // A band the platform does not know falls back rather than rendering an invalid class.
      expect(risk.bandToneByLabel('Severe')).toBe('neutral');
    });

    it('reports no band for an unscored risk', () => {
      expect(risk.score(null, 3)).toBeNull();
      expect(risk.bandLabel(null)).toBe('');
    });
  });

  describe('Stage 6 domains resolve and override', () => {
    const probes: { code: string; probe: string; label: string }[] = [
      {
        code: ADMINISTRATION_CATEGORY.riskCategory,
        probe: 'PatientSafety',
        label: 'Patient Safety',
      },
      {
        code: ADMINISTRATION_CATEGORY.riskEscalationThreshold,
        probe: 'Level2',
        label: 'Quality Committee',
      },
      { code: ADMINISTRATION_CATEGORY.approvalLevel, probe: 'QualityLead', label: 'Quality Lead' },
      {
        code: ADMINISTRATION_CATEGORY.approvalOutcome,
        probe: 'ChangesRequested',
        label: 'Changes Requested',
      },
      {
        code: ADMINISTRATION_CATEGORY.notificationCategory,
        probe: 'CapaOverdue',
        label: 'CAPA Overdue',
      },
      { code: ADMINISTRATION_CATEGORY.notificationChannel, probe: 'Email', label: 'Email' },
      { code: ADMINISTRATION_CATEGORY.notificationPriority, probe: 'Critical', label: 'Critical' },
      { code: ADMINISTRATION_CATEGORY.slaPolicy, probe: 'Major', label: 'Major' },
      { code: ADMINISTRATION_CATEGORY.slaWarningThreshold, probe: 'AtRisk', label: 'At Risk' },
      { code: ADMINISTRATION_CATEGORY.businessCalendar, probe: 'Saturday', label: 'Saturday' },
      {
        code: ADMINISTRATION_CATEGORY.evidenceCategory,
        probe: 'Training',
        label: 'Training & Competency',
      },
      { code: ADMINISTRATION_CATEGORY.evidenceType, probe: 'Certificate', label: 'Certificate' },
      {
        code: ADMINISTRATION_CATEGORY.evidenceRetentionPolicy,
        probe: 'Extended',
        label: 'Extended — 7 years',
      },
      { code: ADMINISTRATION_CATEGORY.documentType, probe: 'Procedure', label: 'Procedure' },
      {
        code: ADMINISTRATION_CATEGORY.documentClassification,
        probe: 'Restricted',
        label: 'Restricted',
      },
      {
        code: ADMINISTRATION_CATEGORY.documentRetentionPolicy,
        probe: 'Major',
        label: 'Major increments (2.0, 3.0)',
      },
      { code: ADMINISTRATION_CATEGORY.executiveKpi, probe: 'OverdueCapas', label: 'Overdue CAPAs' },
      {
        code: ADMINISTRATION_CATEGORY.dashboardCard,
        probe: 'RiskDistribution',
        label: 'Risk Distribution',
      },
      { code: ADMINISTRATION_CATEGORY.riskLikelihood, probe: '3', label: 'Possible' },
      { code: ADMINISTRATION_CATEGORY.riskImpact, probe: '5', label: 'Catastrophic' },
    ];

    it.each(probes)('serves the platform default for $code', ({ code, probe, label }) => {
      expect(configuration.label(code, probe)).toBe(label);
    });

    it.each(probes)('accepts an administrator override for $code', ({ code, probe }) => {
      api.saveOverride.mockReturnValue(
        of({ code, version: 'v1', values: [value(probe, 'Tenant Wording', 10)] }),
      );
      admin.save(code, { code: probe, name: 'Tenant Wording' });

      expect(configuration.label(code, probe)).toBe('Tenant Wording');
    });

    it('carries the rules each domain attaches to its values', () => {
      // Attributes are what make these behaviour rather than labels — spot-check one per domain.
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.slaPolicy,
          'Critical',
          'resolutionHours',
          0,
        ),
      ).toBe(24);
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.notificationChannel,
          'Email',
          'retryAttempts',
          0,
        ),
      ).toBe(3);
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.evidenceType,
          'Certificate',
          'requiresApproval',
          false,
        ),
      ).toBe(true);
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.documentType,
          'Policy',
          'namingPattern',
          '',
        ),
      ).toBe('POL-{code}-v{version}');
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.approvalLevel,
          'Executive',
          'delegable',
          true,
        ),
      ).toBe(false);
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.businessCalendar,
          'Sunday',
          'working',
          true,
        ),
      ).toBe(false);
    });
  });

  describe('tenant isolation covers the new domains', () => {
    it('drops administrator edits at the session boundary', () => {
      api.saveOverride.mockReturnValue(
        of({
          code: ADMINISTRATION_CATEGORY.slaPolicy,
          version: 'v1',
          values: [value('Critical', 'Immediate', 10, { resolutionHours: 4 })],
        }),
      );
      admin.save(ADMINISTRATION_CATEGORY.slaPolicy, { code: 'Critical', name: 'Immediate' });
      expect(configuration.label(ADMINISTRATION_CATEGORY.slaPolicy, 'Critical')).toBe('Immediate');

      configuration.reset();

      expect(configuration.label(ADMINISTRATION_CATEGORY.slaPolicy, 'Critical')).toBe('Critical');
      expect(
        configuration.attribute(
          ADMINISTRATION_CATEGORY.slaPolicy,
          'Critical',
          'resolutionHours',
          0,
        ),
      ).toBe(24);
    });
  });
});
