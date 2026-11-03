import { DiscoveryApiError, type DiscoveryQueryRequest, type DiscoveryQueryResponse } from "./types";

const DEFAULT_API_BASE = "http://localhost:5022";

export function getDiscoveryApiBaseUrl(): string {
  const configured = process.env.NEXT_PUBLIC_ACCREDAONE_API_BASE_URL?.trim();
  if (configured && configured.length > 0) {
    return configured.replace(/\/$/, "");
  }

  if (typeof window !== "undefined") {
    return "";
  }

  return DEFAULT_API_BASE;
}

export function buildDiscoveryQueryBody(query: string, frameworkCode: string | null): DiscoveryQueryRequest {
  const trimmed = query.trim();
  const body: DiscoveryQueryRequest = { query: trimmed };
  if (frameworkCode) {
    body.frameworkCode = frameworkCode;
  }
  return body;
}

export async function postDiscoveryQuery(
  query: string,
  frameworkCode: string | null,
): Promise<DiscoveryQueryResponse> {
  const baseUrl = getDiscoveryApiBaseUrl();
  const body = buildDiscoveryQueryBody(query, frameworkCode);

  let response: Response;
  try {
    response = await fetch(`${baseUrl}/api/v1/discovery/query`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(body),
    });
  } catch {
    throw new DiscoveryApiError(
      "Could not reach the AccredaOne API. Start the backend or check NEXT_PUBLIC_ACCREDAONE_API_BASE_URL.",
    );
  }

  if (!response.ok) {
    const detail = await response.text();
    throw new DiscoveryApiError(
      detail ? `Discovery request failed (${response.status}).` : `Discovery request failed (${response.status}).`,
      response.status,
    );
  }

  return (await response.json()) as DiscoveryQueryResponse;
}
