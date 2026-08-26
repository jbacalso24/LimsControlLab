import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Signal, computed, inject } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export abstract class BaseApiService {
  protected http: HttpClient = inject(HttpClient);

  abstract get apiBase(): Signal<string>;

  protected buildParams(params?: Record<string, unknown>): HttpParams {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined) {
          httpParams = httpParams.set(key, String(value));
        }
      });
    }
    return httpParams;
  }

  protected get<T>(path: string, params?: Record<string, unknown>): Observable<T> {
    const url = `${this.apiBase()}${path}`;
    return this.http.get<T>(url, {
      params: this.buildParams(params),
    });
  }

  protected post<T>(path: string, body?: unknown, params?: Record<string, unknown>): Observable<T> {
    const url = `${this.apiBase()}${path}`;
    return this.http.post<T>(url, body, {
      params: this.buildParams(params),
    });
  }

  protected put<T>(path: string, body?: unknown, params?: Record<string, unknown>): Observable<T> {
    const url = `${this.apiBase()}${path}`;
    return this.http.put<T>(url, body, {
      params: this.buildParams(params),
    });
  }

  protected delete<T>(path: string, params?: Record<string, unknown>): Observable<T> {
    const url = `${this.apiBase()}${path}`;
    return this.http.delete<T>(url, {
      params: this.buildParams(params),
    });
  }

  protected patch<T>(path: string, body?: unknown, params?: Record<string, unknown>): Observable<T> {
    const url = `${this.apiBase()}${path}`;
    return this.http.patch<T>(url, body, {
      params: this.buildParams(params),
    });
  }

  protected rxGet<T>(
    pathSignal: Signal<string>,
    paramsSignal?: Signal<Record<string, unknown> | undefined>
  ): Observable<T> {
    const url = computed(() => `${this.apiBase()}${pathSignal()}`);
    const params = computed(() => this.buildParams(paramsSignal?.()));

    return new Observable((subscriber) => {
      try {
        this.http.get<T>(url(), {
          params: params(),
        }).subscribe(subscriber);
      } catch (error) {
        subscriber.error(error);
      }
    });
  }
}
