import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface FrameworkCatalogItem {
  id?: string;
  frameworkId?: string;
  name: string;
  authority?: string;
  code?: string;
  version?: string;
  status?: string;
  isActive?: boolean;
  publishedAt?: string;
  retiredAt?: string;
  archivedAt?: string;
}

export interface FrameworkStructureNode {
  id?: string;
  code?: string;
  title?: string;
  name?: string;
  children?: FrameworkStructureNode[];
  chapters?: FrameworkStructureNode[];
  standards?: FrameworkStructureNode[];
  objectiveElements?: FrameworkStructureNode[];
}

@Injectable({ providedIn: 'root' })
export class FrameworksApiService {
  private readonly base = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  private unwrap<T>(r: T | { data: T }): T {
    return r && typeof r === 'object' && 'data' in (r as object)
      ? (r as { data: T }).data
      : (r as T);
  }

  listCatalog(includeInactive = true): Observable<FrameworkCatalogItem[]> {
    return this.http
      .get<FrameworkCatalogItem[] | { data: FrameworkCatalogItem[] }>(`${this.base}/frameworks/catalog`, {
        params: { includeInactive },
      })
      .pipe(map((r) => this.unwrap(r) ?? []));
  }

  publish(id: string): Observable<FrameworkCatalogItem> {
    return this.http
      .post<FrameworkCatalogItem | { data: FrameworkCatalogItem }>(`${this.base}/frameworks/${id}/publish`, {})
      .pipe(map((r) => this.unwrap(r)));
  }

  activate(id: string): Observable<FrameworkCatalogItem> {
    return this.http
      .post<FrameworkCatalogItem | { data: FrameworkCatalogItem }>(`${this.base}/frameworks/${id}/activate`, {})
      .pipe(map((r) => this.unwrap(r)));
  }

  retire(id: string): Observable<FrameworkCatalogItem> {
    return this.http
      .post<FrameworkCatalogItem | { data: FrameworkCatalogItem }>(`${this.base}/frameworks/${id}/retire`, {})
      .pipe(map((r) => this.unwrap(r)));
  }

  archive(id: string): Observable<FrameworkCatalogItem> {
    return this.http
      .post<FrameworkCatalogItem | { data: FrameworkCatalogItem }>(`${this.base}/frameworks/${id}/archive`, {})
      .pipe(map((r) => this.unwrap(r)));
  }

  getStructure(id: string): Observable<FrameworkStructureNode> {
    // Gateway v1.1 structure proxy
    const url = `/api/v1.1/frameworks/${id}/structure`;
    return this.http
      .get<FrameworkStructureNode | { data: FrameworkStructureNode }>(url, {
        params: new HttpParams().set('page', '1').set('pageSize', '100'),
      })
      .pipe(map((r) => this.unwrap(r)));
  }
}
