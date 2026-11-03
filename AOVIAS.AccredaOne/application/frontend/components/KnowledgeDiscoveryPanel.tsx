"use client";

import { FormEvent, useCallback, useState } from "react";
import { postDiscoveryQuery } from "../lib/discovery/client";
import { ELIGIBLE_FRAMEWORK_OPTIONS } from "../lib/discovery/frameworkOptions";
import { DiscoveryApiError, type DiscoveryQueryResponse } from "../lib/discovery/types";

type ViewState = "initial" | "loading" | "results" | "no_results" | "error" | "validation";

export function KnowledgeDiscoveryPanel() {
  const [query, setQuery] = useState("");
  const [frameworkCode, setFrameworkCode] = useState<string>("");
  const [view, setView] = useState<ViewState>("initial");
  const [validationMessage, setValidationMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [response, setResponse] = useState<DiscoveryQueryResponse | null>(null);

  const submit = useCallback(async () => {
    const trimmed = query.trim();
    if (!trimmed) {
      setValidationMessage("Enter a question before searching.");
      setView("validation");
      return;
    }

    setValidationMessage(null);
    setErrorMessage(null);
    setView("loading");
    setResponse(null);

    try {
      const result = await postDiscoveryQuery(trimmed, frameworkCode || null);
      setResponse(result);
      if (result.outcome === "no_results" || result.frameworkGroups.length === 0) {
        setView("no_results");
      } else {
        setView("results");
      }
    } catch (error) {
      setView("error");
      if (error instanceof DiscoveryApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Something unexpected went wrong. Try again.");
      }
    }
  }, [query, frameworkCode]);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (view === "loading") {
      return;
    }
    void submit();
  };

  const isLoading = view === "loading";

  return (
    <section id="library" className="ao-panel ao-discovery">
      <p className="ao-kicker">Knowledge discovery</p>
      <h2>Discover requirements</h2>
      <p className="ao-discovery-intro">
        Ask a natural-language question to search the development synthetic dataset. Results are
        fictional engineering placeholders — not official accreditation standards, scores, or
        compliance decisions.
      </p>
      <p className="ao-discovery-dataset" role="note">
        <strong>Development data only:</strong> responses come from pack{" "}
        <code>mvp-development-synthetic-discovery</code> (classification: synthetic).
      </p>

      <form className="ao-discovery-form" onSubmit={onSubmit} noValidate>
        <label className="ao-discovery-field">
          <span>Your question</span>
          <textarea
            name="query"
            rows={3}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="e.g. What practices relate to operational assurance?"
            aria-invalid={view === "validation"}
            disabled={isLoading}
          />
        </label>
        <label className="ao-discovery-field">
          <span>Framework (optional)</span>
          <select
            name="frameworkCode"
            value={frameworkCode}
            onChange={(event) => setFrameworkCode(event.target.value)}
            disabled={isLoading}
          >
            <option value="">All eligible frameworks (cross-framework discovery)</option>
            {ELIGIBLE_FRAMEWORK_OPTIONS.map((option) => (
              <option key={option.code} value={option.code}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <div className="ao-discovery-actions">
          <button type="submit" className="ao-discovery-submit" disabled={isLoading}>
            {isLoading ? "Searching…" : "Search"}
          </button>
          {view === "error" && (
            <button type="button" className="ao-discovery-retry" onClick={() => void submit()} disabled={isLoading}>
              Retry
            </button>
          )}
        </div>
      </form>

      {view === "validation" && validationMessage && (
        <div className="ao-state ao-state-error" role="alert">
          <h3>Check your question</h3>
          <p>{validationMessage}</p>
        </div>
      )}

      {view === "loading" && (
        <div className="ao-state ao-state-loading" aria-live="polite" aria-busy="true">
          <h3>Searching</h3>
          <p>Querying the development discovery API…</p>
        </div>
      )}

      {view === "no_results" && (
        <div className="ao-state" role="status">
          <h3>No matching requirements</h3>
          <p>
            The API returned no results for this query and framework selection. Try another question
            or choose a different framework.
          </p>
          {response && (
            <p className="ao-discovery-meta">
              Outcome: <code>{response.outcome}</code>
            </p>
          )}
        </div>
      )}

      {view === "error" && errorMessage && (
        <div className="ao-state ao-state-error" role="alert">
          <h3>Discovery unavailable</h3>
          <p>{errorMessage}</p>
        </div>
      )}

      {view === "results" && response && (
        <div className="ao-discovery-results" aria-live="polite">
          <p className="ao-discovery-meta">
            Outcome: <code>{response.outcome}</code> · {response.frameworkGroups.length} framework
            {response.frameworkGroups.length === 1 ? "" : "s"} · {response.requirementIds.length}{" "}
            requirement{response.requirementIds.length === 1 ? "" : "s"}
          </p>
          {response.frameworkGroups.map((group) => (
            <article key={group.frameworkId} className="ao-discovery-group">
              <header>
                <span className="ao-label">{group.frameworkCode}</span>
                <h3>{group.displayName}</h3>
                <p className="ao-discovery-provenance">Framework provenance: {group.frameworkProvenance}</p>
              </header>
              <div className="ao-discovery-authoritative">
                <h4>Authoritative source instrument</h4>
                <p>
                  <strong>{group.sourceInstrument.title}</strong> ({group.sourceInstrument.id})
                </p>
                <p>{group.sourceInstrument.provenance}</p>
                <p className="ao-discovery-kind">Kind: {group.sourceInstrument.contentKind}</p>
              </div>
              {group.accredaOneExplanation && (
                <div className="ao-discovery-explanation">
                  <h4>AccredaOne explanation (not source text)</h4>
                  <p>
                    <strong>{group.accredaOneExplanation.title}</strong> ({group.accredaOneExplanation.id})
                  </p>
                  <p>{group.accredaOneExplanation.body}</p>
                  <p className="ao-discovery-provenance">{group.accredaOneExplanation.provenance}</p>
                </div>
              )}
              <ul className="ao-discovery-requirements">
                {group.requirements.map((requirement) => (
                  <li key={requirement.id}>
                    <p className="ao-discovery-req-id">{requirement.id}</p>
                    <p className="ao-discovery-req-label">{requirement.label}</p>
                    {requirement.hierarchyContext && (
                      <p className="ao-discovery-hierarchy">Hierarchy: {requirement.hierarchyContext}</p>
                    )}
                    <p className="ao-discovery-provenance">Requirement provenance: {requirement.provenance}</p>
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      )}

      {view === "initial" && (
        <div className="ao-state">
          <h3>Ready to search</h3>
          <p>Enter a question above to discover relevant synthetic requirements from the development dataset.</p>
        </div>
      )}
    </section>
  );
}
