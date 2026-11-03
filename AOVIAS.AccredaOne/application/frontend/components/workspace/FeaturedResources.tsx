import { FEATURED_RESOURCES } from "../../lib/workspace/featuredResources";

export function FeaturedResources() {
  return (
    <section className="ao-workspace-section" aria-labelledby="featured-heading">
      <header className="ao-workspace-section-header">
        <p className="ao-kicker">Learning resources</p>
        <h2 id="featured-heading">Featured knowledge &amp; learning resources</h2>
        <p className="ao-workspace-section-lead">
          Sample editorial cards for layout and future CMS connection. They are not authoritative
          accreditation material.
        </p>
      </header>
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
