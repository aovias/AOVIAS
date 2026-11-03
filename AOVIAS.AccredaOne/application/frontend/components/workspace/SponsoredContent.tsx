import { WorkspaceSectionHeader } from "../shared/WorkspaceSectionHeader";
import {
  getValidSponsoredPlacements,
  type SponsoredPlacement,
} from "../../lib/workspace/sponsoredContent";

type SponsoredContentProps = {
  placements: SponsoredPlacement[];
};

/**
 * Renders sponsored educational placements when explicitly configured and valid.
 * Returns nothing when no valid placements remain — no fictional advertisements.
 */
export function SponsoredContent({ placements }: SponsoredContentProps) {
  const validPlacements = getValidSponsoredPlacements(placements);

  if (validPlacements.length === 0) {
    return null;
  }

  return (
    <section className="ao-workspace-section ao-sponsored-section" aria-labelledby="sponsored-heading">
      <WorkspaceSectionHeader
        kicker="Sponsored"
        headingId="sponsored-heading"
        heading="Sponsored educational resources"
        description="Separated from authoritative accreditation knowledge. Not an endorsement by accreditation bodies."
      />
      <div className="ao-workspace-grid ao-workspace-grid-2">
        {validPlacements.map((item) => (
          <article
            key={item.id}
            className="ao-card ao-sponsored-card"
            aria-label={`Sponsored: ${item.title}`}
          >
            <span className="ao-badge ao-sponsored-label">Sponsored</span>
            <h3>{item.title}</h3>
            <p>{item.description}</p>
            <p className="ao-sponsored-advertiser">Presented by {item.advertiser}</p>
            {item.href ? (
              <a
                className="ao-link"
                href={item.href}
                rel="noopener noreferrer"
                aria-label={`Learn more about ${item.title} from ${item.advertiser}`}
              >
                Learn more
              </a>
            ) : null}
          </article>
        ))}
      </div>
    </section>
  );
}
