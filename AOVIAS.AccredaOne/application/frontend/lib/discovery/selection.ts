import type { DiscoveryFrameworkGroup, DiscoveryRequirement } from "./types";

export type DiscoveryRequirementSelection = {
  group: DiscoveryFrameworkGroup;
  requirement: DiscoveryRequirement;
};

export function requirementSelectionKey(selection: DiscoveryRequirementSelection): string {
  return `${selection.group.frameworkId}:${selection.requirement.id}`;
}
