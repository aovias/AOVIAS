import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceMaturityDashboardModel } from '../models/compliance-maturity.model';

@Injectable({
  providedIn: 'root',
})
export class ComplianceMaturityService extends BaseApiService {
  /**
   * Retrieves the full compliance maturity dashboard for the authenticated
   * hospital/organization. The Gateway resolves hospital context from the JWT.
   *
   * The global api-response interceptor unwraps the ApiResponse<T> envelope,
   * so this observable emits ComplianceMaturityDashboardModel directly.
   */
  getComplianceMaturityDashboard(): Observable<ComplianceMaturityDashboardModel> {
    return this.get<ComplianceMaturityDashboardModel>('/compliance-maturity/dashboard');
  }
}
