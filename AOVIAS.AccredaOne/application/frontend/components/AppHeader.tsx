"use client";

import { FormEvent, useState } from "react";
import { Menu, Search, ShieldCheck } from "lucide-react";
import { navigateToDiscoverySearch } from "../lib/shell/discoverySearch";

type AppHeaderProps = {
  mobileNavOpen: boolean;
  onMobileNavToggle: () => void;
};

export function AppHeader({ mobileNavOpen, onMobileNavToggle }: AppHeaderProps) {
  const [searchQuery, setSearchQuery] = useState("");

  const onSearchSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    navigateToDiscoverySearch(searchQuery);
  };

  return (
    <header className="ao-header">
      <div className="ao-header-start">
        <button
          className="ao-header-icon-btn ao-header-menu-btn"
          type="button"
          aria-expanded={mobileNavOpen}
          aria-controls="app-sidebar"
          aria-label={mobileNavOpen ? "Close navigation menu" : "Open navigation menu"}
          onClick={onMobileNavToggle}
        >
          <Menu size={20} strokeWidth={2} aria-hidden />
        </button>
        <a className="ao-header-brand" href="#overview">
          <span className="ao-brand-mark" aria-hidden>
            <ShieldCheck size={22} strokeWidth={2} />
          </span>
          <span className="ao-header-brand-text">
            <strong>AccredaOne</strong>
            <span>Knowledge &amp; accreditation intelligence</span>
          </span>
        </a>
      </div>

      <form className="ao-header-search" role="search" onSubmit={onSearchSubmit}>
        <label className="ao-header-search-label" htmlFor="global-discovery-search">
          Search knowledge
        </label>
        <span className="ao-header-search-field">
          <Search size={18} strokeWidth={2} aria-hidden className="ao-header-search-icon" />
          <input
            id="global-discovery-search"
            className="ao-input ao-header-search-input"
            type="search"
            name="q"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Search discovery…"
            autoComplete="off"
          />
        </span>
      </form>

      <p className="ao-header-badge" title="Engineering foundation preview">
        Foundation preview
      </p>
    </header>
  );
}
