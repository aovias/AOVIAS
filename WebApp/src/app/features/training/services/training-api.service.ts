import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface TrainingProgram {
  id: string;
  organizationId: string;
  hospitalId?: string;
  code: string;
  title: string;
  description?: string;
  controlledDocumentId?: string;
  roleCode?: string;
  departmentId?: string;
  isMandatory: boolean;
  renewalMonths?: number;
  status: string;
  createdAt: string;
  sessionCount: number;
}

export interface TrainingSession {
  id: string;
  trainingProgramId: string;
  programTitle: string;
  title: string;
  startsAt: string;
  endsAt?: string;
  location?: string;
  facilitator?: string;
  capacity?: number;
  status: string;
  assignedCount: number;
  attendedCount: number;
  completionPercent: number;
  controlledDocumentId?: string;
}

export interface TrainingAttendance {
  id: string;
  trainingSessionId: string;
  professionalProfileId: string;
  attendanceStatus: string;
  checkedInAt?: string;
  certificateUrl?: string;
  certificateIssuedAt?: string;
  expiryDate?: string;
  assessment?: {
    id: string;
    score?: number;
    passMark?: number;
    passed?: boolean;
    assessorName?: string;
    assessedAt?: string;
    notes?: string;
  };
}

export interface TrainingMatrixItem {
  programId: string;
  programCode: string;
  programTitle: string;
  isMandatory: boolean;
  roleCode?: string;
  departmentId?: string;
  controlledDocumentId?: string;
  completionPercent: number;
  assignedCount: number;
  completedCount: number;
}

@Injectable({ providedIn: 'root' })
export class TrainingApiService extends BaseApiService {
  listPrograms(organizationId?: string, mandatoryOnly?: boolean): Observable<TrainingProgram[]> {
    return this.get<TrainingProgram[]>('/training/programs', { organizationId, mandatoryOnly });
  }

  getProgram(id: string): Observable<TrainingProgram> {
    return this.get<TrainingProgram>(`/training/programs/${id}`);
  }

  createProgram(body: Partial<TrainingProgram> & { organizationId: string; code: string; title: string }): Observable<TrainingProgram> {
    return this.post<TrainingProgram>('/training/programs', body);
  }

  getMatrix(organizationId?: string, roleCode?: string): Observable<TrainingMatrixItem[]> {
    return this.get<TrainingMatrixItem[]>('/training/programs/matrix', { organizationId, roleCode });
  }

  listSessions(programId?: string, from?: string, to?: string): Observable<TrainingSession[]> {
    return this.get<TrainingSession[]>('/training/sessions', { programId, from, to });
  }

  getSession(id: string): Observable<TrainingSession> {
    return this.get<TrainingSession>(`/training/sessions/${id}`);
  }

  createSession(body: {
    trainingProgramId: string;
    title: string;
    startsAt: string;
    endsAt?: string;
    location?: string;
    facilitator?: string;
    capacity?: number;
  }): Observable<TrainingSession> {
    return this.post<TrainingSession>('/training/sessions', body);
  }

  listAttendances(sessionId: string): Observable<TrainingAttendance[]> {
    return this.get<TrainingAttendance[]>(`/training/sessions/${sessionId}/attendances`);
  }

  assignParticipants(sessionId: string, professionalProfileIds: string[]): Observable<TrainingAttendance[]> {
    return this.post<TrainingAttendance[]>(`/training/sessions/${sessionId}/participants`, { professionalProfileIds });
  }

  recordAttendance(attendanceId: string, attendanceStatus = 'Attended'): Observable<TrainingAttendance> {
    return this.post<TrainingAttendance>(`/training/sessions/attendances/${attendanceId}/record`, { attendanceStatus });
  }

  recordAssessment(attendanceId: string, body: { score?: number; passMark?: number; passed?: boolean; assessorName?: string; notes?: string }): Observable<unknown> {
    return this.post(`/training/sessions/attendances/${attendanceId}/assessment`, body);
  }

  setCertificate(attendanceId: string, certificateUrl: string, expiryDate?: string): Observable<TrainingAttendance> {
    return this.post<TrainingAttendance>(`/training/sessions/attendances/${attendanceId}/certificate`, { certificateUrl, expiryDate });
  }
}
