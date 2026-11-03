import type { LucideIcon } from "lucide-react";
import { Compass, LayoutDashboard, Layers } from "lucide-react";

export type AppNavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  /** Short label for collapsed sidebar tooltips */
  shortLabel?: string;
};

export type AppNavGroup = {
  id: string;
  label: string;
  items: AppNavItem[];
};

/**
 * Navigation reflects implemented in-page sections only (hash routes on `/`).
 * Omitted future modules: Framework Explorer, Saved Knowledge, Learning Centre,
 * Announcements, Settings — no routes or product surfaces exist yet.
 */
export const APP_NAVIGATION: AppNavGroup[] = [
  {
    id: "workspace",
    label: "Workspace",
    items: [
      { href: "#overview", label: "Overview", shortLabel: "Overview", icon: LayoutDashboard },
      {
        href: "#states",
        label: "Presentation states",
        shortLabel: "States",
        icon: Layers,
      },
    ],
  },
  {
    id: "discovery",
    label: "Discovery",
    items: [
      {
        href: "#library",
        label: "Knowledge discovery",
        shortLabel: "Discovery",
        icon: Compass,
      },
    ],
  },
];

export function normalizeAppHash(hash: string): string {
  if (!hash || hash === "#") {
    return "#overview";
  }
  return hash.startsWith("#") ? hash : `#${hash}`;
}

export function isNavItemActive(currentHash: string, itemHref: string): boolean {
  return normalizeAppHash(currentHash) === itemHref;
}
