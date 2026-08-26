import { Injectable, signal } from '@angular/core';

export interface CurrentUser {
  sub: string;
  username: string;
  role: 'ControlLabAnalyst' | 'LabCoordinator';
  site: string;
}

@Injectable({
  providedIn: 'root',
})
export class CurrentUserService {
  private readonly tokenSignal = signal<string | null>(
    this.readTokenFromStorage()
  );
  private readonly userSignal = signal<CurrentUser | null>(null);

  readonly token = this.tokenSignal.asReadonly();
  readonly user = this.userSignal.asReadonly();

  constructor() {
    // On app start (e.g. after a page refresh) the token persists in localStorage but the
    // in-memory user is empty. Rehydrate the user from the token's claims so a refresh keeps
    // the session (nav shows who's logged in); if the token is expired or malformed, clear it
    // so the guard routes to a clean login instead of firing authenticated calls that all 401.
    const token = this.tokenSignal();
    if (token) {
      const user = this.decodeUserFromToken(token);
      if (user) {
        this.userSignal.set(user);
      } else {
        this.clearToken();
      }
    }
  }

  private decodeUserFromToken(token: string): CurrentUser | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))) as Record<
        string,
        unknown
      >;
      const exp = typeof payload['exp'] === 'number' ? payload['exp'] : 0;
      if (exp && Date.now() >= exp * 1000) return null; // expired
      const nameId =
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
        payload['sub'] ??
        '';
      const username =
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        payload['unique_name'] ??
        payload['name'] ??
        '';
      const role = (payload['role'] as string) ?? '';
      const site = (payload['site'] as string) ?? '';
      if (!username || (role !== 'ControlLabAnalyst' && role !== 'LabCoordinator')) return null;
      return {
        sub: String(nameId),
        username: String(username),
        role,
        site: String(site),
      };
    } catch {
      return null;
    }
  }

  private readTokenFromStorage(): string | null {
    if (typeof window === 'undefined') return null;
     
    return localStorage.getItem('auth_token');
  }

  setToken(token: string): void {
     
    localStorage.setItem('auth_token', token);
    this.tokenSignal.set(token);
  }

  setUser(user: CurrentUser): void {
    this.userSignal.set(user);
  }

  clearToken(): void {
     
    localStorage.removeItem('auth_token');
    this.tokenSignal.set(null);
    this.userSignal.set(null);
  }

  isAuthenticated(): boolean {
    return this.tokenSignal() !== null;
  }
}
