/**
 * OpenTelemetry-aligned attribute bags.
 * Keys use dotted namespaces (e.g. `page.path`, `product.slug`).
 */

export type AttributeValue = string | number | boolean | null | undefined;

export type Attributes = Readonly<Record<string, AttributeValue>>;

export function mergeAttributes(
  ...parts: ReadonlyArray<Attributes | undefined>
): Attributes {
  const out: Record<string, AttributeValue> = {};
  for (const part of parts) {
    if (!part) continue;
    for (const [key, value] of Object.entries(part)) {
      if (value !== undefined) out[key] = value;
    }
  }
  return out;
}
