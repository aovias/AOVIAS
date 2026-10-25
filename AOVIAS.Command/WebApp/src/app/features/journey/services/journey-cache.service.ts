import { Injectable } from '@angular/core';
import { ComplianceStandard } from '../models/standard.model';
import {
  JourneyChaptersIndexDto,
  JourneyDashboardDto,
  JourneySessionDto,
} from '../models/journey-api.model';
import { environment } from '../../../../environments/environment';

interface WorkspaceCacheEntry {
  session: JourneySessionDto;
  dashboard: JourneyDashboardDto;
  chapters: JourneyChaptersIndexDto;
  cachedAtEpochMs: number;
}

@Injectable({
  providedIn: 'root',
})
export class JourneyCacheService {
  private readonly workspaceByKey = new Map<string, WorkspaceCacheEntry>();
  private readonly standardsByKey = new Map<string, ComplianceStandard[]>();
  private readonly workspaceTtlMs = 5 * 60 * 1000;

  private key(journeyId: string, mode: number | null | undefined, stage?: string | null): string {
    return `${journeyId}::${mode ?? 'default'}::${stage ?? 'all'}`;
  }

  private standardsKey(
    journeyId: string,
    chapterId: string,
    mode: number | null | undefined,
    stage?: string | null,
  ): string {
    return `${journeyId}::${chapterId}::${mode ?? 'default'}::${stage ?? 'all'}`;
  }

  getWorkspace(
    journeyId: string,
    mode: number | null | undefined,
    stage?: string | null,
  ): WorkspaceCacheEntry | null {
    const key = this.key(journeyId, mode, stage);
    const inMemory = this.workspaceByKey.get(key);
    if (inMemory) {
      if (Date.now() - inMemory.cachedAtEpochMs <= this.workspaceTtlMs) {
        return inMemory;
      }
      this.workspaceByKey.delete(key);
    }

    if (!environment.journeyCacheSessionStorageEnabled) {
      return null;
    }

    const raw = sessionStorage.getItem(`journey.workspace.${key}`);
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw) as WorkspaceCacheEntry;
      if (Date.now() - parsed.cachedAtEpochMs > this.workspaceTtlMs) {
        sessionStorage.removeItem(`journey.workspace.${key}`);
        return null;
      }
      this.workspaceByKey.set(key, parsed);
      return parsed;
    } catch {
      sessionStorage.removeItem(`journey.workspace.${key}`);
      return null;
    }
  }

  setWorkspace(
    journeyId: string,
    mode: number | null | undefined,
    session: JourneySessionDto,
    dashboard: JourneyDashboardDto,
    chapters: JourneyChaptersIndexDto,
    stage?: string | null,
  ): void {
    const key = this.key(journeyId, mode, stage);
    const entry: WorkspaceCacheEntry = {
      session,
      dashboard,
      chapters,
      cachedAtEpochMs: Date.now(),
    };
    this.workspaceByKey.set(key, entry);
    if (environment.journeyCacheSessionStorageEnabled) {
      sessionStorage.setItem(`journey.workspace.${key}`, JSON.stringify(entry));
    }
  }

  getChapterStandards(
    journeyId: string,
    chapterId: string,
    mode: number | null | undefined,
    stage?: string | null,
  ): ComplianceStandard[] | null {
    const key = this.standardsKey(journeyId, chapterId, mode, stage);
    return this.standardsByKey.get(key) ?? null;
  }

  setChapterStandards(
    journeyId: string,
    chapterId: string,
    mode: number | null | undefined,
    standards: ComplianceStandard[],
    stage?: string | null,
  ): void {
    const key = this.standardsKey(journeyId, chapterId, mode, stage);
    this.standardsByKey.set(key, standards);
  }

  /** Drops cached standards for a journey (e.g. when maturity stage filter changes). */
  clearStandardsForJourney(journeyId: string): void {
    for (const key of this.standardsByKey.keys()) {
      if (key.startsWith(`${journeyId}::`)) {
        this.standardsByKey.delete(key);
      }
    }
  }

  clearJourney(journeyId: string): void {
    for (const key of this.workspaceByKey.keys()) {
      if (key.startsWith(`${journeyId}::`)) {
        this.workspaceByKey.delete(key);
      }
    }

    for (const key of this.standardsByKey.keys()) {
      if (key.startsWith(`${journeyId}::`)) {
        this.standardsByKey.delete(key);
      }
    }

    if (environment.journeyCacheSessionStorageEnabled) {
      const prefix = `journey.workspace.${journeyId}::`;
      for (let i = sessionStorage.length - 1; i >= 0; i--) {
        const key = sessionStorage.key(i);
        if (key?.startsWith(prefix)) {
          sessionStorage.removeItem(key);
        }
      }
    }
  }

  clearAll(): void {
    this.workspaceByKey.clear();
    this.standardsByKey.clear();
    if (environment.journeyCacheSessionStorageEnabled) {
      for (let i = sessionStorage.length - 1; i >= 0; i--) {
        const key = sessionStorage.key(i);
        if (key?.startsWith('journey.workspace.')) {
          sessionStorage.removeItem(key);
        }
      }
    }
  }
}

