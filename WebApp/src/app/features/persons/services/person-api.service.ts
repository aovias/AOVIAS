import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface PersonListItem {
  id: string;
  platformId: string;
  displayName: string;
  status: string;
  primaryEmail?: string;
  primaryPhone?: string;
  photoUrl?: string;
}

export interface PersonDetail extends PersonListItem {
  searchName: string;
  legalFirstName?: string;
  legalLastName?: string;
  preferredName?: string;
  dateOfBirth?: string;
  timeZone?: string;
  contacts: PersonContact[];
  addresses: PersonAddress[];
  documents: PersonDocument[];
  emergencyContacts: EmergencyContact[];
  preferences?: PersonPreferences;
}

export interface PersonContact {
  id: string;
  contactKind: string;
  value: string;
  isPrimary: boolean;
}

export interface PersonAddress {
  id: string;
  line1: string;
  city?: string;
  state?: string;
  postalCode?: string;
  countryCode?: string;
  isPrimary: boolean;
}

export interface PersonDocument {
  id: string;
  documentNumber: string;
  verificationStatus: string;
}

export interface EmergencyContact {
  id: string;
  name: string;
  phone?: string;
  email?: string;
}

export interface PersonPreferences {
  theme: string;
  languageCode?: string;
  dateFormat: string;
  timeFormat: string;
}

export interface PagedPersons<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface LookupValue {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
}

@Injectable({ providedIn: 'root' })
export class PersonApiService extends BaseApiService {
  list(page = 1, pageSize = 20, status?: string): Observable<PagedPersons<PersonListItem>> {
    return this.get<PagedPersons<PersonListItem>>('/persons', { page, pageSize, status });
  }

  search(q: string, page = 1, pageSize = 20): Observable<PagedPersons<PersonListItem>> {
    return this.get<PagedPersons<PersonListItem>>('/persons/search', { q, page, pageSize });
  }

  getById(id: string): Observable<PersonDetail> {
    return this.get<PersonDetail>(`/persons/${id}`);
  }

  getLookups(category: string): Observable<LookupValue[]> {
    return this.get<LookupValue[]>(`/persons/lookups/${category}`);
  }

  getTimeline(id: string): Observable<{ id: string; occurredAt: string; eventType: string; summary: string }[]> {
    return this.get(`/persons/${id}/timeline`);
  }

  create(body: Record<string, unknown>): Observable<unknown> {
    return this.post('/persons', body);
  }

  update(id: string, body: Record<string, unknown>): Observable<PersonDetail> {
    return this.put<PersonDetail>(`/persons/${id}`, body);
  }
}
