import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { GapAnalysisReportDto } from '../models/gap-analysis-api.model';

@Injectable({
  providedIn: 'root',
})
export class GapAnalysisApiService extends BaseApiService {
  getReport(assessmentId: string): Observable<GapAnalysisReportDto> {
    return this.get<GapAnalysisReportDto>(
      `/knowledge-base/assessments/${assessmentId}/gap-analysis`,
    );
  }

  generateReport(assessmentId: string): Observable<GapAnalysisReportDto> {
    return this.post<GapAnalysisReportDto>(
      `/knowledge-base/assessments/${assessmentId}/gap-analysis`,
      {},
    );
  }
}
