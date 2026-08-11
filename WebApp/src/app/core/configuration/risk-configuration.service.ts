import { Injectable, computed, inject } from '@angular/core';
import { ADMINISTRATION_CATEGORY } from './administration-configuration.catalogs';
import { CONFIGURATION_CATEGORY } from './audit-configuration.catalogs';
import type { ConfigurationValueDescriptor } from './configuration-catalog';
import { PlatformConfigurationService } from './platform-configuration.service';

/**
 * Risk scoring, as configuration.
 *
 * <p>
 * **Why a service rather than three call sites.** The risk matrix page, the risk form and the CAPA
 * detail panel each need the same three things: the scale, the score, and the band the score falls
 * in. Before this they had none of them in common — the matrix hardcoded `[1,2,3,4,5]` twice, the
 * form hardcoded `min="1" max="5"`, and the CAPA panel held its own `if` ladder for the bands. An
 * organization on a 3×3 matrix could not be expressed at all, which the Administration placeholder
 * page admitted in prose: *"fixed at 5×5 with score = likelihood × impact"*.
 * </p>
 *
 * <p>
 * Same shape as {@link AuditWorkflowEngine}: a thin domain service over the configuration store, so
 * consumers ask rather than decide.
 * </p>
 *
 * <p>
 * **What is configurable and what is not.** The scale (how many points, and what each is called),
 * the bands (where Low becomes Medium) and the band styling are configuration. The *operator* —
 * that score is likelihood multiplied by impact — is not. Making that configurable would mean an
 * expression language for a rule nobody has asked to vary, and the alternative most organizations
 * use, an additive matrix, changes the meaning of every threshold with it. If that need arrives it
 * is a `scoring` attribute here, not a redesign.
 * </p>
 */
@Injectable({ providedIn: 'root' })
export class RiskConfigurationService {
  private readonly configuration = inject(PlatformConfigurationService);

  /** Likelihood points, lowest first, as the form and matrix axis need them. */
  readonly likelihoods = computed(() =>
    levelsOf(this.configuration.values(ADMINISTRATION_CATEGORY.riskLikelihood)()),
  );

  /** Impact points, lowest first. */
  readonly impacts = computed(() =>
    levelsOf(this.configuration.values(ADMINISTRATION_CATEGORY.riskImpact)()),
  );

  /** Likelihood points highest first — the matrix draws its rows top-down from the worst case. */
  readonly likelihoodsDescending = computed(() => [...this.likelihoods()].reverse());

  /** Lowest selectable point on each axis, for form bounds. */
  readonly minimumLevel = computed(() => this.likelihoods()[0] ?? 1);

  /** Highest selectable likelihood and impact, for form bounds. */
  readonly maximumLikelihood = computed(() => this.likelihoods().at(-1) ?? 1);
  readonly maximumImpact = computed(() => this.impacts().at(-1) ?? 1);

  /** The worst score the configured scale can produce. */
  readonly maximumScore = computed(() => this.maximumLikelihood() * this.maximumImpact());

  /** Label for a point on the likelihood scale — "Almost Certain" rather than "5". */
  likelihoodLabel(level: number | null | undefined): string {
    return this.configuration.label(ADMINISTRATION_CATEGORY.riskLikelihood, asCode(level), '');
  }

  /** Label for a point on the impact scale. */
  impactLabel(level: number | null | undefined): string {
    return this.configuration.label(ADMINISTRATION_CATEGORY.riskImpact, asCode(level), '');
  }

  /** The score for a likelihood and impact, or `null` when either is unknown. */
  score(likelihood: number | null | undefined, impact: number | null | undefined): number | null {
    if (likelihood == null || impact == null) return null;
    return likelihood * impact;
  }

  /**
   * The band a score falls in — the highest whose `minScore` the score reaches.
   *
   * Read highest-first rather than lowest-first so bands that overlap, or that leave a gap, still
   * resolve to the more serious of the two. A gap in configured thresholds should never silently
   * downgrade a risk.
   */
  band(score: number | null | undefined): ConfigurationValueDescriptor | null {
    if (score == null) return null;

    return (
      this.bandedLevels().find((level) => score >= Number(level.attributes['minScore'])) ?? null
    );
  }

  /** Band label for a score — "High" — or an empty string when the score is unknown. */
  bandLabel(score: number | null | undefined): string {
    return this.band(score)?.label ?? '';
  }

  /** Band label for a likelihood and impact, resolving the score on the way. */
  bandLabelFor(likelihood: number | null | undefined, impact: number | null | undefined): string {
    return this.bandLabel(this.score(likelihood, impact));
  }

  /** Styling token for a band, by band *label* — what the API returns on a matrix cell. */
  bandToneByLabel(label: string | null | undefined): string {
    return this.configuration.attribute(CONFIGURATION_CATEGORY.riskLevel, label, 'tone', 'neutral');
  }

  /** CSS class for a band, by band label. */
  bandClassByLabel(label: string | null | undefined): string {
    return this.configuration.attribute(CONFIGURATION_CATEGORY.riskLevel, label, 'badgeClass', '');
  }

  /** Risk levels carrying a score band, worst first. */
  private readonly bandedLevels = computed(() =>
    [...this.configuration.values(CONFIGURATION_CATEGORY.riskLevel)()]
      .filter((level) => typeof level.attributes['minScore'] === 'number')
      .sort((a, b) => Number(b.attributes['minScore']) - Number(a.attributes['minScore'])),
  );
}

/** The numeric points of a scale, ascending. */
function levelsOf(values: readonly ConfigurationValueDescriptor[]): readonly number[] {
  return values
    .map((v) => v.attributes['level'])
    .filter((level): level is number => typeof level === 'number')
    .sort((a, b) => a - b);
}

/** Scale values are keyed by their number, so a level resolves by code. */
function asCode(level: number | null | undefined): string | null {
  return level == null ? null : String(level);
}
