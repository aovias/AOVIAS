export interface SignupResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
  userId: string;
  organizationId: string;
  hospitalId: string;
  email: string;
  role: string;
  licenseKeyId?: string | null;
  licenseStatus?: string | null;
  trialExpiresAtUtc?: string | null;
  maxSeats?: number | null;
  provisioningStatus?: string;
}

export const TRIAL_PROVISIONING_STORAGE_KEY = 'aovias.signup.trial';

export interface TrialProvisioningSnapshot {
  licenseKeyId?: string | null;
  licenseStatus?: string | null;
  trialExpiresAtUtc?: string | null;
  maxSeats?: number | null;
  provisioningStatus?: string;
  organizationId?: string;
}

export function getTrialProvisioningStorageKey(userId: string | null | undefined): string {
  const u = userId?.trim();
  return u ? `${TRIAL_PROVISIONING_STORAGE_KEY}:${u}` : TRIAL_PROVISIONING_STORAGE_KEY;
}
