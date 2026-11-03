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
        <h4 id="authoritative-heading">Authoritative requirement content (synthetic placeholder)</h4>
        <p>{requirement.provenance}</p>
        <p className="ao-discovery-meta">
          This field uses the requirement provenance string from the API. It is not live publisher
          source text.
        </p>
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
        <h4 id="applicability-heading">Eligibility / applicability</h4>
        <p className="ao-discovery-meta">
          Not returned in the discovery query response. Framework eligibility is enforced server-side
          when building result groups.
        </p>
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
