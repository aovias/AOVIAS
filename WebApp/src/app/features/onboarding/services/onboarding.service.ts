import { Injectable } from '@angular/core';
import { HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { OnboardSubmitRequest } from '../models/onboard-submit-request.model';
import { OnboardingRequest } from '../models/onboarding-request.model';
import { SignupResponse } from '../models/signup-response.model';

@Injectable({
  providedIn: 'root',
})
export class OnboardingService extends BaseApiService {
  createHospital(request: OnboardingRequest): Observable<SignupResponse> {
    return this.post<SignupResponse>('/onboarding/signup', request);
  }

  /** AI Setup Assistant — final onboard after wizard review. */
  submitOnboard(request: OnboardSubmitRequest, idempotencyKey: string): Observable<unknown> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey,
    });
    return this.post<unknown>('/onboarding/onboard', request, headers);
  }
}
