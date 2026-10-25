'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import { getTelemetryClient } from '../adapters/registry';
import type { TelemetryClient } from '../core/interfaces';
import {
  createConsentState,
  DEFAULT_CONSENT_STATE,
  type ConsentState,
} from '../consent/types';

export type TelemetryProviderProps = {
  children: ReactNode;
  /** Injectable consent — defaults to analytics/marketing off. */
  consent?: ConsentState;
  /** Override client (tests). */
  client?: TelemetryClient;
};

type TelemetryReactContextValue = {
  client: TelemetryClient;
  consent: ConsentState;
  setConsent: (consent: ConsentState) => void;
};

const TelemetryReactContext = createContext<TelemetryReactContextValue | null>(
  null,
);

/**
 * Injects TelemetryClient + consent into the React tree.
 * Does not load third-party scripts.
 */
export function TelemetryProvider({
  children,
  consent: initialConsent,
  client: clientOverride,
}: TelemetryProviderProps) {
  const client = useMemo(
    () => clientOverride ?? getTelemetryClient(),
    [clientOverride],
  );
  const [consent, setConsentState] = useState<ConsentState>(() =>
    createConsentState(initialConsent ?? DEFAULT_CONSENT_STATE),
  );

  useEffect(() => {
    client.setConsent(consent);
  }, [client, consent]);

  const setConsent = useCallback(
    (next: ConsentState) => {
      const resolved = createConsentState(next);
      client.setConsent(resolved);
      setConsentState(resolved);
    },
    [client],
  );

  const value = useMemo<TelemetryReactContextValue>(
    () => ({ client, consent, setConsent }),
    [client, consent, setConsent],
  );

  return (
    <TelemetryReactContext.Provider value={value}>
      {children}
    </TelemetryReactContext.Provider>
  );
}

export function useTelemetryContext(): TelemetryReactContextValue {
  const value = useContext(TelemetryReactContext);
  if (!value) {
    // Safe fallback for components outside the provider (SSR edges).
    const client = getTelemetryClient();
    return {
      client,
      consent: client.getConsent(),
      setConsent: (next) => client.setConsent(next),
    };
  }
  return value;
}
