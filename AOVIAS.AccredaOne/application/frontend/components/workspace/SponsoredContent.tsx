import type { SponsoredPlacement } from "../../lib/workspace/sponsoredContent";

type SponsoredContentProps = {
  placements: SponsoredPlacement[];
};

/**
 * Renders sponsored educational placements when explicitly configured.
 * Returns nothing when the list is empty — no fictional advertisements.
 */
export function SponsoredContent({ placements }: SponsoredContentProps) {
  if (placements.length === 0) {
    return null;
  }

  return (
    <section className="ao-workspace-section ao-sponsored-section" aria-labelledby="sponsored-heading">
      <header className="ao-workspace-section-header">
        <p className="ao-kicker ao-sponsored-kicker">Sponsored</p>
        <h2 id="sponsored-heading">Sponsored educational resources</h2>
        <p className="ao-workspace-section-lead">
          Separated from authoritative accreditation knowledge. Not an endorsement by accreditation
          bodies.
        </p>
      </header>
      <div className="ao-workspace-grid ao-workspace-grid-2">
        {placements.map((item) => (
          <article key={item.id} className="ao-card ao-sponsored-card">
            <span className="ao-badge">Sponsored</span>
            <h3>{item.title}</h3>
            <p>{item.description}</p>
            <p className="ao-sponsored-advertiser">Presented by {item.advertiser}</p>
            <a className="ao-link" href={item.href}>
              Learn more
            </a>
          </article>
        ))}
      </div>
    </section>
  );
}
