export type SponsoredPlacement = {
  id: string;
  title: string;
  description: string;
  advertiser: string;
  href?: string;
  imageAlt?: string;
};

export type ValidatedSponsoredPlacement = {
  id: string;
  title: string;
  description: string;
  advertiser: string;
  href?: string;
};

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

/** Permits only absolute http(s) URLs; rejects empty, malformed, and unsafe schemes. */
export function isValidSponsoredHref(href: string | undefined): href is string {
  if (!isNonEmptyString(href)) {
    return false;
  }

  let url: URL;
  try {
    url = new URL(href.trim());
  } catch {
    return false;
  }

  return url.protocol === "http:" || url.protocol === "https:";
}

/**
 * Returns placements with valid required fields, de-duplicated by id (first wins).
 * Optional href is included only when it passes http(s) validation.
 */
export function getValidSponsoredPlacements(
  placements: SponsoredPlacement[],
): ValidatedSponsoredPlacement[] {
  const seenIds = new Set<string>();
  const valid: ValidatedSponsoredPlacement[] = [];

  for (const placement of placements) {
    if (
      !isNonEmptyString(placement.id) ||
      !isNonEmptyString(placement.title) ||
      !isNonEmptyString(placement.description) ||
      !isNonEmptyString(placement.advertiser)
    ) {
      continue;
    }

    const id = placement.id.trim();
    if (seenIds.has(id)) {
      continue;
    }
    seenIds.add(id);

    const validated: ValidatedSponsoredPlacement = {
      id,
      title: placement.title.trim(),
      description: placement.description.trim(),
      advertiser: placement.advertiser.trim(),
    };

    if (isValidSponsoredHref(placement.href)) {
      validated.href = placement.href.trim();
    }

    valid.push(validated);
  }

  return valid;
}

/** No live sponsorship feed — empty list hides the sponsored section entirely. */
export const SPONSORED_PLACEMENTS: SponsoredPlacement[] = [];
