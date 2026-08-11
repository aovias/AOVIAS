import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface ProfessionalListItem {
  id: string;
  professionalNumber: string;
  personSearchName: string;
  personId: string;
  employmentId: string;
}

export interface ProfessionalDetail extends ProfessionalListItem {
  organizationId?: string;
  hospitalId?: string;
  biography?: string;
  yearsOfExperience?: number;
  practiceStartDate?: string;
  qualifications: { id: string; title: string; institution?: string }[];
  registrations: { id: string; registrationNumber: string; expiryDate?: string; status: string }[];
  licenses: { id: string; licenseNumber: string; expiryDate?: string; status: string }[];
  certifications: { id: string; certificationName: string; expiryDate?: string }[];
  competencies: { id: string; skillName: string; level?: string }[];
  clinicalPrivileges: { id: string; privilegeName: string; status: string }[];
  credentialingRecords: { id: string; status: string }[];
  trainingRecords: { id: string; courseName: string; completionDate?: string }[];
}

export interface RenewalItem {
  professionalProfileId: string;
  professionalNumber: string;
  personSearchName: string;
  itemType: string;
  itemName: string;
  expiryDate?: string;
  daysUntilExpiry: number;
}

export interface PagedProfessionals<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ProfessionalApiService extends BaseApiService {
  list(page = 1, pageSize = 20): Observable<PagedProfessionals<ProfessionalListItem>> {
    return this.get<PagedProfessionals<ProfessionalListItem>>('/professional-profiles', { page, pageSize });
  }

  search(q: string, page = 1, pageSize = 20): Observable<PagedProfessionals<ProfessionalListItem>> {
    return this.get<PagedProfessionals<ProfessionalListItem>>('/professional-profiles/search', { q, page, pageSize });
  }

  getById(id: string): Observable<ProfessionalDetail> {
    return this.get<ProfessionalDetail>(`/professional-profiles/${id}`);
  }

  getTimeline(id: string): Observable<{ id: string; occurredAt: string; eventType: string; summary: string }[]> {
    return this.get(`/professional-profiles/${id}/timeline`);
  }

  getRenewals(withinDays = 90): Observable<RenewalItem[]> {
    return this.get<RenewalItem[]>('/professional-profiles/renewals', { withinDays });
  }

  getLookups(category: string): Observable<{ id: string; code: string; name: string }[]> {
    return this.get(`/professional-profiles/lookups/${category}`);
  }

  addTrainingRecord(
    profileId: string,
    body: {
      courseName: string;
      provider?: string;
      hours?: number;
      completionDate?: string;
      certificateUrl?: string;
      isMandatory?: boolean;
      assessmentScore?: number;
    },
  ): Observable<{ id: string; courseName: string; completionDate?: string }> {
    return this.post(`/professional-profiles/${profileId}/training`, body);
  }
}
