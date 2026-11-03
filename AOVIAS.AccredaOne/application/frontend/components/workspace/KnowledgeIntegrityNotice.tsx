export function KnowledgeIntegrityNotice() {
  return (
    <section className="ao-workspace-section ao-integrity-notice" aria-labelledby="integrity-heading">
      <h2 id="integrity-heading">Knowledge integrity</h2>
      <p>
        AccredaOne is designed to help professionals discover and understand accreditation knowledge.
        Authoritative source content and explanatory guidance are presented distinctly. AccredaOne
        does not replace accreditation bodies, official publications or their decisions.
      </p>
      <p className="ao-integrity-synthetic" role="note">
        <strong>Synthetic development data:</strong> discovery responses may come from pack{" "}
        <code>mvp-development-synthetic-discovery</code> (classification: synthetic). That content is
        fictional engineering data — not published NABH, JCI, NABL, or other real standard text.
      </p>
    </section>
  );
}
