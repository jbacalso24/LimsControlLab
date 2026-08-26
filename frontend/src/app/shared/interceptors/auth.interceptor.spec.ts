import { TestBed } from '@angular/core/testing';
import { runInInjectionContext, Injector } from '@angular/core';
import { HttpRequest, HttpResponse } from '@angular/common/http';
import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import { of } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { CurrentUserService } from '../services/auth/current-user.service';

describe('authInterceptor', () => {
  let currentUserService: CurrentUserService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CurrentUserService],
    });

    currentUserService = TestBed.inject(CurrentUserService);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should add Authorization header when token is set', () => {
    const token = 'fake-token-123';
    currentUserService.setToken(token);

    const req = new HttpRequest('GET', '/api/test');
    let capturedRequest: HttpRequest<unknown> | null = null;

    const injector = TestBed.inject(Injector);
    runInInjectionContext(injector, () => {
      authInterceptor(req, (modifiedReq) => {
        capturedRequest = modifiedReq;
        return of(new HttpResponse({ status: 200, body: {} }));
      });
    });

    expect(capturedRequest).toBeTruthy();
    expect(capturedRequest!.headers.get('Authorization')).toBe(`Bearer ${token}`);
  });

  it('should not add Authorization header when no token is set', () => {
    const req = new HttpRequest('GET', '/api/test');
    let capturedRequest: HttpRequest<unknown> | null = null;

    const injector = TestBed.inject(Injector);
    runInInjectionContext(injector, () => {
      authInterceptor(req, (modifiedReq) => {
        capturedRequest = modifiedReq;
        return of(new HttpResponse({ status: 200, body: {} }));
      });
    });

    expect(capturedRequest).toBeTruthy();
    expect(capturedRequest!.headers.get('Authorization')).toBeNull();
  });
});
