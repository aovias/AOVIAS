import { WorkspaceSectionHeader } from "../shared/WorkspaceSectionHeader";
import { FEATURED_RESOURCES } from "../../lib/workspace/featuredResources";

export function FeaturedResources() {
  return (
    <section className="ao-workspace-section" aria-labelledby="featured-heading">
      <WorkspaceSectionHeader
        kicker="Learning resources"
        headingId="featured-heading"
        heading="Featured knowledge & learning resources"
        description="Sample editorial cards for layout and future CMS connection. They are not authoritative accreditation material."
      />
      <div className="ao-workspace-grid ao-workspace-grid-3">
        {FEATURED_RESOURCES.map((resource) => (
          <article key={resource.id} className="ao-card ao-resource-card">
            <span className="ao-badge ao-badge-warning">Sample content</span>
            <p className="ao-resource-category">{resource.category}</p>
            <h3>{resource.title}</h3>
            <p>{resource.summary}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
