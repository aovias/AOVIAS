import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  CreateIncidentRequest,
  IncidentAnalytics,
  IncidentDashboard,
  IncidentDetail,
  IncidentInvestigation,
  IncidentList,
  IncidentQuery,
  IncidentRca,
  LessonLearned,
  PatientFeedback,
  SaveRcaRequest,
} from '../models/incident.models';

@Injectable({ providedIn: 'root' })
export class IncidentApiService extends BaseApiService {
  getDashboard(): Observable<IncidentDashboard> {
    return this.get('/incidents/dashboard');
  }

  getIncidents(query: IncidentQuery = {}): Observable<IncidentList> {
    return this.get('/incidents', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      sortDesc: query.sortDesc ?? true,
      ...(query.status ? { status: query.status } : {}),
      ...(query.severity ? { severity: query.severity } : {}),
      ...(query.category ? { category: query.category } : {}),
      ...(query.department ? { department: query.department } : {}),
      ...(query.incidentType ? { incidentType: query.incidentType } : {}),
      ...(query.quickFilter ? { quickFilter: query.quickFilter } : {}),
      ...(query.search ? { search: query.search } : {}),
      ...(query.sortBy ? { sortBy: query.sortBy } : {}),
    });
  }

  getById(id: string): Observable<IncidentDetail> {
    return this.get(`/incidents/${id}`);
  }

  create(payload: CreateIncidentRequest): Observable<IncidentDetail> {
    return this.post('/incidents', payload);
  }

  patchIncident(id: string, payload: Record<string, unknown>): Observable<IncidentDetail> {
    return super.patch(`/incidents/${id}`, payload);
  }

  saveInvestigation(payload: Record<string, unknown>): Observable<IncidentInvestigation> {
    return this.post('/incidents/investigation', payload);
  }

  saveRca(payload: SaveRcaRequest): Observable<IncidentRca> {
    return this.post('/incidents/rca', payload);
  }

  promoteFinding(payload: { incidentId: string; observation?: string; recommendation?: string }): Observable<unknown> {
    return this.post('/incidents/promote-finding', payload);
  }

  promoteDeficiency(payload: { incidentId: string; findingId?: string }): Observable<unknown> {
    return this.post('/incidents/promote-deficiency', payload);
  }

  createLesson(payload: Record<string, unknown>): Observable<LessonLearned> {
    return this.post('/incidents/lesson', payload);
  }

  getFeedback(): Observable<PatientFeedback[]> {
    return this.get('/incidents/feedback');
  }

  createFeedback(payload: Record<string, unknown>): Observable<PatientFeedback> {
    return this.post('/incidents/feedback', payload);
  }

  getLessons(search?: string): Observable<LessonLearned[]> {
    return this.get('/incidents/lessons', search ? { search } : {});
  }

  getAnalytics(): Observable<IncidentAnalytics> {
    return this.get('/incidents/analytics');
  }
}
