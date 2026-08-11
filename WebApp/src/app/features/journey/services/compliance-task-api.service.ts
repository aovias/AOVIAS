import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import {
  ComplianceTaskDetailDto,
  CreateComplianceTaskRequest,
} from '../models/compliance-task-api.model';

/**
 * Gateway compliance task APIs (`/api/v{version}/compliance/...`).
 * @see ComplaincePlatform/COMPLIANCE_TASK_API_SPEC.md
 */
@Injectable({
  providedIn: 'root',
})
export class ComplianceTaskApiService extends BaseApiService {
  createTask(body: CreateComplianceTaskRequest): Observable<ComplianceTaskDetailDto> {
    return this.post<ComplianceTaskDetailDto>('/compliance/compliance-tasks', body);
  }
}
