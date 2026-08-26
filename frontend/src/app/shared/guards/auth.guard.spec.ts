import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { authGuard } from './auth.guard';
import { CurrentUserService } from '../services/auth/current-user.service';

describe('authGuard', () => {
  let guard: typeof authGuard;
  let router: Partial<Router>;
  let currentUserService: CurrentUserService;
  let mockRoute: ActivatedRouteSnapshot;
  let mockState: RouterStateSnapshot;

  beforeEach(() => {
    const routerSpy: Partial<Router> = {
      createUrlTree: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        CurrentUserService,
        { provide: Router, useValue: routerSpy },
      ],
    });

    guard = authGuard;
    router = TestBed.inject(Router) as Partial<Router>;
    currentUserService = TestBed.inject(CurrentUserService);

    mockRoute = {} as ActivatedRouteSnapshot;
    mockState = { url: '/analysis/1' } as RouterStateSnapshot;

     
    localStorage.clear();
  });

  afterEach(() => {
     
    localStorage.clear();
  });

  it('should allow access when authenticated', () => {
    currentUserService.setToken('test-token');

    const result = TestBed.runInInjectionContext(() =>
      guard(mockRoute, mockState)
    );

    expect(result).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('should redirect to login when not authenticated', () => {
    currentUserService.clearToken();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const urlTree = {} as any;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (router.createUrlTree as any).mockReturnValue(urlTree);

    const result = TestBed.runInInjectionContext(() =>
      guard(mockRoute, mockState)
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
    expect(result).toBe(urlTree);
  });

  it('should redirect to login when token is null', () => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const urlTree = {} as any;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (router.createUrlTree as any).mockReturnValue(urlTree);

    TestBed.runInInjectionContext(() =>
      guard(mockRoute, mockState)
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
  });
});
