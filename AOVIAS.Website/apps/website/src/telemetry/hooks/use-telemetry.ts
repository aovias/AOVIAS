'use client';

import { useTelemetryContext } from '../context/telemetry-provider';
import type { TelemetryClient } from '../core/interfaces';

/** Access the application TelemetryClient (never a concrete provider). */
export function useTelemetry(): TelemetryClient {
  return useTelemetryContext().client;
}
