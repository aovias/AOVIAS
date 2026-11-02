import { AppShell } from "../components/AppShell";
import { PresentationStates } from "../components/PresentationStates";

export default function Home() {
  return (
    <AppShell>
      <section id="overview">
        <p className="ao-kicker">Hospital quality and accreditation lead</p>
        <h1>A workspace for finding and explaining accreditation knowledge.</h1>
        <p className="ao-lead">
          AccredaOne is being prepared as a professional knowledge workspace. This screen is the
          application shell only. It does not score readiness, accept evidence, or decide
          accreditation outcomes.
        </p>
      </section>
      <section className="ao-grid" aria-label="Foundation boundaries">
        <article className="ao-card">
          <span className="ao-label">Synthetic</span>
          <h2>Placeholder collection</h2>
          <p>Sample topic title only. This is not an accreditation requirement or a citation.</p>
        </article>
        <article className="ao-card">
          <span className="ao-label">Not connected</span>
          <h2>No source library</h2>
          <p>Standards text, hospital documents, and explanations are not loaded.</p>
        </article>
        <article className="ao-card">
          <span className="ao-label">Out of scope</span>
          <h2>No decisions</h2>
          <p>Sign-in, uploads, scoring, and framework equivalence are not part of this increment.</p>
        </article>
      </section>
      <section id="library" className="ao-panel">
        <h2>Knowledge library</h2>
        <p>
          The library area is reserved for later discovery and explanation work. It currently shows
          an empty collection so the layout can be reviewed without inventing accreditation content.
        </p>
      </section>
      <PresentationStates />
      <p className="ao-footnote">
        DEV-03 engineering foundation. Phase 1 is not authorized. G1 has not passed.
      </p>
    </AppShell>
  );
}
