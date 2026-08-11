export interface LoginRequest {
  email: string;
  password: string;
  deviceId?: string;
}

export interface MfaLoginRequest {
  mfaChallengeToken: string;
  code: string;
  deviceId?: string;
}
