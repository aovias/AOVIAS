import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HospitalDashboardStore } from '../../../core/services/hospital-dashboard.store';
import { HospitalDashboardDto } from '../models/hospital-dashboard.model';

@Injectable({
  providedIn: 'root',
})
export class HospitalDashboardService {
  private readonly store = inject(HospitalDashboardStore);

  getHospitalDashboard(
    hospitalId: string,
    options?: { background?: boolean; forceRefresh?: boolean },
  ): Observable<HospitalDashboardDto> {
    return this.store.getHospitalDashboard(hospitalId, options);
  }

  getCached(hospitalId: string): HospitalDashboardDto | null {
    return this.store.getCached(hospitalId);
  }
}
