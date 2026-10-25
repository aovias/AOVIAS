import type { JsonLd } from '@/lib/seo/schema';

export type JsonLdScriptProps = {
  data: JsonLd | readonly JsonLd[];
  id?: string;
};

/**
 * Server-safe JSON-LD script tag(s).
 * Prefer emitting Organization + WebSite once at the root; page schemas per route later.
 */
export function JsonLdScript({ data, id }: JsonLdScriptProps) {
  const payloads = Array.isArray(data) ? data : [data];

  return (
    <>
      {payloads.map((payload, index) => (
        <script
          key={id ? `${id}-${index}` : `json-ld-${index}`}
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify(payload),
          }}
        />
      ))}
    </>
  );
}
