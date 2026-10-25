'use client';

import { useCallback } from 'react';

import type { Attributes } from '../core/attributes';
import type { EventName } from '../registry/event-names';
import { useTelemetry } from './use-telemetry';

/** Stable track() callback for interactive components. */
export function useTrackEvent(): (
  name: EventName,
  attributes?: Attributes,
) => void {
  const client = useTelemetry();
  return useCallback(
    (name, attributes) => {
      client.track(name, attributes);
    },
    [client],
  );
}
