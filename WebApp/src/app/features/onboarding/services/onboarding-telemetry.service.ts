import { Injectable } from '@angular/core';
import { BaseApiService } from '../../../shared/services/base-api.service';

@Injectable({ providedIn: 'root' })
export class OnboardingTelemetryService extends BaseApiService {
  track(eventType: string, stage?: string, payload?: Record<string, unknown>): void {
    this.post('/onboarding/events', {
      eventType,
      stage: stage ?? null,
      payloadJson: payload ? JSON.stringify(payload) : null,
    }).subscribe({ error: () => { /* best-effort */ } });
  }
}
