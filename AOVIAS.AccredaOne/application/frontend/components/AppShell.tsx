"use client";

import { useState } from "react";

const links = [
  { href: "#overview", label: "Workspace" },
  { href: "#library", label: "Knowledge library" },
  { href: "#states", label: "Presentation states" },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useState(false);

  return (
    <div className="ao-shell">
      <header className="ao-header">
        <button
          className="ao-menu"
          type="button"
          aria-expanded={open}
          aria-controls="app-sidebar"
          onClick={() => setOpen((value) => !value)}
        >
          Menu
        </button>
        <div className="ao-brand">
          <strong>AccredaOne</strong>
          <span>Accreditation knowledge discovery and explanation</span>
        </div>
        <p className="ao-header-note">Engineering foundation · not a live accreditation system</p>
      </header>
      <div className="ao-body">
        <aside id="app-sidebar" className={open ? "ao-sidebar ao-sidebar-open" : "ao-sidebar"}>
          <p>Navigation</p>
          <nav aria-label="Application">
            {links.map((link) => (
              <a key={link.href} href={link.href} onClick={() => setOpen(false)}>
                {link.label}
              </a>
            ))}
          </nav>
        </aside>
        <main className="ao-main">{children}</main>
      </div>
    </div>
  );
}
