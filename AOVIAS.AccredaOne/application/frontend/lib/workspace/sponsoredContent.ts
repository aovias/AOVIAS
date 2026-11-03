export type SponsoredPlacement = {
  id: string;
  title: string;
  description: string;
  advertiser: string;
  href: string;
  imageAlt?: string;
};

/** No live sponsorship feed — empty list hides the sponsored section entirely. */
export const SPONSORED_PLACEMENTS: SponsoredPlacement[] = [];
