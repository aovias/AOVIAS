import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface DirectorySearchHit {
  personId: string;
  employmentId?: string;
  displayName: string;
  departmentName?: string;
  hospitalName?: string;
  designation?: string;
  employmentStatus?: string;
  email?: string;
  score: number;
}

export interface DirectorySearchResult {
  items: DirectorySearchHit[];
  totalCount: number;
  page: number;
  pageSize: number;
  executionMs: number;
  facets: Record<string, { value: string; count: number }[]>;
}

export interface DirectorySuggestion {
  text: string;
  subtitle?: string;
  personId?: string;
  employmentId?: string;
}

export interface EnterpriseDirectoryProfile {
  personId: string;
  displayName: string;
  email?: string;
  phone?: string;
  employments: { employmentId: string; departmentName?: string; designation?: string }[];
}

export interface SavedSearch {
  id: string;
  name: string;
  visibility: string;
  createdAtUtc: string;
}

export interface SearchHistoryItem {
  id: string;
  searchTerms: string;
  resultCount: number;
  searchedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class EnterpriseDirectoryApiService extends BaseApiService {
  private base = '/directory';

  search(organizationId: string, q: string, page = 1, pageSize = 20): Observable<DirectorySearchResult> {
    return this.get<DirectorySearchResult>(`${this.base}/search`, { organizationId, q, page, pageSize, fuzzy: true })
      ;
  }

  suggestions(prefix: string, organizationId: string): Observable<DirectorySuggestion[]> {
    return this.get<DirectorySuggestion[]>(`${this.base}/suggestions`, { prefix, organizationId })
      ;
  }

  profile(id: string, organizationId?: string): Observable<EnterpriseDirectoryProfile> {
    const params: Record<string, string> = {};
    if (organizationId) params['organizationId'] = organizationId;
    return this.get<EnterpriseDirectoryProfile>(`${this.base}/profile/${id}`, params)
      ;
  }

  facets(organizationId: string): Observable<Record<string, { value: string; count: number }[]>> {
    return this.get<Record<string, { value: string; count: number }[]>>(`${this.base}/facets`, { organizationId })
      ;
  }

  popular(organizationId: string): Observable<{ term: string; count: number }[]> {
    return this.get<{ term: string; count: number }[]>(`${this.base}/popular`, { organizationId })
      ;
  }

  history(organizationId: string): Observable<SearchHistoryItem[]> {
    return this.get<SearchHistoryItem[]>(`${this.base}/history`, { organizationId })
      ;
  }

  savedSearches(organizationId: string): Observable<SavedSearch[]> {
    return this.get<SavedSearch[]>(`${this.base}/saved-searches`, { organizationId })
      ;
  }

  nlSearch(organizationId: string, query: string): Observable<{ results: DirectorySearchResult; translatedQuery: unknown }> {
    return this.post<{ results: DirectorySearchResult; translatedQuery: unknown }>(`${this.base}/nl-search`, { organizationId, query })
      ;
  }
}
