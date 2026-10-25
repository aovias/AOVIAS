'use client';

import { useMemo } from 'react';

import {
  getCheckoutClient,
  getCommerceClient,
  getContactClient,
  getIdentityClient,
  getKnowledgeClient,
  getSearchClient,
} from '@/registries/sdk-registry';

/**
 * Hooks resolve SDKs from the registry — pages never construct clients.
 */

export function useIdentitySdk() {
  return useMemo(() => getIdentityClient(), []);
}

export function useCommerceSdk() {
  return useMemo(() => getCommerceClient(), []);
}

export function useContactSdk() {
  return useMemo(() => getContactClient(), []);
}

export function useKnowledgeSdk() {
  return useMemo(() => getKnowledgeClient(), []);
}

export function useSearchSdk() {
  return useMemo(() => getSearchClient(), []);
}

export function useCheckoutSdk() {
  return useMemo(() => getCheckoutClient(), []);
}
