import type { SdkError } from './errors';

/**
 * Discriminated result used across SDKs.
 * Prefer this over throwing for expected domain/transport outcomes.
 */

export type SdkSuccess<T> = {
  ok: true;
  data: T;
  message?: string;
};

export type SdkFailure = {
  ok: false;
  error: SdkError;
};

export type SdkResult<T> = SdkSuccess<T> | SdkFailure;

export function sdkOk<T>(data: T, message?: string): SdkSuccess<T> {
  return message === undefined ? { ok: true, data } : { ok: true, data, message };
}

export function sdkFail(error: SdkError): SdkFailure {
  return { ok: false, error };
}

export function isSdkOk<T>(result: SdkResult<T>): result is SdkSuccess<T> {
  return result.ok;
}

export function isSdkFail<T>(result: SdkResult<T>): result is SdkFailure {
  return !result.ok;
}
