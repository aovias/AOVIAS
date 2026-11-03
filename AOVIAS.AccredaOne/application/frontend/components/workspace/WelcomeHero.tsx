export function WelcomeHero() {
  return (
    <section id="overview" className="ao-workspace-hero" aria-labelledby="workspace-hero-heading">
      <p className="ao-workspace-eyebrow">Your accreditation knowledge workspace</p>
      <h1 id="workspace-hero-heading">Understand accreditation. Navigate with confidence.</h1>
      <p className="ao-workspace-hero-lead">
        Discover accreditation requirements, explore authoritative framework knowledge, and understand
        what standards mean in practice.
      </p>
      <div className="ao-workspace-hero-actions">
        <a className="ao-btn ao-btn-primary" href="#library">
          Discover knowledge
        </a>
        <a className="ao-btn ao-btn-secondary" href="#frameworks">
          Explore framework knowledge
        </a>
      </div>
    </section>
  );
}
