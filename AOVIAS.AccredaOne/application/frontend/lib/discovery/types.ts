export type DiscoveryQueryRequest = {
  query: string;
  frameworkCode?: string | null;
};

export type DiscoverySourceInstrument = {
  id: string;
  title: string;
  provenance: string;
  contentKind: string;
};

export type DiscoveryAccredaOneExplanation = {
  id: string;
  title: string;
  body: string;
  provenance: string;
  contentKind: string;
};

export type DiscoveryRequirement = {
  id: string;
  label: string;
  provenance: string;
  hierarchyContext?: string | null;
  sourceInstrumentId: string;
};

export type DiscoveryFrameworkGroup = {
  frameworkId: string;
  frameworkCode: string;
  displayName: string;
  frameworkProvenance: string;
  sourceInstrument: DiscoverySourceInstrument;
  accredaOneExplanation?: DiscoveryAccredaOneExplanation | null;
  requirements: DiscoveryRequirement[];
};

export type DiscoveryQueryResponse = {
  outcome: string;
  datasetPackId: string;
  datasetClassification: string;
  requirementIds: string[];
  frameworkGroups: DiscoveryFrameworkGroup[];
};

export class DiscoveryApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
  ) {
    super(message);
    this.name = "DiscoveryApiError";
  }
}
