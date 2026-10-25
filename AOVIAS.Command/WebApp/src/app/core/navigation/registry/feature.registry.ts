import {
  NavigationFeatureKeys,
  type NavigationFeature,
  type NavigationFeatureKey,
} from '../models/feature.model';

/** Default feature enablement map (stub — all registered features on). */
export const FEATURE_REGISTRY: ReadonlyArray<NavigationFeature> = Object.freeze([
  Object.freeze({ key: NavigationFeatureKeys.ComplianceDashboard, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.ComplianceGraph, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.QualityIntelligence, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.PatientSafety, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.Risks, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.Documentation, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.Reports, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.Audit, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.Knowledge, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.Settings, enabled: true }),
  Object.freeze({ key: NavigationFeatureKeys.AiComplianceCopilot, enabled: true }),
]);

export const DEFAULT_FEATURE_ENABLED: Readonly<Record<NavigationFeatureKey, boolean>> =
  Object.freeze(
    Object.fromEntries(FEATURE_REGISTRY.map((f) => [f.key, f.enabled])) as Record<
      NavigationFeatureKey,
      boolean
    >,
  );
