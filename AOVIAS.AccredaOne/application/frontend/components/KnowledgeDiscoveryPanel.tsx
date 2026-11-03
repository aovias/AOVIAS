"use client";

import { FormEvent, useCallback, useState } from "react";
import { postDiscoveryQuery } from "../lib/discovery/client";
import { ELIGIBLE_FRAMEWORK_OPTIONS } from "../lib/discovery/frameworkOptions";
import { DISCOVERY_SAMPLE_QUERIES } from "../lib/discovery/sampleQueries";
import type { DiscoveryRequirementSelection } from "../lib/discovery/selection";
import { DiscoveryApiError, type DiscoveryQueryResponse } from "../lib/discovery/types";
import { RequirementDetailPanel } from "./RequirementDetailPanel";

type ViewState = "initial" | "loading" | "results" | "no_results" | "error" | "validation";

export function KnowledgeDiscoveryPanel() {
  const [query, setQuery] = useState("");
  const [frameworkCode, setFrameworkCode] = useState<string>("");
  const [view, setView] = useState<ViewState>("initial");
  const [validationMessage, setValidationMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [response, setResponse] = useState<DiscoveryQueryResponse | null>(null);
  const [selectedRequirement, setSelectedRequirement] = useState<DiscoveryRequirementSelection | null>(
    null,
  );

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
    setSelectedRequirement(null);

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

  const applySampleQuery = (sampleQuery: string) => {
    setQuery(sampleQuery);
    if (view === "validation") {
      setValidationMessage(null);
      setView("initial");
    }
  };

  const isLoading = view === "loading";
  const showGettingStarted = view === "initial" || view === "validation";

  return (
    <section id="library" className="ao-panel ao-discovery">
      <p className="ao-kicker">Knowledge discovery</p>
      <h2>Discover requirements</h2>

      <div className="ao-discovery-getting-started" aria-labelledby="discovery-purpose-heading">
        <h3 id="discovery-purpose-heading">What this does</h3>
        <p className="ao-discovery-intro">
          Discovery helps you explore <strong>fictional</strong> NOAF and MCQF requirement records from
          AccredaOne&apos;s development synthetic pack. Ask a question in plain language, optionally narrow
          to one framework, then review grouped results and open a requirement to see source-oriented text,
          applicability notes, and a separate AccredaOne explanation.
        </p>
        <p className="ao-discovery-intro">
          This is an engineering preview only — not official accreditation content, scoring, or compliance
          decisions.
        </p>
      </div>

      <p className="ao-discovery-dataset" role="note">
        <strong>Synthetic development data only:</strong> responses come from pack{" "}
        <code>mvp-development-synthetic-discovery</code> (classification: synthetic). Nothing here is
        published NABH, JCI, NABL, or other real standard text.
      </p>

      <form className="ao-discovery-form" onSubmit={onSubmit} noValidate>
        <label className="ao-discovery-field">
          <span>Your question</span>
          <textarea
            name="query"
            rows={3}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Ask about fictional NOAF or MCQF synthetic requirements — e.g. operational assurance, patient experience, or cross-framework overview."
            aria-describedby="discovery-query-hint discovery-samples-heading"
            aria-invalid={view === "validation"}
            disabled={isLoading}
          />
          <span id="discovery-query-hint" className="ao-discovery-field-hint">
            Search the development dataset in natural language. Edit any text before you press Search.
          </span>
        </label>

        <div className="ao-discovery-samples" aria-labelledby="discovery-samples-heading">
          <h4 id="discovery-samples-heading">Try a sample question</h4>
          <p className="ao-discovery-field-hint">
            Samples are grounded in the fictional NOAF/MCQF records. Click to fill the box above — they do
            not search until you submit.
          </p>
          <ul className="ao-discovery-sample-list">
            {DISCOVERY_SAMPLE_QUERIES.map((sample) => (
              <li key={sample.id}>
                <button
                  type="button"
                  className="ao-discovery-sample"
                  disabled={isLoading}
                  aria-label={`Use sample question: ${sample.label}. ${sample.hint}`}
                  onClick={() => applySampleQuery(sample.query)}
                >
                  <span className="ao-discovery-sample-label">{sample.label}</span>
                  <span className="ao-discovery-sample-hint">{sample.hint}</span>
                </button>
              </li>
            ))}
          </ul>
        </div>

        <label className="ao-discovery-field">
          <span>Framework (optional)</span>
          <select
            name="frameworkCode"
            value={frameworkCode}
            onChange={(event) => setFrameworkCode(event.target.value)}
            disabled={isLoading}
            aria-describedby="discovery-framework-hint"
          >
            <option value="">All eligible frameworks (cross-framework discovery)</option>
            {ELIGIBLE_FRAMEWORK_OPTIONS.map((option) => (
              <option key={option.code} value={option.code}>
                {option.label}
              </option>
            ))}
          </select>
          <span id="discovery-framework-hint" className="ao-discovery-field-hint">
            Leave this empty to search <strong>all eligible</strong> synthetic frameworks (NOAF and MCQF).
            Choose one framework to limit results to that fictional framework only.
          </span>
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

      {view === "results" && response && selectedRequirement && (
        <RequirementDetailPanel
          selection={selectedRequirement}
          datasetPackId={response.datasetPackId}
          datasetClassification={response.datasetClassification}
          onBack={() => setSelectedRequirement(null)}
        />
      )}

      {view === "results" && response && !selectedRequirement && (
        <div className="ao-discovery-results" aria-live="polite">
          <h3 className="ao-discovery-results-heading">Discovery results</h3>
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
              <ul className="ao-discovery-requirements">
                {group.requirements.map((requirement) => (
                  <li key={requirement.id}>
                    <p className="ao-discovery-req-id">{requirement.id}</p>
                    <p className="ao-discovery-req-label">{requirement.label}</p>
                    {requirement.hierarchyContext && (
                      <p className="ao-discovery-hierarchy">Hierarchy: {requirement.hierarchyContext}</p>
                    )}
                    <button
                      type="button"
                      className="ao-requirement-detail-link"
                      aria-label={`View details for ${requirement.label}`}
                      onClick={() =>
                        setSelectedRequirement({
                          group,
                          requirement,
                        })
                      }
                    >
                      View requirement details
                    </button>
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      )}

      {showGettingStarted && (
        <div className="ao-state ao-discovery-first-visit">
          <h3>Ready when you are</h3>
          <p>
            Pick a sample question or type your own, then press <strong>Search</strong> to see grouped
            synthetic requirements below.
          </p>
        </div>
      )}
    </section>
  );
}
