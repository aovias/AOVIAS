"use client";

import type { DiscoveryRequirementSelection } from "../lib/discovery/selection";

type RequirementDetailPanelProps = {
  selection: DiscoveryRequirementSelection;
  datasetPackId: string;
  datasetClassification: string;
  onBack: () => void;
};

export function RequirementDetailPanel({
  selection,
  datasetPackId,
  datasetClassification,
  onBack,
}: RequirementDetailPanelProps) {
  const { group, requirement } = selection;
  const source = group.sourceInstrument;
  const explanation = group.accredaOneExplanation;
  const applicability = requirement.applicability;

  return (
    <div className="ao-requirement-detail" aria-labelledby="requirement-detail-title">
      <div className="ao-discovery-actions ao-requirement-detail-nav">
        <button type="button" className="ao-discovery-retry" onClick={onBack}>
          Back to results
        </button>
      </div>

      <p className="ao-discovery-dataset" role="note">
        <strong>Synthetic development data:</strong> pack <code>{datasetPackId}</code> (
        {datasetClassification}). This is not official accreditation content.
      </p>

      <header className="ao-requirement-detail-header">
        <span className="ao-label">{group.frameworkCode}</span>
        <h3 id="requirement-detail-title">{requirement.label}</h3>
        <p className="ao-discovery-meta">
          Requirement <code>{requirement.id}</code> · Framework {group.displayName} (
          <code>{group.frameworkId}</code>)
        </p>
        <p className="ao-discovery-provenance">Framework provenance: {group.frameworkProvenance}</p>
      </header>

      <section className="ao-requirement-detail-section" aria-labelledby="hierarchy-heading">
        <h4 id="hierarchy-heading">Hierarchy</h4>
        {requirement.hierarchyContext ? (
          <p>{requirement.hierarchyContext}</p>
        ) : (
          <p className="ao-discovery-meta">No hierarchy context was returned for this requirement.</p>
        )}
      </section>

      <section className="ao-requirement-detail-section ao-discovery-authoritative" aria-labelledby="authoritative-heading">
        <h4 id="authoritative-heading">Requirement text (synthetic source placeholder)</h4>
        {requirement.requirementText ? (
          <p>{requirement.requirementText}</p>
        ) : (
          <p className="ao-discovery-meta">No requirement text was returned for this requirement.</p>
        )}
        <p className="ao-discovery-meta">
          Source-oriented placeholder from the API — not live publisher accreditation text and not the
          AccredaOne explanation below.
        </p>
      </section>

      <section className="ao-requirement-detail-section ao-discovery-authoritative" aria-labelledby="provenance-heading">
        <h4 id="provenance-heading">Requirement provenance (traceability)</h4>
        <p>{requirement.provenance}</p>
      </section>

      <section className="ao-requirement-detail-section ao-discovery-authoritative" aria-labelledby="source-heading">
        <h4 id="source-heading">Source instrument reference</h4>
        <p>
          <strong>{source.title}</strong> (<code>{source.id}</code>)
        </p>
        <p>Linked from requirement field <code>{requirement.sourceInstrumentId}</code></p>
        <p>{source.provenance}</p>
        <p className="ao-discovery-kind">Kind: {source.contentKind}</p>
      </section>

      <section className="ao-requirement-detail-section" aria-labelledby="applicability-heading">
        <h4 id="applicability-heading">Applicability (synthetic metadata)</h4>
        {applicability ? (
          <>
            <p>{applicability.summary}</p>
            {applicability.contextTags.length > 0 && (
              <p className="ao-discovery-meta">
                Context tags: {applicability.contextTags.map((tag) => tag).join(", ")}
              </p>
            )}
            <p className="ao-discovery-provenance">{applicability.disclaimer}</p>
          </>
        ) : (
          <p className="ao-discovery-meta">
            No applicability metadata was returned for this requirement.
          </p>
        )}
      </section>

      {explanation ? (
        <section
          className="ao-requirement-detail-section ao-discovery-explanation"
          aria-labelledby="explanation-heading"
        >
          <h4 id="explanation-heading">AccredaOne explanation (not source text)</h4>
          <p>
            <strong>{explanation.title}</strong> (<code>{explanation.id}</code>)
          </p>
          <p>{explanation.body}</p>
          <p className="ao-discovery-provenance">{explanation.provenance}</p>
          <p className="ao-discovery-kind">Kind: {explanation.contentKind}</p>
        </section>
      ) : (
        <section className="ao-requirement-detail-section" aria-labelledby="explanation-heading">
          <h4 id="explanation-heading">AccredaOne explanation</h4>
          <p className="ao-discovery-meta">No AccredaOne explanation was returned for this framework group.</p>
        </section>
      )}
    </div>
  );
}
