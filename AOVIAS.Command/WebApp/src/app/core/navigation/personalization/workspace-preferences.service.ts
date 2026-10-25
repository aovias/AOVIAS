import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SessionService } from '../../auth/session.service';

export type NavDensity = 'comfortable' | 'compact';

export interface WorkspacePreferences {
  readonly density: NavDensity;
  readonly compactMode: boolean;
  readonly animations: boolean;
  readonly showRecent: boolean;
  readonly showFavorites: boolean;
  readonly showPinned: boolean;
  readonly showBadges: boolean;
  readonly maxRecentItems: number;
  readonly preferredLandingPage: string;
  readonly defaultCollapsedSidebar: boolean;
  readonly moduleOrder: readonly string[];
  readonly themePreference: 'system' | 'light' | 'dark' | null;
  readonly searchHistoryRetention: number;
}

const DEFAULTS: WorkspacePreferences = Object.freeze({
  density: 'comfortable',
  compactMode: false,
  animations: true,
  showRecent: true,
  showFavorites: true,
  showPinned: true,
  showBadges: true,
  maxRecentItems: 10,
  preferredLandingPage: '/app/dashboard',
  defaultCollapsedSidebar: false,
  moduleOrder: Object.freeze([]),
  themePreference: null,
  searchHistoryRetention: 12,
});

const BASE_KEY = 'praman.nav.workspacePrefs.v1';

@Injectable({ providedIn: 'root' })
export class WorkspacePreferencesService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly session = inject(SessionService);

  private readonly prefsSig = signal<WorkspacePreferences>(this.load());

  readonly prefs = this.prefsSig.asReadonly();
  readonly density = computed(() => this.prefsSig().density);
  readonly showRecent = computed(() => this.prefsSig().showRecent);
  readonly showFavorites = computed(() => this.prefsSig().showFavorites);
  readonly showPinned = computed(() => this.prefsSig().showPinned);
  readonly showBadges = computed(() => this.prefsSig().showBadges);
  readonly maxRecentItems = computed(() => this.prefsSig().maxRecentItems);
  readonly compactMode = computed(
    () => this.prefsSig().compactMode || this.prefsSig().density === 'compact',
  );
  readonly animations = computed(() => this.prefsSig().animations);

  update(patch: Partial<WorkspacePreferences>): void {
    const next = Object.freeze({ ...this.prefsSig(), ...patch }) as WorkspacePreferences;
    this.prefsSig.set(next);
    this.persist(next);
  }

  reset(): void {
    this.prefsSig.set(DEFAULTS);
    this.persist(DEFAULTS);
  }

  private storageKey(): string {
    const uid = this.session.getUserId()?.trim();
    return uid ? `${BASE_KEY}.${uid}` : BASE_KEY;
  }

  private load(): WorkspacePreferences {
    if (!isPlatformBrowser(this.platformId)) return DEFAULTS;
    try {
      const raw = localStorage.getItem(this.storageKey()) ?? localStorage.getItem(BASE_KEY);
      if (!raw) return DEFAULTS;
      const parsed = JSON.parse(raw) as Partial<WorkspacePreferences>;
      return Object.freeze({ ...DEFAULTS, ...parsed, moduleOrder: parsed.moduleOrder ?? [] });
    } catch {
      return DEFAULTS;
    }
  }

  private persist(value: WorkspacePreferences): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(this.storageKey(), JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }
}
