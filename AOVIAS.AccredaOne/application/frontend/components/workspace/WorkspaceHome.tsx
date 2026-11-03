import { KnowledgeDiscoveryPanel } from "../KnowledgeDiscoveryPanel";
import { PresentationStates } from "../PresentationStates";
import { FeaturedResources } from "./FeaturedResources";
import { FrameworkExplorer } from "./FrameworkExplorer";
import { KnowledgeIntegrityNotice } from "./KnowledgeIntegrityNotice";
import { KnowledgeShortcuts } from "./KnowledgeShortcuts";
import { SponsoredContent } from "./SponsoredContent";
import { WelcomeHero } from "./WelcomeHero";
import { SPONSORED_PLACEMENTS } from "../../lib/workspace/sponsoredContent";

export function WorkspaceHome() {
  return (
    <div className="ao-workspace-home">
      <WelcomeHero />
      <KnowledgeDiscoveryPanel presentation="workspace" />
      <FrameworkExplorer />
      <KnowledgeShortcuts />
      <FeaturedResources />
      <SponsoredContent placements={SPONSORED_PLACEMENTS} />
      <KnowledgeIntegrityNotice />
      <PresentationStates />
    </div>
  );
}
