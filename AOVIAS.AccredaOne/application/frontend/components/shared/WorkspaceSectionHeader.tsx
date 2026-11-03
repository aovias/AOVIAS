import type { ReactNode } from "react";

type WorkspaceSectionHeaderProps = {
  kicker?: string;
  heading: ReactNode;
  headingId?: string;
  description?: ReactNode;
  headingLevel?: 2 | 3;
};

export function WorkspaceSectionHeader({
  kicker,
  heading,
  headingId,
  description,
  headingLevel = 2,
}: WorkspaceSectionHeaderProps) {
  const HeadingTag = headingLevel === 3 ? "h3" : "h2";

  return (
    <header className="ao-workspace-section-header">
      {kicker ? <p className="ao-kicker">{kicker}</p> : null}
      <HeadingTag id={headingId}>{heading}</HeadingTag>
      {description ? <p className="ao-workspace-section-lead">{description}</p> : null}
    </header>
  );
}
