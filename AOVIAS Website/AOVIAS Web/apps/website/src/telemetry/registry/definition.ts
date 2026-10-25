import type { Attributes } from '../core/attributes';
import type { TelemetryEventKind } from '../core/types';
import type { EventNamespace } from '../events/taxonomy';
import type { EventName } from './event-names';

export type EventOwner =
  | 'website'
  | 'growth'
  | 'product'
  | 'engineering'
  | 'people'
  | 'platform';

export type ConsentRequirement = 'none' | 'analytics' | 'marketing';

export type EventDefinition<TName extends EventName = EventName> = {
  name: TName;
  category: EventNamespace;
  kind: TelemetryEventKind;
  description: string;
  version: number;
  owner: EventOwner;
  /** Required attribute keys (validated at emit time). */
  requiredAttributes: readonly string[];
  /** Optional attribute keys documented for producers. */
  optionalAttributes?: readonly string[];
  consent: ConsentRequirement;
  /** True when this is an enterprise conversion signal. */
  isConversion?: boolean;
};

export type EventPayloadMap = {
  [K in EventName]: Attributes;
};
