import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export abstract class BaseApiService {
  protected http = inject(HttpClient);
  protected readonly apiBaseUrl = environment.apiBaseUrl;

  protected get<T>(
    endpoint: string,
    params?: Record<string, any>,
    options?: { context?: HttpContext },
  ): Observable<T> {
    const url = `${this.apiBaseUrl}${endpoint}`;
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach((key) => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }

    return this.http.get<T>(url, { params: httpParams, context: options?.context });
  }

  protected post<T>(endpoint: string, body: any, headers?: HttpHeaders): Observable<T> {
    const url = `${this.apiBaseUrl}${endpoint}`;
    return this.http.post<T>(url, body, { headers });
  }

  protected put<T>(endpoint: string, body: any, headers?: HttpHeaders): Observable<T> {
    const url = `${this.apiBaseUrl}${endpoint}`;
    return this.http.put<T>(url, body, { headers });
  }

  protected patch<T>(
    endpoint: string,
    body: any,
    headers?: HttpHeaders,
    params?: Record<string, any>,
  ): Observable<T> {
    const url = `${this.apiBaseUrl}${endpoint}`;
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach((key) => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }
    return this.http.patch<T>(url, body, { headers, params: httpParams });
  }

  protected delete<T>(endpoint: string, params?: Record<string, any>): Observable<T> {
    const url = `${this.apiBaseUrl}${endpoint}`;
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach((key) => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }

    return this.http.delete<T>(url, { params: httpParams });
  }
}
