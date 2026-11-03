/** Shared between header search and the discovery panel (no API coupling). */
export const DISCOVERY_QUERY_INPUT_ID = "discovery-query-input";

export const DISCOVERY_SEARCH_PREFILL_KEY = "ao-discovery-search-prefill";

export const DISCOVERY_FRAMEWORK_PREFILL_KEY = "ao-discovery-framework-prefill";

export function navigateToDiscoverySearch(prefill?: string) {
  if (typeof window === "undefined") {
    return;
  }
  const trimmed = prefill?.trim();
  if (trimmed) {
    sessionStorage.setItem(DISCOVERY_SEARCH_PREFILL_KEY, trimmed);
  }
  if (window.location.hash !== "#library") {
    window.location.hash = "library";
  } else {
    window.dispatchEvent(new HashChangeEvent("hashchange"));
  }
  window.setTimeout(() => {
    const input = document.getElementById(DISCOVERY_QUERY_INPUT_ID);
    if (input instanceof HTMLTextAreaElement) {
      input.focus({ preventScroll: false });
      input.scrollIntoView({ behavior: "smooth", block: "center" });
    }
  }, 50);
}

export function navigateToDiscoveryFramework(frameworkCode: string) {
  if (typeof window === "undefined") {
    return;
  }
  sessionStorage.setItem(DISCOVERY_FRAMEWORK_PREFILL_KEY, frameworkCode);
  navigateToDiscoverySearch();
}
