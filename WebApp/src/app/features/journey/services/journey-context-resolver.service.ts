import { Injectable, inject } from '@angular/core';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { OrganizationService } from '../../../core/services/organization.service';
import { LoggerService } from '../../../core/services/logger.service';

/**
 * Resolves the active journey id for API calls from hospital + persisted framework/journey selection.
 */
@Injectable({
  providedIn: 'root',
})
export class JourneyContextResolverService {
  private readonly compliance = inject(ComplianceContextService);
  private readonly organization = inject(OrganizationService);
  private readonly logger = inject(LoggerService);

  /**
   * Returns canonical journey UUID for GET /Journeys/workspace, or null if not selectable.
   *
   * Reads the **journey** id, never the framework id — the two are distinct (see
   * {@link ComplianceContextService}). This previously read `getFrameworkId()`, which happened to
   * hold a journey id before the two were separated.
   */
  resolveJourneyId(): string | null {
    const stored = this.compliance.getJourneyId();
    if (!stored) {
      return null;
    }

    const hospitalId = this.compliance.getHospitalId();
    if (!hospitalId) {
      return this.isUuid(stored) ? stored : null;
    }

    const journeys = this.organization.getHospitalJourneys(hospitalId);
    const match = journeys.find((j) => j.journeyId === stored);
    if (match) {
      return match.journeyId;
    }

    if (this.isUuid(stored)) {
      this.logger.warn('Journey id not listed in organization hospital journeys; using stored id', {
        hospitalId,
        stored,
      });
      return stored;
    }

    this.logger.warn('Could not resolve journey id from context', { hospitalId, stored });
    return null;
  }

  private isUuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
      value,
    );
  }
}
