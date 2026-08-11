import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface SetupStatus {
  completed: boolean;
  lockFilePresent: boolean;
  configurationFilePresent: boolean;
  completedAtUtc: string | null;
  markerSource: string;
}

export interface DatabaseTestResult {
  success: boolean;
  message: string;
  serverVersionMajor: number | null;
}

export interface SetupConfigurePayload {
  database?: { connectionString: string; provider?: string };
  smtp?: {
    host: string;
    port?: number;
    username?: string;
    password?: string;
    fromAddress?: string;
    useSsl?: boolean;
  };
  storage?: {
    provider: string;
    rootPath?: string;
    connectionString?: string;
    containerName?: string;
  };
  ai?: { provider?: string; endpoint?: string; apiKey?: string; enabled?: boolean };
  auth?: {
    jwtIssuer?: string;
    jwtAudience?: string;
    jwtSecretKey?: string;
    expirationMinutes?: number;
  };
}

export interface SetupConfigureResult {
  saved: boolean;
  path: string;
  sectionsWritten: string[];
}

export interface HealthPingResult {
  service: string;
  url: string;
  success: boolean;
  statusCode: number | null;
  message: string;
}

export interface SetupCompleteResult {
  completed: boolean;
  lockPath: string;
  healthPings: HealthPingResult[];
}

@Injectable({ providedIn: 'root' })
export class SetupApiService extends BaseApiService {
  private readonly base = '/setup';

  getStatus(): Observable<SetupStatus> {
    return this.get<SetupStatus>(`${this.base}/status`);
  }

  testDatabase(connectionString: string): Observable<DatabaseTestResult> {
    return this.post<DatabaseTestResult>(`${this.base}/database/test`, { connectionString });
  }

  configure(payload: SetupConfigurePayload): Observable<SetupConfigureResult> {
    return this.post<SetupConfigureResult>(`${this.base}/configure`, payload);
  }

  complete(): Observable<SetupCompleteResult> {
    return this.post<SetupCompleteResult>(`${this.base}/complete`, {});
  }
}
