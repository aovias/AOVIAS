/**
 * Stable error taxonomy for all AOVIAS frontend SDKs.
 * Future HTTP adapters map transport failures into these codes.
 */

export type SdkErrorCode =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'not_found'
  | 'conflict'
  | 'rate_limited'
  | 'timeout'
  | 'network'
  | 'unavailable'
  | 'unknown';

export type SdkError = {
  code: SdkErrorCode;
  message: string;
  /** Safe for UI; never include secrets. */
  retryable?: boolean;
  /** Structured field errors for forms. */
  fieldErrors?: Readonly<Record<string, string>>;
  /** Opaque correlation for future telemetry. */
  correlationId?: string;
};

export function createSdkError(
  code: SdkErrorCode,
  message: string,
  extras?: Omit<SdkError, 'code' | 'message'>,
): SdkError {
  return {
    code,
    message,
    ...extras,
  };
}
