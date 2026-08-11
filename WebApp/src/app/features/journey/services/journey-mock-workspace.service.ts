import { Injectable } from '@angular/core';
import { OperationalMode } from '../models/nabh.enums';
import { JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import { buildMockWorkspaceSnapshot } from '../mappers/mock-journey.mapper';

/**
 * Offline NABH-shaped workspace (Phase 36). Replaces HTTP {@link JourneyDataService} for Journey UI.
 */
@Injectable({
  providedIn: 'root',
})
export class JourneyMockWorkspaceService {
  private cachedJourneyId: string | null = null;
  private cachedLifecycle: OperationalMode | null = null;
  private snapshot: JourneyWorkspaceSnapshot | null = null;

  getWorkspace(journeyId: string, lifecycleMode: OperationalMode): JourneyWorkspaceSnapshot {
    if (
      this.snapshot &&
      this.cachedJourneyId === journeyId &&
      this.cachedLifecycle === lifecycleMode
    ) {
      return this.snapshot;
    }
    this.cachedJourneyId = journeyId;
    this.cachedLifecycle = lifecycleMode;
    this.snapshot = buildMockWorkspaceSnapshot(journeyId, lifecycleMode);
    return this.snapshot;
  }

  clearCache(): void {
    this.snapshot = null;
    this.cachedJourneyId = null;
    this.cachedLifecycle = null;
  }
}
