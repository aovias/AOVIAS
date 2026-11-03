"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { APP_NAVIGATION, isNavItemActive } from "../lib/shell/navigation";

type AppSidebarProps = {
  activeHash: string;
  mobileOpen: boolean;
  collapsed: boolean;
  onMobileClose: () => void;
  onCollapsedChange: (collapsed: boolean) => void;
};

export function AppSidebar({
  activeHash,
  mobileOpen,
  collapsed,
  onMobileClose,
  onCollapsedChange,
}: AppSidebarProps) {
  const sidebarClass = [
    "ao-sidebar",
    mobileOpen ? "ao-sidebar-open" : "",
    collapsed ? "ao-sidebar-collapsed" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <aside id="app-sidebar" className={sidebarClass} aria-label="Application navigation">
      <div className="ao-sidebar-toolbar">
        <button
          type="button"
          className="ao-sidebar-collapse-btn ao-header-icon-btn"
          aria-expanded={!collapsed}
          aria-controls="app-sidebar"
          aria-label={collapsed ? "Expand sidebar navigation" : "Collapse sidebar navigation"}
          onClick={() => onCollapsedChange(!collapsed)}
        >
          {collapsed ? (
            <ChevronRight size={18} strokeWidth={2} aria-hidden />
          ) : (
            <ChevronLeft size={18} strokeWidth={2} aria-hidden />
          )}
        </button>
      </div>

      {APP_NAVIGATION.map((group) => (
        <div key={group.id} className="ao-nav-group">
          <p className="ao-nav-group-label">{group.label}</p>
          <nav aria-label={group.label}>
            <ul className="ao-nav-list">
              {group.items.map((item) => {
                const active = isNavItemActive(activeHash, item.href);
                const Icon = item.icon;
                const tooltip = collapsed ? item.shortLabel ?? item.label : undefined;
                return (
                  <li key={item.href}>
                    <a
                      href={item.href}
                      className={active ? "ao-nav-link ao-nav-link-active" : "ao-nav-link"}
                      aria-current={active ? "page" : undefined}
                      title={tooltip}
                      onClick={onMobileClose}
                    >
                      <Icon size={20} strokeWidth={2} aria-hidden className="ao-nav-icon" />
                      <span className="ao-nav-link-label">{item.label}</span>
                    </a>
                  </li>
                );
              })}
            </ul>
          </nav>
        </div>
      ))}
    </aside>
  );
}
