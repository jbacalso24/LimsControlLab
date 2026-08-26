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
