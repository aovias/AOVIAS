"use client";

import { useCallback, useEffect, useState } from "react";
import { AppFooter } from "./AppFooter";
import { AppHeader } from "./AppHeader";
import { AppSidebar } from "./AppSidebar";
import { useLocationHash } from "../lib/shell/useLocationHash";

export function AppShell({ children }: { children: React.ReactNode }) {
  const activeHash = useLocationHash();
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const closeMobileNav = useCallback(() => setMobileNavOpen(false), []);

  useEffect(() => {
    if (!mobileNavOpen) {
      document.documentElement.classList.remove("ao-scroll-lock");
      return;
    }
    document.documentElement.classList.add("ao-scroll-lock");
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMobileNavOpen(false);
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.documentElement.classList.remove("ao-scroll-lock");
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [mobileNavOpen]);

  const shellClass = [
    "ao-shell",
    sidebarCollapsed ? "ao-shell-sidebar-collapsed" : "",
    mobileNavOpen ? "ao-shell-mobile-nav-open" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={shellClass}>
      <AppHeader
        mobileNavOpen={mobileNavOpen}
        onMobileNavToggle={() => setMobileNavOpen((open) => !open)}
      />
      {mobileNavOpen ? (
        <button
          type="button"
          className="ao-sidebar-backdrop"
          aria-label="Close navigation menu"
          onClick={closeMobileNav}
        />
      ) : null}
      <div className="ao-body">
        <AppSidebar
          activeHash={activeHash}
          mobileOpen={mobileNavOpen}
          collapsed={sidebarCollapsed}
          onMobileClose={closeMobileNav}
          onCollapsedChange={setSidebarCollapsed}
        />
        <main className="ao-main" id="main-content">
          {children}
          <AppFooter />
        </main>
      </div>
    </div>
  );
}
