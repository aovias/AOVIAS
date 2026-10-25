import type {
  CommerceClient,
  ContactClient,
  IdentityClient,
  KnowledgeClient,
  SearchClient,
} from '@aovias/sdk';
import { SdkClientRegistry } from '@aovias/sdk/core';

import { createMockCommerceClient } from '@/adapters/commerce/mock-commerce-client';
import { createHttpContactClient } from '@/adapters/contact/http-contact-client';
import { createMockKnowledgeClient } from '@/adapters/knowledge/mock-knowledge-client';
import { createMockSearchClient } from '@/adapters/search/mock-search-client';
import { integrationConfig } from '@/config/integration';
import { createMockIdentityClient } from '@/services/identity/mock-client';
import type { CheckoutClient } from '@/services/checkout/client';
import { createMockCheckoutClient } from '@/services/checkout/mock-client';

/**
 * Composition root for AOVIAS website SDKs.
 * Pages and form adapters resolve clients here — never instantiate HTTP clients.
 */

const registry = new SdkClientRegistry();
let bootstrapped = false;

/** Website-only checkout bridge (UI cart models) until Commerce maps fully. */
const CHECKOUT_KEY = 'checkout';

function bootstrap(): void {
  if (bootstrapped) return;

  registry.register<IdentityClient>('identity', {
    id: 'identity-mock',
    mode: integrationConfig.services.identity.mode === 'http' ? 'http' : 'mock',
    label: 'Identity Platform (mock)',
    create: createMockIdentityClient,
  });

  registry.register<CommerceClient>('commerce', {
    id: 'commerce-mock',
    mode: integrationConfig.services.commerce.mode === 'http' ? 'http' : 'mock',
    label: 'Commerce (mock)',
    create: createMockCommerceClient,
  });

  registry.register<ContactClient>('contact', {
    id: 'contact-http',
    mode: 'http',
    label: 'Contact (HTTP → /api/contact)',
    create: createHttpContactClient,
  });

  registry.register<KnowledgeClient>('knowledge', {
    id: 'knowledge-mock',
    mode: integrationConfig.services.knowledge.mode === 'http' ? 'http' : 'mock',
    label: 'Knowledge (mock)',
    create: createMockKnowledgeClient,
  });

  registry.register<SearchClient>('search', {
    id: 'search-mock',
    mode: integrationConfig.services.search.mode === 'http' ? 'http' : 'mock',
    label: 'Search (mock)',
    create: createMockSearchClient,
  });

  registry.register<CheckoutClient>(CHECKOUT_KEY, {
    id: 'checkout-mock',
    mode: 'mock',
    label: 'Checkout bridge (mock)',
    create: createMockCheckoutClient,
  });

  bootstrapped = true;
}

export function getSdkRegistry(): SdkClientRegistry {
  bootstrap();
  return registry;
}

export function getIdentityClient(): IdentityClient {
  return getSdkRegistry().resolve<IdentityClient>('identity');
}

export function getCommerceClient(): CommerceClient {
  return getSdkRegistry().resolve<CommerceClient>('commerce');
}

export function getContactClient(): ContactClient {
  return getSdkRegistry().resolve<ContactClient>('contact');
}

export function getKnowledgeClient(): KnowledgeClient {
  return getSdkRegistry().resolve<KnowledgeClient>('knowledge');
}

export function getSearchClient(): SearchClient {
  return getSdkRegistry().resolve<SearchClient>('search');
}

export function getCheckoutClient(): CheckoutClient {
  return getSdkRegistry().resolve<CheckoutClient>(CHECKOUT_KEY);
}

/** Diagnostics for readiness reports / future admin tooling. */
export function listRegisteredSdkModes(): Record<string, string | undefined> {
  const reg = getSdkRegistry();
  return {
    identity: reg.mode('identity'),
    commerce: reg.mode('commerce'),
    contact: reg.mode('contact'),
    knowledge: reg.mode('knowledge'),
    search: reg.mode('search'),
    checkout: reg.mode(CHECKOUT_KEY),
  };
}
