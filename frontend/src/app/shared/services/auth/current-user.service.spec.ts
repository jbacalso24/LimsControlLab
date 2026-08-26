import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { CurrentUserService } from './current-user.service';

describe('CurrentUserService', () => {
  let service: CurrentUserService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CurrentUserService);
     
    localStorage.clear();
  });

  afterEach(() => {
     
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should store and retrieve token', () => {
    const token = 'test-token-123';
    service.setToken(token);
    expect(service.token()).toBe(token);
     
    expect(localStorage.getItem('auth_token')).toBe(token);
  });

  it('should set and retrieve user', () => {
    const user = {
      sub: 'user-123',
      username: 'user-123',
      role: 'ControlLabAnalyst' as const,
      site: 'Site-A',
    };
    service.setUser(user);
    expect(service.user()).toEqual(user);
  });

  it('should report authenticated when token is set', () => {
    service.setToken('test-token');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('should report not authenticated when token is null', () => {
    service.clearToken();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('should clear token and user', () => {
    service.setToken('test-token');
    service.setUser({
      sub: 'user-123',
      username: 'user-123',
      role: 'ControlLabAnalyst',
      site: 'Site-A',
    });

    service.clearToken();

    expect(service.token()).toBeNull();
    expect(service.user()).toBeNull();
     
    expect(localStorage.getItem('auth_token')).toBeNull();
  });
});
