import { Compass, LayoutDashboard } from "lucide-react";

const SHORTCUTS = [
  {
    href: "#library",
    title: "Knowledge discovery",
    description: "Ask questions in natural language and review grouped requirement records.",
    icon: Compass,
  },
  {
    href: "#overview",
    title: "Workspace overview",
    description: "Return to the workspace introduction and framework orientation.",
    icon: LayoutDashboard,
  },
] as const;

export function KnowledgeShortcuts() {
  return (
    <section className="ao-workspace-section" aria-labelledby="shortcuts-heading">
      <header className="ao-workspace-section-header">
        <p className="ao-kicker">Continue your journey</p>
        <h2 id="shortcuts-heading">Continue your knowledge journey</h2>
        <p className="ao-workspace-section-lead">
          Shortcuts to implemented workspace destinations. Saved knowledge and a separate library
          catalogue are not available in this increment.
        </p>
      </header>
      <div className="ao-workspace-grid ao-workspace-grid-2">
        {SHORTCUTS.map((item) => {
          const Icon = item.icon;
          return (
            <a key={item.href} href={item.href} className="ao-card ao-shortcut-card">
              <Icon size={22} strokeWidth={2} aria-hidden className="ao-shortcut-icon" />
              <h3>{item.title}</h3>
              <p>{item.description}</p>
            </a>
          );
        })}
      </div>
    </section>
  );
}
