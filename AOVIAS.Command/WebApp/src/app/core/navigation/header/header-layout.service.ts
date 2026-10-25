import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type HeaderDensityBand = 'wide' | 'desktop' | 'laptop' | 'tablet' | 'mobile';

/** Action visibility priority (higher = keep visible longer). */
export type HeaderActionId =
  | 'user'
  | 'notifications'
  | 'search'
  | 'refresh'
  | 'theme'
  | 'bookmarks'
  | 'more';

const ACTION_PRIORITY: HeaderActionId[] = [
  'user',
  'notifications',
  'search',
  'refresh',
  'theme',
  'bookmarks',
];

@Injectable({ providedIn: 'root' })
export class HeaderLayoutService {
  private readonly platformId = inject(PLATFORM_ID);

  private readonly widthPx = signal(isPlatformBrowser(this.platformId) ? window.innerWidth : 1600);

  readonly band = computed<HeaderDensityBand>(() => {
    const w = this.widthPx();
    if (w > 1800) return 'wide';
    if (w >= 1400) return 'desktop';
    if (w >= 1100) return 'laptop';
    if (w >= 768) return 'tablet';
    return 'mobile';
  });

  readonly searchIconOnly = computed(() => {
    const b = this.band();
    return b === 'laptop' || b === 'tablet' || b === 'mobile';
  });

  readonly condenseLabels = computed(() => {
    const b = this.band();
    return b === 'desktop' || b === 'laptop' || b === 'tablet' || b === 'mobile';
  });

  readonly showInlineWorkspace = computed(() => {
    const b = this.band();
    return b === 'wide' || b === 'desktop' || b === 'laptop';
  });

  readonly showInlineRole = computed(() => {
    const b = this.band();
    return b === 'wide' || b === 'desktop' || b === 'laptop';
  });

  readonly showInlinePageTitle = computed(() => {
    const b = this.band();
    return b !== 'mobile';
  });

  /**
   * How many trailing low-priority actions hide into More.
   * wide: 0, desktop: bookmarks, laptop+: theme+bookmarks, tablet/mobile: more aggressive
   */
  readonly overflowActionIds = computed<HeaderActionId[]>(() => {
    const b = this.band();
    if (b === 'wide') return [];
    if (b === 'desktop') return ['bookmarks'];
    if (b === 'laptop') return ['bookmarks', 'theme'];
    if (b === 'tablet') return ['bookmarks', 'theme', 'refresh'];
    return ['bookmarks', 'theme', 'refresh', 'search'];
  });

  readonly inlineActionIds = computed(() => {
    const overflow = new Set(this.overflowActionIds());
    return ACTION_PRIORITY.filter((id) => !overflow.has(id));
  });

  constructor() {
    if (!isPlatformBrowser(this.platformId)) return;
    const onResize = () => this.widthPx.set(window.innerWidth);
    window.addEventListener('resize', onResize, { passive: true });
  }

  isInline(id: HeaderActionId): boolean {
    return this.inlineActionIds().includes(id);
  }

  isOverflow(id: HeaderActionId): boolean {
    return this.overflowActionIds().includes(id);
  }
}
