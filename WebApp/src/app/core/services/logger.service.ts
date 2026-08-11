import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
}

@Injectable({
  providedIn: 'root',
})
export class LoggerService {
  private readonly isDebugEnabled = environment.enableDebugLogging;

  debug(message: string, ...args: any[]): void {
    if (this.isDebugEnabled) {
      console.debug(`[DEBUG] ${message}`, ...args);
    }
  }

  info(message: string, ...args: any[]): void {
    console.info(`[INFO] ${message}`, ...args);
  }

  warn(message: string, ...args: any[]): void {
    console.warn(`[WARN] ${message}`, ...args);
  }

  error(message: string, error?: Error | any, ...args: any[]): void {
    console.error(`[ERROR] ${message}`, error, ...args);

    // In production, you might want to send errors to a logging service
    if (environment.production && error) {
      // TODO: Send to error tracking service (e.g., Sentry, LogRocket)
    }
  }
}
