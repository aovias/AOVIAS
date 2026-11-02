"use client";

import { useState } from "react";

type View = "ready" | "loading" | "empty" | "error";

const copy: Record<View, { title: string; body: string }> = {
  ready: {
    title: "Workspace ready",
    body: "This is a synthetic placeholder. No accreditation requirement, citation, or hospital record is loaded.",
  },
  loading: {
    title: "Loading",
    body: "A future knowledge request would show progress here. Nothing is being retrieved in this foundation.",
  },
  empty: {
    title: "Nothing to show",
    body: "The library has no connected sources. This empty state is intentional sample content.",
  },
  error: {
    title: "Something went wrong",
    body: "This sample error is not a live service failure. No accreditation decision was made.",
  },
};

export function PresentationStates() {
  const [view, setView] = useState<View>("empty");
  const current = copy[view];

  return (
    <section id="states" className="ao-panel">
      <h2>Presentation states</h2>
      <p>Reusable loading, empty, and error treatments for later product work. Content below is synthetic.</p>
      <div className="ao-toolbar" role="group" aria-label="Preview presentation state">
        {(["ready", "loading", "empty", "error"] as View[]).map((item) => (
          <button
            key={item}
            type="button"
            aria-pressed={view === item}
            onClick={() => setView(item)}
          >
            {item}
          </button>
        ))}
      </div>
      <div className={view === "error" ? "ao-state ao-state-error" : view === "loading" ? "ao-state ao-state-loading" : "ao-state"}>
        <h3>{current.title}</h3>
        <p>{current.body}</p>
      </div>
    </section>
  );
}
