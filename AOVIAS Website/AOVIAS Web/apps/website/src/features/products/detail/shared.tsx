import type { MediaSlot } from '@/components/product';
import type { ProductNavItem } from '@/components/product';
import { productMediaSlot } from '@/components/brand';

/**
 * Product detail media — brand illustrations via MediaSlot custom nodes.
 */
export function productMedia(label: string): MediaSlot {
  return productMediaSlot(label);
}

/** @deprecated Prefer `productMedia` — retained for temporary call-site compatibility. */
export function productPlaceholder(label: string): MediaSlot {
  return productMedia(label);
}

/** Standard navigation for full product detail pages. */
export const standardProductNav: readonly ProductNavItem[] = [
  { id: 'overview', label: 'Overview', href: '#overview' },
  { id: 'capabilities', label: 'Capabilities', href: '#capabilities' },
  { id: 'architecture', label: 'Architecture', href: '#architecture' },
  { id: 'integrations', label: 'Integrations', href: '#integrations' },
  { id: 'specifications', label: 'Specifications', href: '#specifications' },
  { id: 'trust', label: 'Trust', href: '#trust' },
  { id: 'pricing', label: 'Pricing', href: '#pricing' },
  { id: 'resources', label: 'Resources', href: '#resources' },
  { id: 'cta', label: 'Next steps', href: '#cta' },
] as const;
