import { Injectable, inject, signal } from '@angular/core';
import { OrganizationService } from './organization.service';

/**
 * Shared framework name/code shown in the shell header (aligned with main content).
 * Updated when hospital/framework context changes (see sidebar + ComplianceContext).
 */
@Injectable({
  providedIn: 'root',
})
export class ContextFrameworkDisplayService {
  private readonly organizationService = inject(OrganizationService);

  readonly frameworkName = signal('Select Framework');
  readonly frameworkCode = signal<string>('');

  updateFromFrameworkId(frameworkId: string): void {
    const hospitals = this.organizationService.getHospitals();
    for (const hospital of hospitals) {
      const journey = (hospital.journey ?? []).find((j) => j.journeyId === frameworkId);
      if (journey) {
        this.frameworkName.set(journey.frameworkName);
        this.frameworkCode.set(journey.frameworkCode);
        return;
      }
    }
  }
}
