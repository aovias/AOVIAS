export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
  userId: string;
  email: string;
  role: string;
  roles?: string[];
  platformId?: string;
  organizationId?: string | null;
  hospitalId?: string | null;
  sessionId?: string;
  requiresMfa?: boolean;
  mfaChallengeToken?: string;
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
}

export interface UserSessionDto {
  sessionId: string;
  deviceId?: string;
  ipAddress?: string;
  userAgent?: string;
  createdAt: string;
  expiresAt: string;
  lastActivityAt?: string;
  isCurrent: boolean;
}
