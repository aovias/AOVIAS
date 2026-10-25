import { Injectable, signal, inject, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/** <768 — mobile drawer. */
export const SHELL_SIDEBAR_DRAWER_MAX_PX = 767;

/** 768–1023 — tablet overlay. */
export const SHELL_SIDEBAR_OVERLAY_MAX_PX = 1023;
export const SHELL_SIDEBAR_OVERLAY_MIN_PX = 768;

/** 1024–1399 — laptop; collapsed by default when no persisted preference. */
export const SHELL_SIDEBAR_LAPTOP_MIN_PX = 1024;
export const SHELL_SIDEBAR_LAPTOP_MAX_PX = 1399;

/** ≥1400 — desktop/wide expanded default. */
export const SHELL_SIDEBAR_DESKTOP_MIN_PX = 1400;

/** ≥1600 — in-flow flex rail (no fixed positioning / content padding). */
export const SHELL_SIDEBAR_WIDE_MIN_PX = 1600;

/** @deprecated Use SHELL_SIDEBAR_OVERLAY_MIN_PX — kept for any external imports. */
export const SHELL_TABLET_INLINE_LAYOUT_MIN_PX = SHELL_SIDEBAR_OVERLAY_MIN_PX;
/** @deprecated Overlay/tablet band ends at 1023 in NAV-1. */
export const SHELL_TABLET_INLINE_LAYOUT_MAX_PX = SHELL_SIDEBAR_OVERLAY_MAX_PX;

export const SHELL_SIDEBAR_EXPANDED_PX = 240;
export const SHELL_SIDEBAR_COLLAPSED_PX = 72;

/** Persisted intentional rail collapse (not peek). */
export const SHELL_SIDEBAR_RAIL_COLLAPSED_STORAGE_KEY = 'praman-sidebar-collapsed';

@Injectable({
  providedIn: 'root',
})
export class ShellSidebarService {
  private readonly platformId = inject(PLATFORM_ID);

  /** Drawer/overlay open (cleared when leaving overlay/drawer modes). */
  readonly isOpen = signal(false);

  /**
   * True when viewport uses overlay or drawer (max-width 1023).
   * Mobile drawer and tablet overlay share the same open/close API.
   */
  readonly isOverlayMode = signal(false);

  /** True when viewport is mobile drawer band (≤767). */
  readonly isDrawerMode = signal(false);

  /** True when 1024–1399 — used for default-collapsed policy. */
  private readonly isLaptopBand = signal(false);

  /**
   * True when overlay/drawer is off — fixed/padded rail layout for content.
   * NAV-1: any non-overlay viewport uses reserved column padding when needed.
   */
  private readonly shellTabletInlineLayout = signal(false);

  private readonly sidebarRailCollapsed = signal(false);

  /**
   * Temporary expand while collapsed + pointer hover. Does not change persisted preference.
   */
  private readonly sidebarPeekExpanded = signal(false);

  readonly shellContentInlineStartPaddingPx = computed(() => {
    if (!this.shellTabletInlineLayout() || this.isOverlayMode()) {
      return 0;
    }
    return this.shellSidebarColumnPx();
  });

  /**
   * Effective rail column width: 0 overlay/drawer, 72 collapsed (not peeking), 240 expanded/peek.
   */
  readonly shellSidebarColumnPx = computed(() => {
    if (this.isOverlayMode()) {
      return 0;
    }
    if (this.sidebarRailCollapsed() && !this.sidebarPeekExpanded()) {
      return SHELL_SIDEBAR_COLLAPSED_PX;
    }
    return SHELL_SIDEBAR_EXPANDED_PX;
  });

  /** Labels hidden when collapsed and not peeking (desktop rail only). */
  readonly labelsHidden = computed(
    () => !this.isOverlayMode() && this.sidebarRailCollapsed() && !this.sidebarPeekExpanded(),
  );

  setSidebarRailCollapsed(collapsed: boolean): void {
    this.sidebarRailCollapsed.set(collapsed);
  }

  toggleSidebarRailCollapsed(): void {
    this.sidebarRailCollapsed.update((c) => {
      const next = !c;
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem(SHELL_SIDEBAR_RAIL_COLLAPSED_STORAGE_KEY, next ? '1' : '0');
      }
      if (next) {
        this.sidebarPeekExpanded.set(false);
      }
      return next;
    });
  }

  readonly isSidebarRailCollapsed = computed(() => this.sidebarRailCollapsed());
  readonly isSidebarPeekExpanded = computed(() => this.sidebarPeekExpanded());

  setPeekExpanded(peek: boolean): void {
    if (!this.sidebarRailCollapsed() || this.isOverlayMode()) {
      this.sidebarPeekExpanded.set(false);
      return;
    }
    this.sidebarPeekExpanded.set(peek);
  }

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const stored = localStorage.getItem(SHELL_SIDEBAR_RAIL_COLLAPSED_STORAGE_KEY);
    const mqOverlay = window.matchMedia(`(max-width: ${SHELL_SIDEBAR_OVERLAY_MAX_PX}px)`);
    const mqDrawer = window.matchMedia(`(max-width: ${SHELL_SIDEBAR_DRAWER_MAX_PX}px)`);
    const mqLaptop = window.matchMedia(
      `(min-width: ${SHELL_SIDEBAR_LAPTOP_MIN_PX}px) and (max-width: ${SHELL_SIDEBAR_LAPTOP_MAX_PX}px)`,
    );
    /** Fixed rail + content padding band (1024–1599). */
    const mqFixedRail = window.matchMedia(
      `(min-width: ${SHELL_SIDEBAR_LAPTOP_MIN_PX}px) and (max-width: ${SHELL_SIDEBAR_WIDE_MIN_PX - 1}px)`,
    );

    this.isOverlayMode.set(mqOverlay.matches);
    this.isDrawerMode.set(mqDrawer.matches);
    this.isLaptopBand.set(mqLaptop.matches);
    this.shellTabletInlineLayout.set(mqFixedRail.matches);

    if (stored === '1' || stored === '0') {
      this.sidebarRailCollapsed.set(stored === '1');
    } else {
      // First visit: laptop band defaults collapsed; desktop/wide defaults expanded.
      this.sidebarRailCollapsed.set(mqLaptop.matches);
    }

    if (!mqOverlay.matches) {
      this.isOpen.set(false);
    }

    mqOverlay.addEventListener('change', (e) => {
      this.isOverlayMode.set(e.matches);
      if (!e.matches) {
        this.isOpen.set(false);
        this.sidebarPeekExpanded.set(false);
      }
    });

    mqDrawer.addEventListener('change', (e) => {
      this.isDrawerMode.set(e.matches);
    });

    mqLaptop.addEventListener('change', (e) => {
      this.isLaptopBand.set(e.matches);
    });

    mqFixedRail.addEventListener('change', (e) => {
      this.shellTabletInlineLayout.set(e.matches);
    });
  }

  toggle(): void {
    if (!this.isOverlayMode()) {
      return;
    }
    this.isOpen.update((v) => !v);
  }

  open(): void {
    if (!this.isOverlayMode()) {
      return;
    }
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
  }
}
