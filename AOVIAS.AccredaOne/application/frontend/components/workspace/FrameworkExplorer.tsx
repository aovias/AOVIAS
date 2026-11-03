"use client";

import {
  FRAMEWORK_CATALOG,
  frameworkAvailabilityBadgeClass,
  frameworkAvailabilityLabel,
} from "../../lib/workspace/frameworkCatalog";
import { navigateToDiscoveryFramework } from "../../lib/shell/discoverySearch";

function FrameworkCard({
  shortName,
  fullName,
  description,
  availability,
  discoveryCode,
}: (typeof FRAMEWORK_CATALOG)[number]) {
  const canExplore = availability === "connected_synthetic" && discoveryCode;

  return (
    <article className="ao-card ao-framework-card">
      <div className="ao-framework-card-header">
        <span className={frameworkAvailabilityBadgeClass(availability)}>
          {frameworkAvailabilityLabel(availability)}
        </span>
        <h3>{shortName}</h3>
      </div>
      <p className="ao-framework-card-fullname">{fullName}</p>
      <p className="ao-framework-card-desc">{description}</p>
      {canExplore ? (
        <button
          type="button"
          className="ao-btn ao-btn-secondary ao-framework-explore"
          onClick={() => navigateToDiscoveryFramework(discoveryCode)}
        >
          Explore in discovery
        </button>
      ) : (
        <p className="ao-framework-card-status" role="status">
          Explore will be available when authoritative content is connected.
        </p>
      )}
    </article>
  );
}

export function FrameworkExplorer() {
  return (
    <section id="frameworks" className="ao-workspace-section" aria-labelledby="frameworks-heading">
      <header className="ao-workspace-section-header">
        <p className="ao-kicker">Framework knowledge</p>
        <h2 id="frameworks-heading">Explore framework knowledge</h2>
        <p className="ao-workspace-section-lead">
          AccredaOne is designed to support NABH, JCI, NABL and other frameworks over time. Only
          fictional NOAF and MCQF records are searchable in the current development synthetic dataset.
        </p>
      </header>
      <div className="ao-workspace-grid ao-workspace-grid-3">
        {FRAMEWORK_CATALOG.map((entry) => (
          <FrameworkCard key={entry.id} {...entry} />
        ))}
      </div>
    </section>
  );
}
